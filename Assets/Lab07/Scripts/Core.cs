using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab07
{
    /// <summary>Palette. Values are planned so that every role stays distinguishable in greyscale (see README).</summary>
    public static class Pal
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static readonly Color BgTop = Hex("#2A1B6B");
        public static readonly Color BgBottom = Hex("#0A0724");
        public static readonly Color Panel = Hex("#241A5E");
        public static readonly Color PanelHi = Hex("#3A2D8F");
        public static readonly Color PanelLo = Hex("#150F3A");
        public static readonly Color Cyan = Hex("#2EE6D6");
        public static readonly Color Amber = Hex("#FFC93C");
        public static readonly Color Pink = Hex("#FF5C93");
        public static readonly Color Ink = Hex("#0B0827");
        public static readonly Color White = Hex("#F6F4FF");
        public static readonly Color Dim = Hex("#B9B2E8");
        public static readonly Color Mute = Hex("#7A72B8");
        public static readonly Color Locked = Hex("#322A6A");
        public static Color A(Color c, float a) { c.a = a; return c; }
    }

    // ------------------------------------------------------------------ easing
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, InCubic, OutCubic, InOutCubic, OutQuart, OutExpo, InBack, OutBack, OutElastic, OutBounce, InOutSine }

    public static class Easing
    {
        public static float Eval(Ease e, float t)
        {
            t = Mathf.Clamp01(t);
            switch (e)
            {
                case Ease.Linear: return t;
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1 - (1 - t) * (1 - t);
                case Ease.InOutQuad: return t < .5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1 - Mathf.Pow(1 - t, 3);
                case Ease.InOutCubic: return t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
                case Ease.OutQuart: return 1 - Mathf.Pow(1 - t, 4);
                case Ease.OutExpo: return t >= 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
                case Ease.InBack: { const float c1 = 1.70158f, c3 = c1 + 1; return c3 * t * t * t - c1 * t * t; }
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
                case Ease.OutElastic:
                    {
                        if (t <= 0) return 0; if (t >= 1) return 1;
                        const float c4 = 2 * Mathf.PI / 3;
                        return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - .75f) * c4) + 1;
                    }
                case Ease.OutBounce:
                    {
                        const float n1 = 7.5625f, d1 = 2.75f;
                        if (t < 1 / d1) return n1 * t * t;
                        if (t < 2 / d1) { t -= 1.5f / d1; return n1 * t * t + .75f; }
                        if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + .9375f; }
                        t -= 2.625f / d1; return n1 * t * t + .984375f;
                    }
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
            }
            return t;
        }
    }

    // ------------------------------------------------------------------ tween engine
    public sealed class TweenItem
    {
        internal object key; internal float delay, dur, t; internal Ease ease;
        internal Action<float> step; internal Action done; internal bool dead, loop, pingpong;
        public void Kill() { dead = true; }
    }

    /// <summary>Tiny tween engine (no third-party dependency). Runs on unscaled time so menus animate while the game is paused.</summary>
    public static class Tw
    {
        public static float FixedDt = 0f; // set by the capture tool for deterministic video frames
        static readonly List<TweenItem> items = new List<TweenItem>();
        static readonly List<TweenItem> pending = new List<TweenItem>();
        static GameObject runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { items.Clear(); pending.Clear(); runner = null; FixedDt = 0; }

        static void Ensure()
        {
            if (runner) return;
            runner = new GameObject("[Tween]");
            runner.hideFlags = HideFlags.HideAndDontSave;
            runner.AddComponent<TweenRunner>();
        }

        public static TweenItem To(Action<float> step, float dur, Ease ease = Ease.OutCubic, float delay = 0f, Action done = null, object key = null)
        {
            Ensure();
            var it = new TweenItem { step = step, dur = Mathf.Max(dur, 0.0001f), ease = ease, delay = delay, done = done, key = key };
            pending.Add(it);
            return it;
        }

        public static TweenItem Loop(Action<float> step, float period, Ease ease = Ease.InOutSine, bool pingpong = true, object key = null, float delay = 0f)
        {
            Ensure();
            var it = new TweenItem { step = step, dur = Mathf.Max(period, 0.0001f), ease = ease, delay = delay, key = key, loop = true, pingpong = pingpong };
            pending.Add(it);
            return it;
        }

        public static void Kill(object key)
        {
            if (key == null) return;
            foreach (var i in items) if (!i.dead && Equals(i.key, key)) i.dead = true;
            foreach (var i in pending) if (!i.dead && Equals(i.key, key)) i.dead = true;
        }

        internal static void Tick(float dt)
        {
            if (pending.Count > 0) { items.AddRange(pending); pending.Clear(); }
            for (int n = 0; n < items.Count; n++)
            {
                var it = items[n];
                if (it.dead) continue;
                if (it.delay > 0f) { it.delay -= dt; if (it.delay > 0f) continue; }
                it.t += dt;
                try
                {
                    if (it.loop)
                    {
                        float u = it.t / it.dur;
                        float k = it.pingpong ? Mathf.PingPong(u, 1f) : u - Mathf.Floor(u);
                        it.step(Easing.Eval(it.ease, k));
                    }
                    else
                    {
                        float k = Mathf.Clamp01(it.t / it.dur);
                        it.step(Easing.Eval(it.ease, k));
                        if (k >= 1f) { it.dead = true; it.done?.Invoke(); }
                    }
                }
                catch (MissingReferenceException) { it.dead = true; }
                catch (Exception ex) { it.dead = true; Debug.LogException(ex); }
            }
            items.RemoveAll(i => i.dead);
        }

        // ---- helpers (every helper kills the previous tween on the same property so taps never fight each other)
        public static TweenItem Move(RectTransform rt, Vector2 to, float dur, Ease e = Ease.OutCubic, float delay = 0f, Action done = null)
        {
            var key = (rt, "pos"); Kill(key); var from = rt.anchoredPosition;
            return To(k => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k), dur, e, delay, done, key);
        }
        public static TweenItem MoveFrom(RectTransform rt, Vector2 from, float dur, Ease e = Ease.OutCubic, float delay = 0f, Action done = null)
        {
            var to = rt.anchoredPosition; rt.anchoredPosition = from; return Move(rt, to, dur, e, delay, done);
        }
        public static TweenItem Scale(Transform tr, float to, float dur, Ease e = Ease.OutBack, float delay = 0f, Action done = null)
        {
            var key = (tr, "scale"); Kill(key); var from = tr.localScale;
            return To(k => tr.localScale = Vector3.LerpUnclamped(from, new Vector3(to, to, 1f), k), dur, e, delay, done, key);
        }
        public static TweenItem ScaleFrom(Transform tr, float from, float to, float dur, Ease e = Ease.OutBack, float delay = 0f, Action done = null)
        {
            tr.localScale = new Vector3(from, from, 1f); return Scale(tr, to, dur, e, delay, done);
        }
        public static TweenItem Alpha(CanvasGroup g, float to, float dur, Ease e = Ease.OutQuad, float delay = 0f, Action done = null)
        {
            var key = (g, "alpha"); Kill(key); var from = g.alpha;
            return To(k => g.alpha = Mathf.LerpUnclamped(from, to, k), dur, e, delay, done, key);
        }
        public static TweenItem AlphaFrom(CanvasGroup g, float from, float to, float dur, Ease e = Ease.OutQuad, float delay = 0f, Action done = null)
        {
            g.alpha = from; return Alpha(g, to, dur, e, delay, done);
        }
        public static TweenItem Tint(UnityEngine.UI.Graphic gr, Color to, float dur, Ease e = Ease.OutQuad, float delay = 0f)
        {
            var key = (gr, "tint"); Kill(key); var from = gr.color;
            return To(k => gr.color = Color.LerpUnclamped(from, to, k), dur, e, delay, null, key);
        }
        public static TweenItem Count(Action<int> set, int from, int to, float dur, Ease e = Ease.OutQuart, float delay = 0f, object key = null)
        {
            if (key != null) Kill(key);
            return To(k => set(Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, k))), dur, e, delay, null, key);
        }
        public static TweenItem Shake(RectTransform rt, float amp = 22f, float dur = 0.42f)
        {
            var key = (rt, "shake"); Kill(key); var basePos = rt.anchoredPosition;
            return To(k => rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * Mathf.PI * 7f) * (1 - k) * amp, 0),
                dur, Ease.Linear, 0f, () => rt.anchoredPosition = basePos, key);
        }
    }

    public class TweenRunner : MonoBehaviour
    {
        void Update() { Tw.Tick(Tw.FixedDt > 0 ? Tw.FixedDt : Time.unscaledDeltaTime); }
    }

    /// <summary>Remembers the designed (rest) anchored position of an element so intro tweens can be replayed safely.</summary>
    public class Rest : MonoBehaviour
    {
        public Vector2 pos; public Vector3 scale = Vector3.one; public bool set;
        public static Rest Of(RectTransform rt)
        {
            var r = rt.GetComponent<Rest>(); if (!r) r = rt.gameObject.AddComponent<Rest>();
            if (!r.set) { r.pos = rt.anchoredPosition; r.scale = rt.localScale; r.set = true; }
            return r;
        }
    }

    public static class Intro
    {
        /// <summary>Staggered rise-and-fade used for list/grid content. Index drives the stagger (hierarchy: first things first).</summary>
        public static void Rise(RectTransform rt, int index, float offset = 70f, float stagger = 0.07f, float dur = 0.5f)
        {
            var r = Rest.Of(rt);
            var cg = rt.GetComponent<CanvasGroup>(); if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            Tw.Kill((rt, "pos"));
            rt.anchoredPosition = r.pos + new Vector2(0, -offset);
            Tw.Move(rt, r.pos, dur, Ease.OutCubic, index * stagger);
            Tw.AlphaFrom(cg, 0, 1, dur * .8f, Ease.OutQuad, index * stagger);
        }
        public static void SlideIn(RectTransform rt, int index, float fromX, float stagger = 0.07f, float dur = 0.5f)
        {
            var r = Rest.Of(rt);
            var cg = rt.GetComponent<CanvasGroup>(); if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            Tw.Kill((rt, "pos"));
            rt.anchoredPosition = r.pos + new Vector2(fromX, 0);
            Tw.Move(rt, r.pos, dur, Ease.OutCubic, index * stagger);
            Tw.AlphaFrom(cg, 0, 1, dur * .7f, Ease.OutQuad, index * stagger);
        }
        public static void Pop(RectTransform rt, int index, float stagger = 0.05f, float dur = 0.45f)
        {
            var r = Rest.Of(rt);
            var cg = rt.GetComponent<CanvasGroup>(); if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            Tw.Kill((rt, "scale"));
            rt.localScale = Vector3.one * .6f;
            Tw.Scale(rt, r.scale.x, dur, Ease.OutBack, index * stagger);
            Tw.AlphaFrom(cg, 0, 1, dur * .6f, Ease.OutQuad, index * stagger);
        }
    }
}
