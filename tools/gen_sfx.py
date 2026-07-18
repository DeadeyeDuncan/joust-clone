"""Generate deterministic arcade-style sound effects for the Joust clone."""

from __future__ import annotations

import wave
from pathlib import Path

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sfx"
SAMPLE_RATE = 22050
MAX_PEAK = 0.9


def timebase(duration: float) -> np.ndarray:
    return np.arange(round(duration * SAMPLE_RATE), dtype=np.float64) / SAMPLE_RATE


def oscillator(frequency: float | np.ndarray, duration: float, shape: str = "sine") -> np.ndarray:
    frequency = np.broadcast_to(frequency, timebase(duration).shape)
    phase = 2.0 * np.pi * np.cumsum(frequency) / SAMPLE_RATE
    sine = np.sin(phase)
    if shape == "square":
        return np.where(sine >= 0.0, 1.0, -1.0)
    if shape == "triangle":
        return 2.0 / np.pi * np.arcsin(sine)
    if shape == "saw":
        return 2.0 * ((phase / (2.0 * np.pi) + 0.5) % 1.0) - 1.0
    return sine


def envelope(duration: float, attack: float = 0.008, release: float = 0.08) -> np.ndarray:
    t = timebase(duration)
    gain = np.minimum(t / max(attack, 1.0 / SAMPLE_RATE), 1.0)
    gain *= np.minimum((duration - t) / max(release, 1.0 / SAMPLE_RATE), 1.0)
    return np.clip(gain, 0.0, 1.0)


def decay(duration: float, rate: float, attack: float = 0.002) -> np.ndarray:
    t = timebase(duration)
    return np.minimum(t / max(attack, 1.0 / SAMPLE_RATE), 1.0) * np.exp(-rate * t)


def noise(duration: float, seed: int) -> np.ndarray:
    return np.random.default_rng(seed).uniform(-1.0, 1.0, len(timebase(duration)))


def sweep(start: float, end: float, duration: float, shape: str = "sine") -> np.ndarray:
    return oscillator(np.linspace(start, end, len(timebase(duration))), duration, shape)


def note(frequency: float, duration: float, shape: str = "triangle") -> np.ndarray:
    body = 0.72 * oscillator(frequency, duration, shape)
    body += 0.28 * oscillator(frequency * 2.0, duration, "square")
    return body * envelope(duration, attack=0.006, release=min(0.09, duration / 2.0))


def sequence(notes: list[tuple[float, float]], gap: float = 0.0) -> np.ndarray:
    parts: list[np.ndarray] = []
    for frequency, duration in notes:
        parts.append(note(frequency, duration))
        if gap:
            parts.append(np.zeros(round(gap * SAMPLE_RATE)))
    return np.concatenate(parts)


def flap() -> np.ndarray:
    duration = 0.24
    thump = 0.78 * sweep(125.0, 48.0, duration) * decay(duration, 13.0, 0.001)
    air = 0.22 * noise(duration, 101) * decay(duration, 20.0, 0.001)
    return (thump + air) * envelope(duration, attack=0.001, release=0.045)


def joust() -> np.ndarray:
    duration = 0.52
    t = timebase(duration)
    clang = sum(
        weight * np.sin(2.0 * np.pi * frequency * t) * np.exp(-rate * t)
        for frequency, weight, rate in (
            (515.0, 0.62, 5.5),
            (793.0, 0.48, 6.8),
            (1197.0, 0.34, 8.1),
            (1763.0, 0.22, 10.0),
        )
    )
    strike = 0.24 * noise(duration, 202) * np.exp(-55.0 * t)
    return (clang + strike) * envelope(duration, attack=0.001, release=0.08)


def egg_plop() -> np.ndarray:
    duration = 0.30
    tone = sweep(245.0, 72.0, duration) * decay(duration, 10.0, 0.001)
    click = 0.18 * noise(duration, 303) * decay(duration, 42.0, 0.001)
    return (tone + click) * envelope(duration, attack=0.001, release=0.05)


