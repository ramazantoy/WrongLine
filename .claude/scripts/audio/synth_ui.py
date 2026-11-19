"""Interface sounds, the crash's impact layers and the two game over jingles."""
import numpy as np
from scipy.signal import butter, lfilter
from synthlib import CLIPS, SR, noise, normalize, write_wav


def tone(freq, seconds, decay, harmonics=(1.0, 0.3, 0.1), attack=0.002):
    n = int(SR * seconds)
    t = np.arange(n) / SR
    out = sum(a * np.sin(2 * np.pi * freq * (i + 1) * t) for i, a in enumerate(harmonics))
    return out * np.exp(-t / decay) * np.minimum(1, t / attack)


def band(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], btype="band")
    return lfilter(b, a, x)


def lowpass(x, cutoff):
    b, a = butter(2, cutoff / (SR / 2))
    return lfilter(b, a, x)


def sequence(notes, step, decay, harmonics=(1.0, 0.35, 0.12)):
    total = int(SR * (step * len(notes) + 0.9))
    out = np.zeros(total)
    for i, f in enumerate(notes):
        s = tone(f, 0.9, decay, harmonics)
        start = int(i * step * SR)
        out[start:start + len(s)] += s
    return out


def crash_metal():
    n = int(SR * 1.4)
    t = np.arange(n) / SR
    thud = np.sin(2 * np.pi * (55 + 90 * np.exp(-t * 18)) * t) * np.exp(-t * 6)
    crunch = band(noise(n, 31), 200, 3500) * np.exp(-t * 7)
    ring = sum(np.sin(2 * np.pi * f * t) * np.exp(-t * d) for f, d in ((310, 5), (740, 7), (1330, 9), (2100, 12))) * 0.12
    return normalize(thud * 1.1 + crunch * 0.9 + ring, 0.9)


def crash_glass():
    n = int(SR * 1.1)
    t = np.arange(n) / SR
    rng = np.random.default_rng(32)
    out = np.zeros(n)
    for _ in range(70):
        start = int(rng.exponential(0.12) * SR)
        if start >= n - 800:
            continue
        f = rng.uniform(2500, 8000)
        k = np.arange(800) / SR
        out[start:start + 800] += np.sin(2 * np.pi * f * k) * np.exp(-k * rng.uniform(300, 900)) * rng.uniform(0.2, 1)
    return normalize(out, 0.7)


def main():
    ui = CLIPS / "Sfx"
    write_wav(ui / "Ui_Tap.wav", normalize(tone(880, 0.12, 0.03), 0.7))
    write_wav(ui / "Ui_Back.wav", normalize(tone(520, 0.14, 0.04), 0.7))
    write_wav(ui / "Ui_Tick.wav", normalize(tone(1500, 0.05, 0.01, (1.0, 0.2)), 0.55))
    write_wav(ui / "Ui_Toggle.wav", normalize(sequence([660, 990], 0.05, 0.04), 0.7))
    write_wav(ui / "Crash_Metal.wav", crash_metal())
    write_wav(ui / "Crash_Glass.wav", crash_glass())
    # Game over: three falling notes, minor. Record: a rising major arpeggio.
    write_wav(ui / "Jingle_GameOver.wav", normalize(lowpass(sequence([440, 392, 349, 262], 0.28, 0.5, (1.0, 0.5, 0.2)), 3500), 0.8))
    write_wav(ui / "Jingle_Record.wav", normalize(sequence([523, 659, 784, 1047, 1319], 0.11, 0.45), 0.8))


if __name__ == "__main__":
    main()
