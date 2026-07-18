import json, subprocess, sys, hashlib
from pathlib import Path

from tools import gen_sprites

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sprites"

REQUIRED = (
    ["p1_stand", "p1_brake", "p2_stand", "p2_brake", "egg_crack", "ptero_mouth",
     "troll_rise", "troll_grab", "troll_drag", "plat_tile", "plat_edge_l",
     "plat_edge_r", "logo", "font_SP"]
    + [f"p{p}_run_{i}" for p in (1, 2) for i in range(4)]
    + [f"p{p}_flap_{i}" for p in (1, 2) for i in range(3)]
    + [f"buzzard{t}_flap_{i}" for t in (1, 2, 3) for i in range(3)]
    + [f"buzzard{t}_glide" for t in (1, 2, 3)]
    + [f"buzzard_free_{i}" for i in range(2)]
    + [f"hatchling_{i}" for i in range(2)] + [f"egg_{i}" for i in range(2)]
    + [f"ptero_fly_{i}" for i in range(2)] + [f"plat_burn_{i}" for i in range(3)]
    + [f"lava_{i}" for i in range(4)] + [f"shimmer_{i}" for i in range(3)]
    + [f"font_{c}" for c in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.:->"]
)

def run_gen():
    subprocess.run([sys.executable, str(ROOT / "tools" / "gen_sprites.py")], check=True)

def test_generator_emits_complete_manifest(tmp_path):
    run_gen()
    m = json.loads((OUT / "sprites.json").read_text())
    from PIL import Image
    sizes = {s: Image.open(OUT / p).size for s, p in m["sheets"].items()}
    for name in REQUIRED:
        assert name in m["frames"], f"missing frame {name}"
        f = m["frames"][name]
        assert (OUT / m["sheets"][f["sheet"]]).exists()
        x, y, w, h = f["rect"]
        assert w > 0 and h > 0
        sw, sh = sizes[f["sheet"]]
        assert x >= 0 and y >= 0 and x + w <= sw and y + h <= sh, f"{name} rect out of sheet bounds"
    for p in (1, 2):
        assert m["frames"][f"p{p}_stand"]["lance"] is not None
    assert m["frames"]["egg_0"]["lance"] is None
    for tier in (1, 2, 3):
        assert m["frames"][f"buzzard{tier}_flap_0"]["lance"] is None

def test_generator_deterministic():
    run_gen()
    h1 = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.iterdir()}
    run_gen()
    h2 = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.iterdir()}
    assert h1 == h2


def test_buzzard_riders_use_tier_accents_and_free_birds_are_bare():
    accents = set(gen_sprites.ENEMY_RIDER_ACCENTS.values())
    for tier in (1, 2, 3):
        accent = gen_sprites.ENEMY_RIDER_ACCENTS[tier]
        for phase in range(3):
            assert accent in gen_sprites.buzzard_frame(
                tier, "flap", phase
            ).get_flattened_data()
        assert accent in gen_sprites.buzzard_frame(
            tier, "glide"
        ).get_flattened_data()

    for phase in range(2):
        assert accents.isdisjoint(
            set(gen_sprites.free_buzzard_frame(phase).get_flattened_data())
        )
