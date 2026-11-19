"""Shared helpers for the game's synthesized sounds (numpy only; WAV via the wave module)."""
import subprocess
import wave
from pathlib import Path

import numpy as np

SR = 44100
ROOT = Path(__file__).resolve().parents[3]
CLIPS = ROOT / "Assets" / "Game" / "Audio" / "Clips"


def periodic_noise(seconds, lo_hz, hi_hz, tilt=0.0, seed=0):
    """Noise that loops perfectly: random-phase sinusoids on whole-Hz bins (the buffer is `seconds` long, seconds integer)."""
    n = int(SR * seconds)
    rng = np.random.default_rng(seed)
    spec = np.zeros(n // 2 + 1, dtype=complex)
    bins = np.arange(int(lo_hz * seconds), int(hi_hz * seconds) + 1)
    freqs = bins / seconds
    amp = (freqs / max(lo_hz, 1.0)) ** tilt
    spec[bins] = amp * np.exp(1j * rng.uniform(0, 2 * np.pi, len(bins)))
    out = np.fft.irfft(spec, n)
    return out / np.max(np.abs(out))


def noise(n, seed=0):
    return np.random.default_rng(seed).uniform(-1, 1, n)


def one_pole_lp(x, cutoff):
    from scipy.signal import lfilter
    a = 1.0 - np.exp(-2 * np.pi * cutoff / SR)
    return lfilter([a], [1.0, a - 1.0], x)


def normalize(x, peak=0.89):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def write_wav(path, data):
    """Write mono or stereo float data (-1..1) as 16-bit WAV."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    data = np.clip(data, -1, 1)
    pcm = (data * 32767).astype("<i2")
    channels = 1 if pcm.ndim == 1 else pcm.shape[1]
    with wave.open(str(path), "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print("wrote", path.relative_to(ROOT), f"({len(data) / SR:.2f}s)")


def write_ogg(path, data, quality=5):
    """Encode through ffmpeg (libvorbis); the WAV is made next to it and removed."""
    path = Path(path)
    tmp = path.with_suffix(".tmp.wav")
    write_wav(tmp, data)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(tmp), "-c:a", "libvorbis", "-q:a", str(quality), str(path)], check=True)
    tmp.unlink()
    print("encoded", path.relative_to(ROOT))
