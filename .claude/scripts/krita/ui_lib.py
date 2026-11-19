# Drawing helpers for the UI sprite set, sent to the Krita bridge after kdraw.py (run_ui.py does this).
# Globals expected from the caller: OUTDIR (where PNGs go), ONLY (list of sprite names or None).
import math, random, os
from PyQt5.QtCore import QRect

WHITE = (255, 255, 255)
INK7 = "d) Ink-7 Brush Rough"
INK3 = "d) Ink-3 Gpen"
DRY = "g) Dry Textured Creases"  # the main brush of the buttons: dry, streaked, ragged ends
results = {}

def wob(rs, amp):
    return rs.uniform(-amp, amp)

def bar(node, view, x0, y0, x1, y1, size, seed, amp=3.0, preset=INK7, col=WHITE):
    """A filled brush bar: overlapping horizontal passes with a drifting centre line, dry tapered ends."""
    brush(view, preset, size=size, opacity=1.0, color=col)
    rs = random.Random(seed)
    n = max(24, int((x1 - x0) / 8))
    y = y0 + size * 0.5
    ye = y1 - size * 0.5
    step = max(size * 0.45, 4)
    passes = []
    while y < ye + step * 0.5:
        passes.append(min(y, ye))
        y += step
    for k, py in enumerate(passes):
        xa = x0 + rs.uniform(0, size * 0.25)
        xb = x1 - rs.uniform(0, size * 0.9)
        drift = rs.uniform(-amp, amp)
        pts = [(xa + (xb - xa) * i / (n - 1), py + drift * math.sin(i / 11.0 + k) + wob(rs, amp * 0.25)) for i in range(n)]
        stroke(node, pts, p0=0.75, pmid=1.0, p1=0.35, jitter=0.5, seed=seed * 31 + k)

def line_stroke(node, view, pts, size, seed, preset=INK7, p=(0.6, 1.0, 0.45), jitter=0.8):
    brush(view, preset, size=size, opacity=1.0, color=WHITE)
    stroke(node, pts, p0=p[0], pmid=p[1], p1=p[2], jitter=jitter, seed=seed)

def blob(node, view, cx, cy, r, seed):
    """Ink drop: a few swirling dabs, filled."""
    rs = random.Random(seed)
    brush(view, INK7, size=r * 1.1, opacity=1.0, color=WHITE)
    for k in range(5):
        rr = r * (0.55 - k * 0.1)
        pts = [(cx + rr * math.cos(a) + wob(rs, 1.5), cy + rr * math.sin(a) + wob(rs, 1.5))
               for a in [2 * math.pi * i / 20 + k for i in range(21)]]
        stroke(node, pts, p0=1.0, pmid=1.0, p1=1.0, jitter=0.5, seed=seed + k)

def save(name, doc, node, w, h):
    doc.refreshProjection(); doc.waitForDone()
    path = os.path.join(OUTDIR, name + ".png")
    node.save(path, 300.0, 300.0, png_info(), QRect(0, 0, w, h))
    results[name] = os.path.getsize(path)

def sprite(name, w, h, fn):
    global view
    if ONLY and name not in ONLY:
        return
    try:
        doc, view = new_doc(w, h, name, bg=None)
        n = layer(doc, name)
        fn(doc, n, view)
        save(name, doc, n, w, h)
        close(doc)
    except Exception:
        import traceback; results[name] = "ERR " + traceback.format_exc()[-500:]

def inside(poly, x, y):
    """Point in polygon (ray casting)."""
    c = False; n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]; x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-9) + x1:
            c = not c
    return c
