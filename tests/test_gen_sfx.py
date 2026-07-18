import hashlib
import subprocess
import sys
import wave
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sfx"
NAMES = [
    "flap",
    "joust",
    "egg_plop",
    "hatch",
    "collect_0",
    "collect_1",
    "collect_2",
    "collect_3",
    "screech",
    "troll",
    "shimmer",
    "fanfare",
    "death",
    "bonus",
    "hiscore",
]


def run_gen():
    subprocess.run([sys.executable, str(ROOT / "tools" / "gen_sfx.py")], check=True)


def test_all_sfx_exist_valid_and_deterministic():
    run_gen()
    for n in NAMES:
        with wave.open(str(OUT / f"{n}.wav")) as wav:
            assert wav.getframerate() == 22050 and wav.getsampwidth() == 2
            assert 0.05 <= wav.getnframes() / 22050 <= 6.0
    h1 = {n: hashlib.sha256((OUT / f"{n}.wav").read_bytes()).hexdigest() for n in NAMES}
    run_gen()
    assert h1 == {
        n: hashlib.sha256((OUT / f"{n}.wav").read_bytes()).hexdigest() for n in NAMES
    }
