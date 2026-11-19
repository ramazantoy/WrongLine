"""Draw the UI sprite set through the Krita bridge. Usage: python run_ui.py <out dir> [sprite name ...]
Krita 5.3+ must be running with the bridge plugin (see the repo notes); the sprites land in <out dir> as PNG."""
import sys, os, json, urllib.request

here = os.path.dirname(os.path.abspath(__file__))

def send(code):
    req = urllib.request.Request("http://127.0.0.1:8765/", data=code.encode("utf-8"), method="POST")
    r = json.loads(urllib.request.urlopen(req, timeout=900).read())
    if r.get("error"):
        print("ERROR:\n" + r["error"]); sys.exit(1)
    return r.get("result")

out = os.path.abspath(sys.argv[1]); only = sys.argv[2:]
os.makedirs(out, exist_ok=True)
read = lambda f: open(os.path.join(here, f), encoding="utf-8").read()
send(read("kdraw.py"))
font = os.path.abspath(os.path.join(here, "..", "..", "..", "Assets", "Game", "UI", "Fonts", "BarlowCondensed-ExtraBold.ttf"))
send("OUTDIR = %r\nONLY = %r\nFONT_PATH = %r\n" % (out, only or None, font) + read("ui_lib.py"))
result = send(read("ui_sprites.py"))
print(json.dumps(result, indent=1))

# Sprites that are used centred (buttons, underline, divider) are shifted sideways so the visible brush has the same margin on both sides.
CENTRED = ("Brush_Button_", "Brush_Card", "Brush_Underline", "Brush_Divider")
try:
    from PIL import Image
    import numpy as np
except ImportError:
    sys.exit(0)
for name in result:
    if not name.startswith(CENTRED):
        continue
    path = os.path.join(out, name + ".png")
    img = Image.open(path).convert("RGBA")
    alpha = np.asarray(img)[..., 3]
    cols = np.where(alpha.max(axis=0) > 12)[0]
    shift = ((img.width - 1 - cols.max()) - cols.min()) // 2
    if shift:
        shifted = Image.new("RGBA", img.size, (0, 0, 0, 0))
        shifted.paste(img, (shift, 0))
        shifted.save(path)
    print("%s centred by %d px" % (name, shift))

# The card is big, so a lopsided edge shows: its left half is mirrored onto the right.
for name in result:
    if not name.startswith("Brush_Card"):
        continue
    path = os.path.join(out, name + ".png")
    img = Image.open(path).convert("RGBA")
    half = img.crop((0, 0, img.width // 2, img.height))
    img.paste(half.transpose(Image.FLIP_LEFT_RIGHT), (img.width - half.width, 0))
    img.save(path)
    print("%s mirrored" % name)
