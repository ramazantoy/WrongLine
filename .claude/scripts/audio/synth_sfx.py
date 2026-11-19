"""One-shot effects: the whoosh of a car passing, the scrape of a sideswipe, the turn signal's tick and tock."""
import numpy as np
from scipy.signal import butter, lfilter
from synthlib import CLIPS, SR, noise, normalize, write_wav


def bandpass(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], btype="band")
    return lfilter(b, a, x)


def whoosh(seed, seconds=1.1):
    n = int(SR * seconds)
    t = np.linspace(0, 1, n)
    raw = noise(n, seed)
    # The pitch of the rush falls as the car passes: two bands, the lower one lagging.
    hi = bandpass(raw, 900, 5200) * np.exp(-((t - 0.38) ** 2) / 0.018)
    lo = bandpass(raw, 160, 900) * np.exp(-((t - 0.5) ** 2) / 0.03)
    out = hi * 0.8 + lo
    fade = np.minimum(1, np.minimum(t / 0.03, (1 - t) / 0.2))
    return normalize(out * fade, 0.8)


def scrape(seed, seconds=0.55):
    n = int(SR * seconds)
    t = np.arange(n) / SR
    raw = noise(n, seed)
    body = bandpass(raw, 1200, 6500)
    grind = bandpass(raw, 300, 1200) * (0.5 + 0.5 * np.sin(2 * np.pi * 38 * t))
    out = body * 0.7 + grind * 0.6
    env = np.minimum(1, t / 0.01) * np.exp(-t / 0.28)
    return normalize(out * env, 0.85)


def click(freq, seconds, decay):
    n = int(SR * seconds)
    t = np.arange(n) / SR
    tone = np.sin(2 * np.pi * freq * t) + 0.4 * np.sin(2 * np.pi * freq * 2.1 * t)
    tick = bandpass(noise(n, int(freq)), 1500, 6000) * 0.5
    return normalize((tone + tick) * np.exp(-t / decay) * np.minimum(1, t / 0.001), 0.7)


def main():
    out = CLIPS / "Sfx"
    write_wav(out / "PassBy_A.wav", whoosh(11))
    write_wav(out / "PassBy_B.wav", whoosh(12, 0.9))
    write_wav(out / "Scrape_A.wav", scrape(21))
    write_wav(out / "Scrape_B.wav", scrape(22, 0.45))
    write_wav(out / "Signal_Tick.wav", click(2300, 0.06, 0.012))
    write_wav(out / "Signal_Tock.wav", click(1700, 0.07, 0.015))


if __name__ == "__main__":
    main()
