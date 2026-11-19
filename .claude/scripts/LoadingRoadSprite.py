"""Turns the loading screen's road picture (white brush on dark gray) into a white sprite with alpha.
Usage: python LoadingRoadSprite.py <source image> <output png> [crop top y]
The dark ground becomes transparent, the brush keeps its soft edges; the lower part of the picture is kept."""
import sys
from PIL import Image
import numpy as np

src, out = sys.argv[1], sys.argv[2]
top = int(sys.argv[3]) if len(sys.argv) > 3 else 0
im = Image.open(src).convert("L")
w, h = im.size
a = np.asarray(im.crop((0, top, w, h)), dtype=np.float32)
ground = float(np.percentile(a, 40))            # the flat gray ground
alpha = np.clip((a - (ground + 14.0)) / (225.0 - (ground + 14.0)), 0.0, 1.0)
alpha = np.where(alpha < 0.04, 0.0, alpha)      # JPEG noise in the ground stays out
rgba = np.zeros(alpha.shape + (4,), dtype=np.uint8)
rgba[..., :3] = 255
rgba[..., 3] = (alpha * 255.0 + 0.5).astype(np.uint8)
Image.fromarray(rgba, "RGBA").save(out)
print("ground", ground, "size", rgba.shape[1], rgba.shape[0], "opaque px", int((rgba[..., 3] > 0).sum()))
