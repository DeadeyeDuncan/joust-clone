"""Generated sprite loading with one-shot regeneration on stale output."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pygame


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "assets"

REQUIRED = (
    ["p1_stand", "p1_brake", "p2_stand", "p2_brake", "egg_crack", "ptero_mouth",
     "troll_rise", "troll_grab", "troll_drag", "plat_tile", "plat_edge_l",
     "plat_edge_r", "logo", "font_SP"]
    + [f"p{player}_run_{index}" for player in (1, 2) for index in range(4)]
    + [f"p{player}_flap_{index}" for player in (1, 2) for index in range(3)]
    + [f"buzzard{tier}_flap_{index}" for tier in (1, 2, 3) for index in range(3)]
    + [f"buzzard{tier}_glide" for tier in (1, 2, 3)]
    + [f"buzzard_free_{index}" for index in range(2)]
    + [f"hatchling_{index}" for index in range(2)]
    + [f"egg_{index}" for index in range(2)]
    + [f"ptero_fly_{index}" for index in range(2)]
    + [f"plat_burn_{index}" for index in range(3)]
    + [f"lava_{index}" for index in range(4)]
    + [f"shimmer_{index}" for index in range(3)]
    + [f"font_{char}" for char in "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.:->"]
)
REQUIRED_SFX = (
    "flap", "joust", "egg_plop", "hatch", "collect_0", "collect_1",
    "collect_2", "collect_3", "screech", "troll", "shimmer", "fanfare",
    "death", "bonus", "hiscore",
)


class AssetError(RuntimeError):
    pass


class Assets:
    def __init__(self, frames, anchors, lances):
        self._frames = frames
        self._anchors = anchors
        self._lances = lances

    def frame(self, name):
        return self._frames[name]

    def anchor(self, name):
        return self._anchors[name]

    def lance(self, name):
        return self._lances[name]


def regenerate():
    for tool in ("gen_sprites.py", "gen_sfx.py"):
        subprocess.run([sys.executable, str(ROOT / "tools" / tool)], check=True)


def _read():
    sprite_dir = ASSET_DIR / "sprites"
    sfx_dir = ASSET_DIR / "sfx"
    if any(not (sfx_dir / f"{name}.wav").is_file() for name in REQUIRED_SFX):
        raise OSError("required sound missing")
    manifest = json.loads((sprite_dir / "sprites.json").read_text(encoding="utf-8"))
    definitions = manifest["frames"]
    if any(name not in definitions for name in REQUIRED):
        raise ValueError("required sprite frame missing")

    sheets = {
        name: pygame.image.load(str(sprite_dir / filename))
        for name, filename in manifest["sheets"].items()
    }
    frames = {}
    anchors = {}
    lances = {}
    for name, definition in definitions.items():
        sheet = sheets[definition["sheet"]]
        rect = pygame.Rect(definition["rect"])
        frames[name] = sheet.subsurface(rect).copy()
        anchors[name] = tuple(definition["anchor"])
        lance = definition.get("lance")
        lances[name] = tuple(lance) if lance is not None else None
    return Assets(frames, anchors, lances)


def load():
    for attempt in range(2):
        try:
            return _read()
        except (
            OSError,
            AttributeError,
            KeyError,
            TypeError,
            ValueError,
            json.JSONDecodeError,
            pygame.error,
        ) as error:
            if attempt == 0:
                try:
                    regenerate()
                except (OSError, subprocess.SubprocessError):
                    pass
                continue
            raise AssetError("Joust assets are missing or stale after regeneration.") from error
    raise AssetError("Joust assets are missing or stale after regeneration.")