def hatch() -> np.ndarray:
    duration = 0.48
    t = timebase(duration)
    cracks = noise(duration, 404) * (
        0.55 * np.exp(-90.0 * np.maximum(t - 0.02, 0.0)) * (t >= 0.02)
        + 0.42 * np.exp(-105.0 * np.maximum(t - 0.15, 0.0)) * (t >= 0.15)
    )
    chirp = 0.55 * sweep(380.0, 920.0, duration, "triangle") * envelope(
        duration, attack=0.16, release=0.09
    )
    return cracks + chirp


def collect(index: int) -> np.ndarray:
    duration = 0.19
    base = (440.0, 554.37, 659.25, 880.0)[index]
    tone = 0.72 * sweep(base, base * 1.18, duration, "square")
    sparkle = 0.28 * oscillator(base * 2.0, duration, "triangle")
    return (tone + sparkle) * decay(duration, 7.0, 0.003) * envelope(
        duration, attack=0.002, release=0.045
    )


def screech() -> np.ndarray:
    duration = 0.66
    t = timebase(duration)
    carrier = np.linspace(1280.0, 2180.0, len(t)) + 95.0 * np.sin(2.0 * np.pi * 22.0 * t)
    voice = 0.68 * oscillator(carrier, duration, "saw")
    voice += 0.32 * oscillator(carrier * 1.51, duration, "square")
    return voice * envelope(duration, attack=0.012, release=0.12)


def troll() -> np.ndarray:
    duration = 0.82
    t = timebase(duration)
    pitch = np.linspace(82.0, 43.0, len(t)) + 5.0 * np.sin(2.0 * np.pi * 9.0 * t)
    growl = 0.62 * oscillator(pitch, duration, "saw")
    growl += 0.28 * oscillator(pitch * 2.03, duration, "square")
    growl += 0.10 * noise(duration, 505)
    return growl * envelope(duration, attack=0.025, release=0.18)


def shimmer() -> np.ndarray:
    duration = 0.92
    t = timebase(duration)
    tones = sum(
        np.sin(2.0 * np.pi * frequency * t + phase)
        for frequency, phase in ((880.0, 0.0), (1108.73, 0.7), (1318.51, 1.4), (1760.0, 2.1))
    )
    pulse = 0.65 + 0.35 * np.sin(2.0 * np.pi * 8.0 * t) ** 2
    return tones * 0.25 * pulse * envelope(duration, attack=0.025, release=0.22)


def fanfare() -> np.ndarray:
    return sequence([(523.25, 0.25), (659.25, 0.25), (783.99, 0.25), (1046.5, 0.62)], 0.025)


def death() -> np.ndarray:
    duration = 1.12
    t = timebase(duration)
    fall = 0.7 * sweep(520.0, 62.0, duration, "square")
    rumble = 0.3 * sweep(150.0, 38.0, duration, "saw")
    return (fall + rumble) * envelope(duration, attack=0.006, release=0.24) * np.exp(-0.7 * t)


def bonus() -> np.ndarray:
    return sequence([(659.25, 0.16), (880.0, 0.16), (1318.51, 0.34)], 0.018)


def hiscore() -> np.ndarray:
    phrase = [(523.25, 0.18), (659.25, 0.18), (783.99, 0.18), (1046.5, 0.28)]
    phrase += [(783.99, 0.16), (1046.5, 0.16), (1318.51, 0.48)]
    return sequence(phrase, 0.02)


def write_wav(name: str, samples: np.ndarray) -> None:
    peak = float(np.max(np.abs(samples)))
    if peak:
        samples = samples * (MAX_PEAK / peak)
    pcm = np.round(np.clip(samples, -MAX_PEAK, MAX_PEAK) * 32767.0).astype("<i2")
    with wave.open(str(OUT / f"{name}.wav"), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    sounds = {
        "flap": flap(),
        "joust": joust(),
        "egg_plop": egg_plop(),
        "hatch": hatch(),
        **{f"collect_{index}": collect(index) for index in range(4)},
        "screech": screech(),
        "troll": troll(),
        "shimmer": shimmer(),
        "fanfare": fanfare(),
        "death": death(),
        "bonus": bonus(),
        "hiscore": hiscore(),
    }
    for name, samples in sounds.items():
        write_wav(name, samples)


if __name__ == "__main__":
    main()
