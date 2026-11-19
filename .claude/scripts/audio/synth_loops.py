"""Engine and wind loops. Every partial sits on a whole-Hz bin of a 1 or 2 second buffer, so the loops are seamless.
The game pitches them up with the car's speed (AudioSource.pitch), so they are written low and dark."""
import numpy as np
from synthlib import CLIPS, SR, normalize, periodic_noise, write_wav


def lowpass_periodic(x, cutoff_hz):
    """Spectral low-pass: the loop stays seamless because the buffer is a whole number of seconds."""
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / SR)
    spec *= 1.0 / (1.0 + (freqs / cutoff_hz) ** 4)
    return np.fft.irfft(spec, len(x))


def engine(fundamental, harmonics, rolloff, wobble_hz, wobble_depth, noise_level, cutoff, sub, seed):
    """A deep, thick engine: a low firing frequency, a steep harmonic fall-off, a sub octave and a hard low-pass (no buzz)."""
    seconds = 1
    t = np.arange(SR * seconds) / SR
    rng = np.random.default_rng(seed)
    out = np.zeros_like(t)
    for h in range(1, harmonics + 1):
        amp = 1.0 / (h ** rolloff)
        out += amp * np.sin(2 * np.pi * fundamental * h * t + rng.uniform(0, 2 * np.pi))
    out += sub * np.sin(2 * np.pi * (fundamental // 2) * t)
    # Uneven firing: a slow amplitude wobble on whole-Hz frequencies.
    out *= 1.0 + wobble_depth * np.sin(2 * np.pi * wobble_hz * t)
    out *= 1.0 + 0.5 * wobble_depth * np.sin(2 * np.pi * (wobble_hz * 2 + 1) * t + 1.0)
    # Gentle soft clip for body, then the low-pass takes the fizz off.
    out = np.tanh(1.2 * out / np.max(np.abs(out)))
    out += noise_level * periodic_noise(seconds, 40, 600, tilt=-1.5, seed=seed)
    return normalize(lowpass_periodic(out, cutoff))


def main():
    write_wav(CLIPS / "Engine" / "Engine_Low.wav", engine(30, 10, 1.5, 4, 0.20, 0.06, 520, 0.8, 1))
    write_wav(CLIPS / "Engine" / "Engine_High.wav", engine(50, 12, 1.2, 7, 0.12, 0.08, 900, 0.5, 2))
    wind = periodic_noise(2, 80, 5000, tilt=-0.8, seed=3)
    write_wav(CLIPS / "Engine" / "Wind_Loop.wav", normalize(wind, 0.7))


if __name__ == "__main__":
    main()
