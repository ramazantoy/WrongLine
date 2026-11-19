"""Engine and wind loops. Every partial sits on a whole-Hz bin of a 1 or 2 second buffer, so the loops are seamless.
The game pitches them up with the car's speed (AudioSource.pitch), so they are written low and dark."""
import numpy as np
from synthlib import CLIPS, SR, normalize, periodic_noise, write_wav


def engine(fundamental, harmonics, rolloff, wobble_hz, wobble_depth, noise_level, seed):
    seconds = 1
    t = np.arange(SR * seconds) / SR
    rng = np.random.default_rng(seed)
    out = np.zeros_like(t)
    for h in range(1, harmonics + 1):
        amp = 1.0 / (h ** rolloff)
        out += amp * np.sin(2 * np.pi * fundamental * h * t + rng.uniform(0, 2 * np.pi))
    # Uneven firing: a slow amplitude wobble on whole-Hz frequencies.
    out *= 1.0 + wobble_depth * np.sin(2 * np.pi * wobble_hz * t)
    out *= 1.0 + 0.5 * wobble_depth * np.sin(2 * np.pi * (wobble_hz * 2 + 1) * t + 1.0)
    # Soft clip adds the exhaust's odd harmonics.
    out = np.tanh(1.8 * out / np.max(np.abs(out)))
    out += noise_level * periodic_noise(seconds, 60, 1800, tilt=-1.0, seed=seed)
    return normalize(out)


def main():
    write_wav(CLIPS / "Engine" / "Engine_Low.wav", engine(48, 14, 1.15, 6, 0.18, 0.10, 1))
    write_wav(CLIPS / "Engine" / "Engine_High.wav", engine(96, 18, 0.85, 12, 0.10, 0.16, 2))
    wind = periodic_noise(2, 80, 5000, tilt=-0.8, seed=3)
    write_wav(CLIPS / "Engine" / "Wind_Loop.wav", normalize(wind, 0.7))


if __name__ == "__main__":
    main()
