"""Send Python to the Krita bridge. Usage: python k.py file.py  |  python k.py -c "code" """
import sys, json, urllib.request
code = sys.argv[2] if sys.argv[1] == "-c" else open(sys.argv[1], encoding="utf-8").read()
req = urllib.request.Request("http://127.0.0.1:8765/", data=code.encode("utf-8"), method="POST")
r = json.loads(urllib.request.urlopen(req, timeout=600).read())
if r.get("out"): print(r["out"], end="")
if r.get("error"): print("ERROR:\n" + r["error"]); sys.exit(1)
if r.get("result") is not None: print(json.dumps(r["result"], ensure_ascii=False, indent=1))
