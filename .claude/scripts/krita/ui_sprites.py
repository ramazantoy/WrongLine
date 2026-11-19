# The UI sprite set. White on transparent; Unity tints them with Image.color. Fixed seeds: a rerun draws the same sprites.
def draw_all():
    # Buttons: wide brush bars, three variants so neighbouring buttons do not look stamped.
    for i, (seed, amp) in enumerate([(11, 3.0), (23, 4.0), (37, 2.5)]):
        sprite("Brush_Button_" + "ABC"[i], 768, 192,
               lambda d, n, v, s=seed, a=amp: bar(n, v, 24, 30, 744, 162, 56, s, amp=a))
    sprite("Brush_Panel", 1024, 1280, lambda d, n, v: bar(n, v, 30, 40, 994, 1240, 120, 5, amp=6))
    sprite("Brush_Underline", 768, 56, lambda d, n, v: bar(n, v, 16, 14, 752, 42, 24, 9, amp=2))
    sprite("Brush_Divider", 768, 28, lambda d, n, v: bar(n, v, 16, 6, 752, 22, 12, 13, amp=1.5, preset=INK3))
    sprite("Slider_Track", 512, 48, lambda d, n, v: bar(n, v, 12, 10, 500, 38, 22, 17, amp=1.5))
    sprite("Slider_Fill", 512, 48, lambda d, n, v: bar(n, v, 12, 8, 500, 40, 28, 19, amp=1.0))
    sprite("Slider_Knob", 112, 112, lambda d, n, v: blob(n, v, 56, 56, 46, 3))
    sprite("Toggle_Box", 112, 112, lambda d, n, v: [
        line_stroke(n, v, [(22, 24), (92, 20), (90, 90), (20, 92), (22, 22)], 12, 5)])
    sprite("Toggle_Check", 112, 112, lambda d, n, v: [
        line_stroke(n, v, [(24, 58), (46, 84), (92, 22)], 16, 7, p=(0.9, 1.0, 0.5))])
    # Icons, 128 px.
    sprite("Icon_Pause", 128, 128, lambda d, n, v: [
        line_stroke(n, v, [(44, 22), (46, 106)], 26, 2, p=(0.9, 1, 0.8)),
        line_stroke(n, v, [(86, 22), (84, 106)], 26, 4, p=(0.9, 1, 0.8))])
    sprite("Icon_Back", 128, 128, lambda d, n, v: [
        line_stroke(n, v, [(84, 20), (40, 64), (84, 108)], 20, 6, p=(0.9, 1, 0.8))])
    sprite("Icon_ArrowLeft", 128, 128, lambda d, n, v: [
        line_stroke(n, v, [(84, 20), (40, 64), (84, 108)], 20, 6, p=(0.9, 1, 0.8))])
    sprite("Icon_ArrowRight", 128, 128, lambda d, n, v: [
        line_stroke(n, v, [(44, 20), (88, 64), (44, 108)], 20, 8, p=(0.9, 1, 0.8))])
    def settings(d, n, v):
        for k, (y, kx) in enumerate([(32, 84), (64, 44), (96, 70)]):
            line_stroke(n, v, [(16, y), (112, y + 1)], 9, 20 + k, p=(0.8, 1, 0.7))
            blob(n, v, kx, y, 15, 30 + k)
    sprite("Icon_Settings", 128, 128, settings)
    sprite("Icon_Close", 128, 128, lambda d, n, v: [
        line_stroke(n, v, [(28, 28), (100, 100)], 18, 1, p=(0.9, 1, 0.8)),
        line_stroke(n, v, [(100, 28), (28, 100)], 18, 2, p=(0.9, 1, 0.8))])

draw_all()


# Logo: drawn from the heading font so it matches the screens; needs FONT_PATH from run_ui.py.
def draw_logo():
    from PyQt5.QtGui import QFontDatabase, QFont, QPainterPath
    from PyQt5.QtCore import QPointF
    def logo(d, n, v):
        fid = QFontDatabase.addApplicationFont(FONT_PATH)
        fam = QFontDatabase.applicationFontFamilies(fid)[0]
        f = QFont(fam); f.setPixelSize(250)
        path = QPainterPath(); path.addText(QPointF(0, 0), f, "WRONGLINE")
        r = path.boundingRect()
        sx = 1000.0 / r.width()
        polys = [[((p.x() - r.x()) * sx + 40, (p.y() - r.y()) * sx + 30) for p in poly] for poly in path.toSubpathPolygons()]
        flat = [pt for poly in polys for pt in poly]
        rs = random.Random(4)
        # fill: dry brush passes clipped to the glyph shapes (even-odd across all subpaths)
        def inside_all(x, y):
            return sum(1 for poly in polys if inside(poly, x, y)) % 2 == 1
        brush(v, INK7, size=14, opacity=1.0, color=WHITE)
        ys = [p[1] for p in flat]; xs = [p[0] for p in flat]
        y = min(ys) + 5
        while y < max(ys):
            cur = []
            x = min(xs)
            while x <= max(xs) + 4:
                if inside_all(x, y): cur.append((x, y + wob(rs, 1.0)))
                elif cur:
                    if len(cur) > 1: stroke(n, [cur[0], cur[-1]], p0=0.8, pmid=1.0, p1=0.8, jitter=0.3, seed=int(y))
                    cur = []
                x += 3
            y += 7
        # outline for crisp edges
        brush(v, INK3, size=6, opacity=1.0, color=WHITE)
        for poly in polys:
            stroke(n, poly + [poly[0]], p0=1.0, pmid=1.0, p1=1.0, jitter=0.4, seed=7)
    sprite("Logo_WrongLine", 1080, 232, logo)
draw_logo()
result = results
