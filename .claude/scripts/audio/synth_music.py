"""Two synthwave loops: a calm menu theme and a driving theme. Notes ring past the loop's end and wrap to its start, so the loop is seamless."""
import numpy as np
from scipy.signal import butter, lfilter
from synthlib import CLIPS, SR, noise, normalize, write_ogg

# Am - F - C - G, one chord per bar: bass root (MIDI), chord tones.
PROGRESSION = [(45, [57, 60, 64]), (41, [57, 60, 65]), (48, [55, 60, 64]), (43, [55, 59, 62])]


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


class Track:
    def __init__(self, bars, bpm):
        self.beat = 60.0 / bpm
        self.bar = self.beat * 4
        self.n = int(round(bars * self.bar * SR))
        self.bars = bars
        self.l = np.zeros(self.n)
        self.r = np.zeros(self.n)

    def add(self, sig, start_seconds, pan=0.0, gain=1.0):
        start = int(start_seconds * SR) % self.n
        idx = (start + np.arange(len(sig))) % self.n
        np.add.at(self.l, idx, sig * gain * (1 - max(pan, 0)))
        np.add.at(self.r, idx, sig * gain * (1 + min(pan, 0)))


def env(n, attack, release):
    t = np.arange(n) / SR
    return np.minimum(1, t / max(attack, 1e-4)) * np.minimum(1, (n / SR - t) / max(release, 1e-4))


def saw(freq, n, detune=(0.0,)):
    t = np.arange(n) / SR
    out = np.zeros(n)
    for d in detune:
        out += 2 * ((t * freq * (1 + d)) % 1.0) - 1
    return out / len(detune)


def lp(x, cutoff):
    b, a = butter(2, cutoff / (SR / 2))
    return lfilter(b, a, x)


def kick():
    n = int(SR * 0.35)
    t = np.arange(n) / SR
    f = 50 + 110 * np.exp(-t * 28)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)


def snare():
    n = int(SR * 0.3)
    t = np.arange(n) / SR
    b, a = butter(2, 1500 / (SR / 2), btype="high")
    return (lfilter(b, a, noise(n, 5)) * 0.8 + np.sin(2 * np.pi * 190 * t) * 0.4) * np.exp(-t * 16)


def hat():
    n = int(SR * 0.08)
    t = np.arange(n) / SR
    b, a = butter(2, 7000 / (SR / 2), btype="high")
    return lfilter(b, a, noise(n, 6)) * np.exp(-t * 55)


def pad(track, chord_notes, start, length, gain):
    for m in chord_notes:
        n = int(length * SR)
        sig = lp(saw(hz(m), n, (-0.006, 0.0, 0.007)), 1400) * env(n, 0.5, 0.8)
        track.add(sig, start, 0.0, gain)


def arp(track, chord_notes, start_bar, steps, gain, cutoff):
    sixteenth = track.beat / 4
    pattern = [0, 1, 2, 1, 0, 2, 1, 2]
    for i in range(steps):
        m = chord_notes[pattern[i % len(pattern)]] + (12 if i % 8 in (3, 7) else 24)
        n = int(0.22 * SR)
        sig = lp(saw(hz(m), n, (-0.004, 0.004)), cutoff) * np.exp(-np.arange(n) / SR * 11)
        track.add(sig, start_bar * track.bar + i * sixteenth, 0.4 if i % 2 else -0.4, gain)


def bass(track, root, bar, gain, eighths):
    for i in range(8):
        if not eighths and i % 4 not in (0, 3):
            continue
        n = int(track.beat / 2 * SR * 0.95)
        sig = lp(saw(hz(root), n), 380) * env(n, 0.004, 0.03)
        track.add(sig, bar * track.bar + i * track.beat / 2, 0.0, gain)


def drums(track, bar, with_hats):
    base = bar * track.bar
    for beat in range(4):
        track.add(kick(), base + beat * track.beat, 0.0, 0.9)
    for beat in (1, 3):
        track.add(snare(), base + beat * track.beat, 0.0, 0.5)
    if with_hats:
        for i in range(8):
            track.add(hat(), base + i * track.beat / 2 + track.beat / 4, 0.3, 0.22)


def build(bars, bpm, drum_bars, arp_gain, pad_gain, bass_gain, eighths, arp_cutoff):
    t = Track(bars, bpm)
    for bar in range(bars):
        root, chord = PROGRESSION[bar % 4]
        pad(t, chord, bar * t.bar, t.bar * 1.15, pad_gain)
        if bar >= 2:
            bass(t, root, bar, bass_gain, eighths)
            arp(t, chord, bar, 16, arp_gain, arp_cutoff)
        if drum_bars and bar >= drum_bars:
            drums(t, bar, with_hats=bar >= drum_bars + 2)
    mix = np.stack([t.l, t.r], axis=1)
    mix = np.tanh(1.2 * mix / np.max(np.abs(mix)))
    return normalize(mix, 0.8)


def main():
    music = CLIPS / "Music"
    write_ogg(music / "Theme_Menu.ogg", build(24, 88, 0, 0.16, 0.22, 0.30, False, 1800))
    write_ogg(music / "Theme_Drive.ogg", build(32, 112, 4, 0.18, 0.14, 0.40, True, 2600))


if __name__ == "__main__":
    main()
