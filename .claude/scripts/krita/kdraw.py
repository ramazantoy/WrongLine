# Helper library loaded into the Krita bridge namespace (persists between calls).
import math, random
from krita import Krita, InfoObject, ManagedColor
from PyQt5.QtCore import QPoint, QPointF, QRectF
from PyQt5.QtGui import QPainterPath, QColor

K = Krita.instance()
K.setBatchmode(True)  # no export/option dialogs (user had to click OK on every save)

def new_doc(w, h, name="claude", bg=(245, 240, 228)):
    doc = K.createDocument(w, h, name, "RGBA", "U8", "", 300.0)
    doc.setBatchmode(True)
    win = K.activeWindow()
    view = win.addView(doc)
    K.setActiveDocument(doc)
    bgl = doc.rootNode().childNodes()[0]
    if bg is not None:
        import struct
        px = bytes([bg[2], bg[1], bg[0], 255]) * (w * h)  # BGRA
        bgl.setPixelData(px, 0, 0, w, h)
    doc.refreshProjection()
    return doc, view

def layer(doc, name):
    n = doc.createNode(name, "paintlayer")
    doc.rootNode().addChildNode(n, None)
    doc.setActiveNode(n)
    return n

def brush(view, preset, size=None, opacity=1.0, color=(30, 30, 40)):
    p = K.resources("preset")[preset]
    view.setCurrentBrushPreset(p)
    if size is not None:
        view.setBrushSize(size)
    view.setPaintingOpacity(opacity)
    c = ManagedColor("RGBA", "U8", "")
    c.setComponents([color[2] / 255, color[1] / 255, color[0] / 255, 1.0])  # BGRA order for RGBA/U8
    view.setForeGroundColor(c)

def stroke(node, pts, p0=0.3, pmid=1.0, p1=0.15, jitter=0.0, seed=0):
    """Polyline as consecutive paintLine segments with a pressure envelope (taper in/out)."""
    rnd = random.Random(seed)
    n = len(pts)
    def pr(i):
        u = i / max(1, n - 1)
        if u < 0.25: return p0 + (pmid - p0) * (u / 0.25)
        if u > 0.75: return pmid + (p1 - pmid) * ((u - 0.75) / 0.25)
        return pmid
    P = [(x + rnd.uniform(-jitter, jitter), y + rnd.uniform(-jitter, jitter)) for x, y in pts]
    for i in range(n - 1):
        a, b = P[i], P[i + 1]
        node.paintLine(QPoint(int(a[0]), int(a[1])), QPoint(int(b[0]), int(b[1])), pr(i), pr(i + 1))

def wave(x0, y0, w, amp, n=60, freq=2.0):
    return [(x0 + w * i / (n - 1), y0 + amp * math.sin(2 * math.pi * freq * i / (n - 1))) for i in range(n)]

def bezier(p0, c0, c1, p1, n=40):
    out = []
    for i in range(n):
        t = i / (n - 1); u = 1 - t
        out.append((u**3 * p0[0] + 3 * u * u * t * c0[0] + 3 * u * t * t * c1[0] + t**3 * p1[0],
                    u**3 * p0[1] + 3 * u * u * t * c0[1] + 3 * u * t * t * c1[1] + t**3 * p1[1]))
    return out

def export(doc, path):
    doc.refreshProjection()
    doc.waitForDone()
    return doc.exportImage(path, png_info())

def png_info():
    """explicit PNG options so Krita never needs to ask"""
    i = InfoObject()
    for k, v in {"alpha": True, "compression": 6, "forceSRGB": False, "indexed": False, "interlaced": False,
                 "saveSRGBProfile": False, "transparencyFillcolor": [255, 255, 255]}.items():
        i.setProperty(k, v)
    return i

def close(doc):
    doc.setModified(False)
    doc.close()

result = "kdraw loaded"
