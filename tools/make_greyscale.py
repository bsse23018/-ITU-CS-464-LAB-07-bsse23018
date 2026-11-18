# Usage: python tools/make_greyscale.py docs/screens
# For every NN-name-colour.png in the folder, writes NN-name-greyscale.png (luminance, saturation 0).
import sys, glob, os
from PIL import Image
folder = sys.argv[1] if len(sys.argv) > 1 else "docs/screens"
for f in sorted(glob.glob(os.path.join(folder, "*-colour.png"))):
    out = f.replace("-colour.png", "-greyscale.png")
    Image.open(f).convert("L").save(out)
    print("wrote", out)
