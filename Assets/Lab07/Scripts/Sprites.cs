using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab07
{
    /// <summary>
    /// All art is generated procedurally (signed-distance shapes rasterised into textures) so the project
    /// needs no third-party images and nothing is AI-generated. Icons are white masks tinted by Image.color.
    /// </summary>
    public static class Sprites
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { cache.Clear(); }

        static Sprite Make(string key, Texture2D t, Vector4 border = default)
        {
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.Apply(false, true);
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = key; cache[key] = s; return s;
        }

        // ---- 9-sliced rounded rectangle
        public static Sprite Round(int r)
        {
            string key = "round" + r; if (cache.TryGetValue(key, out var c)) return c;
            int w = r * 2 + 4; var t = new Texture2D(w, w, TextureFormat.RGBA32, false); var px = new Color32[w * w];
            for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                {
                    float d = RR(x + .5f, y + .5f, w, r);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(.5f - d) * 255));
                }
            t.SetPixels32(px); return Make(key, t, new Vector4(r, r, r, r));
        }

        /// <summary>Outline only (transparent inside), same slicing as Round(r) so the two stack perfectly.</summary>
        public static Sprite Ring(int r, int thick)
        {
            string key = "ring" + r + "_" + thick; if (cache.TryGetValue(key, out var c)) return c;
            int w = r * 2 + 4; var t = new Texture2D(w, w, TextureFormat.RGBA32, false); var px = new Color32[w * w];
            for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                {
                    float d = RR(x + .5f, y + .5f, w, r);
                    float a = Mathf.Clamp01(.5f - d) * Mathf.Clamp01(.5f + d + thick);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); return Make(key, t, new Vector4(r, r, r, r));
        }

        static float RR(float x, float y, int w, int r)
        {
            float h = w * .5f; float qx = Mathf.Abs(x - h) - (h - r), qy = Mathf.Abs(y - h) - (h - r);
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        public static Sprite Circle()
        {
            string key = "circle"; if (cache.TryGetValue(key, out var c)) return c;
            int w = 128; var t = new Texture2D(w, w, TextureFormat.RGBA32, false); var px = new Color32[w * w];
            for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                {
                    float d = new Vector2(x + .5f - 64, y + .5f - 64).magnitude - 63f;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(.5f - d) * 255));
                }
            t.SetPixels32(px); return Make(key, t);
        }

        public static Sprite Glow()
        {
            string key = "glow"; if (cache.TryGetValue(key, out var c)) return c;
            int w = 128; var t = new Texture2D(w, w, TextureFormat.RGBA32, false); var px = new Color32[w * w];
            for (int y = 0; y < w; y++) for (int x = 0; x < w; x++)
                {
                    float d = new Vector2(x + .5f - 64, y + .5f - 64).magnitude / 64f; float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); return Make(key, t);
        }

        public static Sprite Gradient(Color top, Color bottom)
        {
            string key = "grad" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom);
            if (cache.TryGetValue(key, out var c)) return c;
            var t = new Texture2D(2, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++) { var col = Color.Lerp(bottom, top, y / 255f); t.SetPixel(0, y, col); t.SetPixel(1, y, col); }
            return Make(key, t);
        }

        // ---------------------------------------------------------------- icons
        public static Sprite Icon(string name)
        {
            string key = "icon_" + name; if (cache.TryGetValue(key, out var c)) return c;
            const int W = 256; var t = new Texture2D(W, W, TextureFormat.RGBA32, false); var px = new Color32[W * W];
            Func<Vector2, float> f = IconSdf(name);
            for (int y = 0; y < W; y++) for (int x = 0; x < W; x++)
                {
                    var p = new Vector2((x + .5f) / W, (y + .5f) / W);
                    float d = f(p) * W;
                    px[y * W + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(.5f - d) * 255));
                }
            t.SetPixels32(px); return Make(key, t);
        }

        // --- SDF toolbox (unit square, y up)
        static float Circ(Vector2 p, float cx, float cy, float r) => (p - new Vector2(cx, cy)).magnitude - r;
        static float Box(Vector2 p, float cx, float cy, float hx, float hy, float rad = 0f)
        {
            float qx = Mathf.Abs(p.x - cx) - hx + rad, qy = Mathf.Abs(p.y - cy) - hy + rad;
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - rad;
        }
        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float w)
        {
            var a = new Vector2(ax, ay); var b = new Vector2(bx, by); var pa = p - a; var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba)); return (pa - ba * h).magnitude - w;
        }
        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]); float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i]; var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
        static Vector2[] V(params float[] xy) { var v = new Vector2[xy.Length / 2]; for (int i = 0; i < v.Length; i++) v[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]); return v; }
        static Vector2[] Mirror(params float[] half)
        {   // half = right-side points top->bottom (x>0.5); returns closed polygon
            int n = half.Length / 2; var v = new Vector2[n * 2];
            for (int i = 0; i < n; i++) { v[i] = new Vector2(half[i * 2], half[i * 2 + 1]); v[n * 2 - 1 - i] = new Vector2(1f - half[i * 2], half[i * 2 + 1]); }
            return v;
        }
        static float U(float a, float b) => Mathf.Min(a, b);
        static float Sub(float a, float b) => Mathf.Max(a, -b);
        static float Outline(float d, float t) => Mathf.Abs(d + t) - t;
        static float Ringd(Vector2 p, float cx, float cy, float r, float w) => Mathf.Abs((p - new Vector2(cx, cy)).magnitude - r) - w;
        static Vector2 Rot(Vector2 p, float cx, float cy, float deg)
        {
            float a = -deg * Mathf.Deg2Rad, s = Mathf.Sin(a), c = Mathf.Cos(a); var q = p - new Vector2(cx, cy);
            return new Vector2(q.x * c - q.y * s, q.x * s + q.y * c) + new Vector2(cx, cy);
        }
        static Vector2[] StarPts(float cx, float cy, float R, float r, int n = 5)
        {
            var v = new Vector2[n * 2];
            for (int i = 0; i < n * 2; i++) { float a = Mathf.PI / 2 + i * Mathf.PI / n; float rad = i % 2 == 0 ? R : r; v[i] = new Vector2(cx + Mathf.Cos(a) * rad, cy + Mathf.Sin(a) * rad); }
            return v;
        }

        static Func<Vector2, float> IconSdf(string n)
        {
            switch (n)
            {
                case "play": return p => Poly(p, V(.30f, .14f, .30f, .86f, .86f, .5f));
                case "pause": return p => U(Box(p, .34f, .5f, .10f, .32f, .03f), Box(p, .66f, .5f, .10f, .32f, .03f));
                case "back": return p => U(Seg(p, .64f, .18f, .34f, .5f, .075f), Seg(p, .34f, .5f, .64f, .82f, .075f));
                case "next": return p => U(Seg(p, .36f, .18f, .66f, .5f, .075f), Seg(p, .66f, .5f, .36f, .82f, .075f));
                case "chev": return p => U(Seg(p, .36f, .22f, .66f, .5f, .08f), Seg(p, .66f, .5f, .36f, .78f, .08f));
                case "home":
                    return p => Sub(U(Poly(p, V(.5f, .9f, .08f, .5f, .92f, .5f)), Box(p, .5f, .33f, .28f, .2f, .02f)), Box(p, .5f, .27f, .07f, .12f, .02f));
                case "gear":
                    return p =>
                    {
                        float d = Sub(Circ(p, .5f, .5f, .32f), Circ(p, .5f, .5f, .13f));
                        for (int k = 0; k < 8; k++) d = U(d, Box(Rot(p, .5f, .5f, k * 45f), .5f, .84f, .075f, .1f, .015f));
                        return d;
                    };
                case "coin": return p => U(Ringd(p, .5f, .5f, .38f, .06f), Poly(p, StarPts(.5f, .49f, .24f, .1f)));
                case "heart":
                    return p => U(U(Circ(p, .335f, .62f, .225f), Circ(p, .665f, .62f, .225f)), Poly(p, V(.13f, .55f, .87f, .55f, .5f, .1f)));
                case "star": return p => Poly(p, StarPts(.5f, .5f, .5f, .22f)) - .0f;
                case "star_o": return p => Outline(Poly(p, StarPts(.5f, .5f, .5f, .22f)), .035f);
                case "lock":
                    return p => U(Box(p, .5f, .35f, .27f, .22f, .05f), Mathf.Max(Ringd(p, .5f, .56f, .17f, .05f), .56f - p.y));
                case "restart":
                    return p =>
                    {
                        var q = p - new Vector2(.5f, .5f); float ang = Mathf.Atan2(q.y, q.x) * Mathf.Rad2Deg;
                        float arc = Mathf.Abs(q.magnitude - .3f) - .075f;
                        if (ang > 20f && ang < 80f) arc = 1f;
                        return U(arc, Poly(p, V(.58f, .98f, .58f, .58f, .96f, .78f)));
                    };
                case "bag":
                    return p => U(Poly(p, V(.18f, .1f, .82f, .1f, .9f, .62f, .1f, .62f)), Mathf.Max(Ringd(p, .5f, .62f, .2f, .05f), .62f - p.y));
                case "ship1": return ShipSdf(0);
                case "ship2": return ShipSdf(1);
                case "ship3": return ShipSdf(2);
                case "check": return p => U(Seg(p, .2f, .5f, .42f, .28f, .075f), Seg(p, .42f, .28f, .82f, .72f, .075f));
                case "close": return p => U(Seg(p, .22f, .22f, .78f, .78f, .075f), Seg(p, .22f, .78f, .78f, .22f, .075f));
                case "speaker":
                    return p => U(U(Box(p, .22f, .5f, .1f, .14f, .02f), Poly(p, V(.28f, .64f, .56f, .84f, .56f, .16f, .28f, .36f))),
                        U(Mathf.Max(Ringd(p, .56f, .5f, .22f, .04f), .57f - p.x), Mathf.Max(Ringd(p, .56f, .5f, .34f, .04f), .57f - p.x)));
                case "plus": return p => U(Box(p, .5f, .5f, .32f, .075f, .02f), Box(p, .5f, .5f, .075f, .32f, .02f));
                case "bolt": return p => Poly(p, V(.58f, .96f, .2f, .46f, .46f, .46f, .36f, .04f, .8f, .58f, .54f, .58f));
                case "info": return p => U(Ringd(p, .5f, .5f, .4f, .045f), U(Box(p, .5f, .4f, .05f, .16f, .02f), Circ(p, .5f, .68f, .06f)));
                case "trophy":
                    return p => U(U(Poly(p, V(.2f, .92f, .8f, .92f, .7f, .5f, .3f, .5f)), Box(p, .5f, .4f, .05f, .1f)),
                        U(Box(p, .5f, .22f, .2f, .06f, .02f), U(Mathf.Max(Ringd(p, .17f, .72f, .1f, .035f), p.x - .2f), Mathf.Max(Ringd(p, .83f, .72f, .1f, .035f), .8f - p.x))));
                case "flag": return p => U(Box(p, .28f, .5f, .045f, .42f, .02f), Poly(p, V(.3f, .88f, .86f, .7f, .3f, .52f)));
                case "clock": return p => U(Ringd(p, .5f, .5f, .38f, .05f), U(Seg(p, .5f, .5f, .5f, .74f, .04f), Seg(p, .5f, .5f, .68f, .42f, .04f)));
                case "infinity": return p => U(Ringd(p, .3f, .5f, .17f, .06f), Ringd(p, .7f, .5f, .17f, .06f));
                case "shield": return p => Poly(p, V(.5f, .94f, .86f, .8f, .82f, .4f, .5f, .06f, .18f, .4f, .14f, .8f));
                case "magnet":
                    return p => U(Mathf.Max(Ringd(p, .5f, .5f, .26f, .095f), p.y - .5f), U(Box(p, .24f, .66f, .095f, .2f, .02f), Box(p, .76f, .66f, .095f, .2f, .02f)));
                case "gem": return p => Poly(p, V(.5f, .92f, .88f, .62f, .5f, .08f, .12f, .62f));
                case "sparkle": return p => Poly(p, StarPts(.5f, .5f, .5f, .14f, 4));
                case "phone": return p => Sub(Box(p, .5f, .5f, .2f, .38f, .06f), Box(p, .5f, .52f, .15f, .29f, .02f));
                case "note": return p => U(U(Circ(p, .34f, .24f, .14f), Box(p, .46f, .55f, .04f, .32f)), Poly(p, V(.46f, .86f, .76f, .72f, .76f, .6f, .46f, .72f)));
                case "bars": return p => U(Box(p, .25f, .3f, .08f, .2f, .02f), U(Box(p, .5f, .45f, .08f, .35f, .02f), Box(p, .75f, .6f, .08f, .5f, .02f)));
                case "user": return p => U(Circ(p, .5f, .68f, .2f), Mathf.Max(Circ(p, .5f, .1f, .38f), p.y - .45f));
                case "dot": return p => Circ(p, .5f, .5f, .3f);
                case "rock": return p => Poly(p, V(.5f, .94f, .86f, .72f, .9f, .32f, .6f, .08f, .22f, .16f, .1f, .56f));
            }
            return p => Circ(p, .5f, .5f, .3f);
        }

        static Func<Vector2, float> ShipSdf(int kind)
        {
            Vector2[] body;
            if (kind == 0) body = Mirror(.5f, .95f, .62f, .64f, .63f, .42f, .93f, .16f, .93f, .07f, .63f, .2f, .5f, .12f);
            else if (kind == 1) body = Mirror(.5f, .92f, .68f, .7f, .76f, .5f, .95f, .38f, .95f, .12f, .7f, .2f, .5f, .1f);
            else body = Mirror(.5f, .97f, .58f, .6f, .6f, .36f, .84f, .1f, .66f, .2f, .56f, .16f, .5f, .06f);
            return p => Sub(Poly(p, body), Circ(p, .5f, kind == 2 ? .6f : .55f, .06f));
        }
    }
}
