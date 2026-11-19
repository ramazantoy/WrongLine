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
print(json.dumps(send(read("ui_sprites.py")), indent=1))
