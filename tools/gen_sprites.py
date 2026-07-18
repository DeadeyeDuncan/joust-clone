"""Generate the complete deterministic sprite set for the Joust clone.

All artwork is authored at native pixel resolution.  Pillow never resizes or
filters an image here, which keeps every silhouette and highlight hard-edged.
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets" / "sprites"

RGBA = tuple[int, int, int, int]
BLACK: RGBA = (12, 10, 16, 255)
INK: RGBA = (35, 22, 35, 255)
TRANSPARENT: RGBA = (0, 0, 0, 0)


def canvas(size: tuple[int, int]) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", size, TRANSPARENT)
    return image, ImageDraw.Draw(image)


def mounted_frame(player: int, action: str, phase: int = 0) -> Image.Image:
    """Draw a knight and ostrich with a readable, lance-forward silhouette."""
    image, d = canvas((56, 44))
    bird = (
        ((72, 31, 27, 255), (151, 76, 39, 255), (229, 145, 61, 255))
        if player == 1
        else ((29, 48, 75, 255), (49, 109, 145, 255), (103, 191, 205, 255))
    )
    armor = (
        ((83, 46, 25, 255), (192, 113, 36, 255), (255, 210, 91, 255))
        if player == 1
        else ((31, 43, 71, 255), (76, 103, 151, 255), (167, 205, 224, 255))
    )
    plume = (218, 46, 45, 255) if player == 1 else (63, 111, 221, 255)

    # Tail and the rear-most leg establish direction before interior detail.
    tail = [(11, 25), (3, 20), (8, 28), (2, 31), (13, 32)]
    d.polygon(tail, fill=BLACK)
    d.polygon([(10, 26), (5, 22), (9, 29), (5, 30), (14, 31)], fill=bird[1])
    d.line([(7, 24), (12, 28)], fill=bird[2], width=1)

    leg_sets = {
        ("stand", 0): ((17, 34, 16, 41), (29, 34, 31, 41)),
        ("brake", 0): ((18, 34, 11, 40), (29, 34, 37, 39)),
        ("run", 0): ((17, 34, 11, 41), (29, 34, 34, 40)),
        ("run", 1): ((18, 34, 18, 42), (29, 34, 30, 39)),
        ("run", 2): ((17, 34, 23, 40), (29, 34, 25, 42)),
        ("run", 3): ((17, 34, 13, 39), (29, 34, 37, 41)),
        ("flap", 0): ((18, 34, 15, 39), (29, 34, 32, 39)),
        ("flap", 1): ((18, 34, 20, 39), (29, 34, 27, 39)),
        ("flap", 2): ((18, 34, 16, 39), (29, 34, 31, 39)),
    }
    legs = leg_sets[(action, phase)]
    for x1, y1, x2, y2 in legs:
        d.line([(x1, y1), (x2, y2)], fill=BLACK, width=3)
        d.line([(x1, y1), (x2, y2 - 1)], fill=bird[2], width=1)
        d.line([(x2 - 2, y2), (x2 + 3, y2)], fill=BLACK, width=2)
        d.line([(x2, y2 - 1), (x2 + 3, y2 - 1)], fill=bird[1], width=1)

    # Ostrich body: black shell, dark belly, mid-tone mass, upper-left gloss.
    d.ellipse((9, 20, 38, 36), fill=BLACK)
    d.ellipse((11, 22, 36, 34), fill=bird[1])
    d.polygon([(12, 29), (18, 34), (33, 34), (36, 29)], fill=bird[0])
    d.ellipse((14, 22, 29, 27), fill=bird[2])
    d.rectangle((16, 22, 23, 22), fill=(255, 194, 88, 255) if player == 1 else (151, 225, 225, 255))

    # Wing positions carry the flap animation without changing body registration.
    if action == "flap":
        wing_shapes = (
            [(17, 25), (9, 13), (15, 12), (26, 25)],
            [(16, 25), (8, 25), (13, 32), (28, 29)],
            [(18, 25), (13, 35), (23, 34), (30, 27)],
        )
        inner_shapes = (
            [(17, 24), (11, 15), (15, 15), (23, 25)],
            [(16, 26), (11, 26), (15, 30), (25, 28)],
            [(19, 26), (15, 33), (22, 32), (27, 27)],
        )
        d.polygon(wing_shapes[phase], fill=BLACK)
        d.polygon(inner_shapes[phase], fill=bird[1])
        d.line(inner_shapes[phase][0:2], fill=bird[2], width=1)
    else:
        wing = [(18, 24), (31, 25), (29, 33), (17, 31), (13, 27)]
        if action == "brake":
            wing = [(20, 24), (12, 15), (9, 19), (15, 31), (28, 31)]
        d.polygon(wing, fill=BLACK)
        inset = [(19, 25), (29, 26), (27, 31), (17, 29), (15, 27)]
        if action == "brake":
            inset = [(19, 24), (12, 18), (11, 20), (16, 29), (25, 30)]
        d.polygon(inset, fill=bird[0])
        d.line(inset[:3], fill=bird[2], width=1)

    # Long neck and alert head form the recognizable ostrich profile.
    d.line([(34, 27), (39, 14), (43, 12)], fill=BLACK, width=6)
    d.line([(34, 26), (39, 15), (43, 13)], fill=bird[1], width=3)
    d.line([(37, 23), (41, 14)], fill=bird[2], width=1)
    d.ellipse((39, 8, 48, 16), fill=BLACK)
    d.ellipse((40, 9, 47, 14), fill=bird[1])
    d.rectangle((41, 9, 44, 10), fill=bird[2])
    d.polygon([(47, 11), (53, 13), (47, 14)], fill=BLACK)
    d.polygon([(47, 12), (51, 13), (47, 13)], fill=(239, 174, 55, 255))
    d.point((45, 11), fill=(242, 242, 213, 255))
    d.point((46, 11), fill=BLACK)

    # Saddle, boot, torso and helmet.  A bright top-left rim sells metal.
    d.rectangle((20, 18, 32, 23), fill=BLACK)
    d.rectangle((22, 19, 30, 21), fill=(118, 48, 37, 255))
    d.polygon([(20, 10), (29, 10), (32, 21), (21, 21)], fill=BLACK)
    d.polygon([(22, 11), (28, 11), (30, 19), (22, 19)], fill=armor[1])
    d.line([(23, 11), (27, 11), (29, 15)], fill=armor[2], width=1)
    d.polygon([(22, 19), (30, 19), (34, 24), (30, 26), (27, 22), (22, 22)], fill=BLACK)
    d.polygon([(24, 20), (29, 20), (32, 24), (30, 24), (27, 21), (24, 21)], fill=armor[0])
    d.ellipse((20, 4, 29, 13), fill=BLACK)
    d.rectangle((21, 6, 28, 11), fill=armor[1])
    d.rectangle((22, 5, 27, 7), fill=armor[2])
    d.rectangle((25, 8, 30, 10), fill=BLACK)
    d.rectangle((27, 8, 30, 8), fill=armor[2])
    d.polygon([(23, 4), (24, 0), (27, 4)], fill=BLACK)
    d.polygon([(24, 3), (25, 1), (26, 4)], fill=plume)

    # Round shield gives each player a second bold color read.
    d.ellipse((15, 13, 23, 23), fill=BLACK)
    d.ellipse((17, 15, 21, 21), fill=plume)
    d.line([(17, 16), (20, 16)], fill=armor[2], width=1)

    # The lance is drawn last so the hand-to-tip line stays clean and readable.
    d.line([(27, 16), (52, 5)], fill=BLACK, width=3)
    d.line([(27, 15), (52, 4)], fill=(219, 155, 71, 255), width=1)
    d.polygon([(51, 3), (56, 2), (53, 7)], fill=BLACK)
    d.polygon([(52, 4), (55, 3), (53, 6)], fill=(207, 222, 216, 255))
    d.rectangle((26, 14, 29, 17), fill=BLACK)
    d.point((27, 14), fill=armor[2])
    return image


BUZZARD_RAMPS: dict[int, tuple[RGBA, RGBA, RGBA]] = {
    1: ((79, 24, 27, 255), (164, 48, 46, 255), (235, 101, 67, 255)),
    2: ((54, 53, 65, 255), (126, 124, 126, 255), (217, 211, 187, 255)),
    3: ((25, 42, 79, 255), (43, 91, 162, 255), (81, 176, 219, 255)),
}


def buzzard_frame(tier: int, pose: str, phase: int = 0) -> Image.Image:
    image, d = canvas((48, 34))
    dark, mid, light = BUZZARD_RAMPS[tier]
    d.polygon([(12, 21), (3, 17), (7, 24), (2, 27), (15, 27)], fill=BLACK)
    d.polygon([(12, 22), (6, 19), (9, 24), (6, 26), (16, 25)], fill=mid)

    if pose == "glide":
        outer = [(10, 20), (5, 10), (18, 14), (27, 19), (39, 16), (34, 24), (20, 25)]
        inner = [(11, 20), (8, 12), (18, 16), (27, 21), (36, 18), (32, 22), (20, 23)]
    else:
        outers = (
            [(13, 21), (10, 4), (17, 3), (25, 21)],
            [(13, 21), (5, 15), (8, 25), (25, 27)],
            [(15, 21), (12, 31), (22, 30), (28, 22)],
        )
        inners = (
            [(14, 20), (12, 6), (16, 6), (22, 21)],
            [(13, 21), (8, 17), (10, 23), (23, 25)],
            [(16, 22), (14, 29), (21, 28), (25, 22)],
        )
        outer, inner = outers[phase], inners[phase]
    d.polygon(outer, fill=BLACK)
    d.polygon(inner, fill=dark)
    d.line(inner[:3], fill=light, width=1)

    d.ellipse((12, 17, 37, 29), fill=BLACK)
    d.ellipse((14, 19, 35, 27), fill=mid)
    d.polygon([(16, 24), (24, 28), (34, 26), (35, 23)], fill=dark)
    d.line([(16, 19), (27, 18)], fill=light, width=2)
    d.ellipse((32, 14, 42, 23), fill=BLACK)
    d.ellipse((33, 15, 40, 21), fill=mid)
    d.rectangle((34, 15, 38, 16), fill=light)
    d.polygon([(40, 17), (47, 20), (40, 21)], fill=BLACK)
    d.polygon([(40, 18), (45, 20), (40, 20)], fill=(230, 164, 48, 255))
    d.point((38, 17), fill=(242, 239, 205, 255))
    d.point((39, 17), fill=BLACK)
    # Tier badge echoes the arcade hierarchy even at thumbnail size.
    d.rectangle((20, 21, 23, 24), fill=BLACK)
    d.rectangle((21, 21, 22, 22), fill=light)
    return image


def free_buzzard_frame(phase: int) -> Image.Image:
    image = buzzard_frame(1, "flap", 0 if phase == 0 else 2).crop((3, 3, 45, 33))
    # A warmer wild-bird tint distinguishes free mounts from enemy tiers.
    pixels = image.load()
    replacements = {
        BUZZARD_RAMPS[1][0]: (62, 43, 31, 255),
        BUZZARD_RAMPS[1][1]: (151, 101, 47, 255),
        BUZZARD_RAMPS[1][2]: (237, 180, 77, 255),
    }
    for y in range(image.height):
        for x in range(image.width):
            pixels[x, y] = replacements.get(pixels[x, y], pixels[x, y])
    return image


def hatchling_frame(phase: int) -> Image.Image:
    image, d = canvas((24, 24))
    d.line([(11, 17), (8 + phase * 3, 22)], fill=BLACK, width=2)
    d.line([(15, 17), (17 - phase * 3, 22)], fill=BLACK, width=2)
    d.line([(6, 22), (10, 22)], fill=(218, 145, 48, 255), width=1)
    d.line([(14, 22), (19, 22)], fill=(218, 145, 48, 255), width=1)
    d.ellipse((5, 8, 18, 19), fill=BLACK)
    d.ellipse((7, 10, 17, 18), fill=(184, 113, 45, 255))
    d.ellipse((9, 10, 14, 13), fill=(244, 190, 80, 255))
    d.ellipse((13, 4, 21, 12), fill=BLACK)
    d.ellipse((14, 5, 20, 10), fill=(218, 145, 48, 255))
    d.polygon([(20, 7), (24, 9), (20, 10)], fill=BLACK)
    d.polygon([(20, 8), (23, 9), (20, 9)], fill=(244, 201, 77, 255))
    d.point((18, 6), fill=(242, 241, 204, 255))
    d.point((19, 6), fill=BLACK)
    return image


def egg_frame(kind: str, phase: int = 0) -> Image.Image:
    image, d = canvas((14, 16))
    d.ellipse((1, 0, 12, 15), fill=BLACK)
    d.ellipse((3, 2, 10, 13), fill=(218, 201, 151, 255))
    d.rectangle((4, 2, 7, 4), fill=(249, 238, 193, 255))
    d.rectangle((4, 12, 9, 13), fill=(151, 126, 92, 255))
    if kind == "crack":
        d.line([(2, 7), (5, 6), (6, 9), (9, 7), (12, 8)], fill=BLACK, width=1)
    elif phase == 1:
        d.rectangle((9, 5, 10, 7), fill=(151, 126, 92, 255))
        d.point((4, 9), fill=(151, 126, 92, 255))
    else:
        d.point((8, 8), fill=(151, 126, 92, 255))
    return image


def ptero_frame(pose: str, phase: int = 0) -> Image.Image:
    image, d = canvas((64, 40))
    dark = (55, 50, 77, 255)
    mid = (112, 99, 130, 255)
    light = (192, 166, 164, 255)
    if phase == 0:
        wing_top = [(30, 22), (14, 2), (5, 5), (20, 24)]
        wing_bot = [(29, 24), (12, 31), (5, 37), (26, 31)]
    else:
        wing_top = [(30, 22), (9, 15), (2, 19), (21, 27)]
        wing_bot = [(29, 24), (14, 32), (8, 36), (27, 30)]
    d.polygon(wing_top, fill=BLACK)
    d.polygon([(29, 21), wing_top[1], (8, 6) if phase == 0 else (6, 19), (21, 25)], fill=mid)
    d.line([(27, 20), wing_top[1], wing_top[2]], fill=light, width=1)
    d.polygon(wing_bot, fill=BLACK)
    d.polygon([(28, 25), wing_bot[1], (9, 35), (26, 29)], fill=dark)
    d.ellipse((22, 18, 48, 31), fill=BLACK)
    d.ellipse((24, 20, 47, 29), fill=mid)
    d.polygon([(25, 21), (36, 20), (45, 24), (31, 24)], fill=light)
    d.polygon([(47, 21), (59, 14), (54, 23)], fill=BLACK)
    d.polygon([(48, 22), (57, 16), (53, 23)], fill=mid)
    d.ellipse((48, 17, 59, 27), fill=BLACK)
    d.ellipse((49, 18, 58, 25), fill=mid)
    d.polygon([(57, 20), (64, 23), (57, 26)], fill=BLACK)
    if pose == "mouth":
        d.polygon([(57, 20), (64, 20), (59, 23)], fill=(224, 169, 87, 255))
        d.polygon([(57, 24), (63, 27), (58, 26)], fill=(224, 169, 87, 255))
        d.rectangle((58, 23, 62, 24), fill=(91, 25, 37, 255))
    else:
        d.polygon([(57, 21), (63, 23), (57, 25)], fill=(224, 169, 87, 255))
    d.point((55, 19), fill=(238, 232, 194, 255))
    d.point((56, 19), fill=BLACK)
    return image


def troll_frame(pose: str) -> Image.Image:
    image, d = canvas((28, 44))
    dark = (31, 58, 42, 255)
    mid = (61, 112, 64, 255)
    light = (132, 169, 79, 255)
    offsets = {"rise": (0, 7), "grab": (0, 1), "drag": (-3, 5)}
    ox, oy = offsets[pose]
    wrist = [(8 + ox, 43), (7 + ox, 26 + oy), (20 + ox, 23 + oy), (22 + ox, 43)]
    d.polygon(wrist, fill=BLACK)
    d.polygon([(10 + ox, 43), (9 + ox, 27 + oy), (18 + ox, 25 + oy), (20 + ox, 43)], fill=mid)
    d.polygon([(10 + ox, 29 + oy), (12 + ox, 26 + oy), (14 + ox, 42)], fill=light)
    fingers = (
        [(8, 29), (2, 17), (5, 14), (12, 26)],
        [(12, 25), (9, 9), (13, 6), (17, 24)],
        [(16, 25), (17, 8), (21, 8), (21, 27)],
        [(19, 28), (24, 16), (27, 18), (23, 32)],
    )
    for finger in fingers:
        shifted = [(x + ox, y + oy) for x, y in finger]
        d.polygon(shifted, fill=BLACK)
        d.line(shifted[1:3], fill=light, width=2)
        d.line(shifted[2:4], fill=mid, width=2)
    d.rectangle((8 + ox, 28 + oy, 20 + ox, 35 + oy), fill=mid)
    d.line([(9 + ox, 29 + oy), (17 + ox, 27 + oy)], fill=light, width=2)
    d.point((12 + ox, 35 + oy), fill=dark)
    d.point((17 + ox, 34 + oy), fill=dark)
    return image


def platform_frame(kind: str, phase: int = 0) -> Image.Image:
    height = 24 if kind == "burn" else 16
    image, d = canvas((32, height))
    top = (209, 160, 77, 255)
    stone = (93, 82, 72, 255)
    shadow = (45, 39, 43, 255)
    if kind == "burn":
        flame_shapes = (
            [(3, 15), (6, 7), (9, 13), (13, 2), (16, 14), (21, 5), (24, 14), (29, 8), (30, 17)],
            [(2, 16), (5, 4), (10, 14), (14, 7), (18, 15), (23, 1), (26, 14), (30, 10)],
            [(2, 16), (7, 9), (10, 15), (13, 4), (18, 14), (21, 8), (25, 15), (29, 4), (31, 17)],
        )
        d.polygon(flame_shapes[phase], fill=(116, 24, 28, 255))
        d.polygon([(4, 16), (7, 10), (10, 16), (14, 8), (18, 17), (23, 7), (27, 17)], fill=(236, 67, 28, 255))
        d.polygon([(7, 16), (9, 13), (12, 17), (15, 12), (18, 17), (22, 11), (25, 17)], fill=(255, 189, 48, 255))
        y = 16
    else:
        y = 0
    d.rectangle((0, y, 31, y + 3), fill=BLACK)
    d.rectangle((1, y + 1, 30, y + 2), fill=top)
    d.rectangle((0, y + 4, 31, height - 1), fill=BLACK)
    d.rectangle((2, y + 4, 29, height - 3), fill=stone)
    d.rectangle((3, y + 5, 18, y + 6), fill=(145, 122, 91, 255))
    d.rectangle((2, height - 5, 29, height - 3), fill=shadow)
    for x in range(4, 30, 7):
        d.point((x, y + 8 + ((x // 7) & 1)), fill=shadow)
    if kind == "edge_l":
        d.polygon([(0, y), (8, y), (5, height - 1), (0, height - 1)], fill=BLACK)
        d.polygon([(2, y + 2), (6, y + 2), (4, height - 3), (2, height - 3)], fill=stone)
    elif kind == "edge_r":
        d.polygon([(24, y), (31, y), (31, height - 1), (27, height - 1)], fill=BLACK)
        d.polygon([(26, y + 2), (29, y + 2), (29, height - 3), (28, height - 3)], fill=stone)
    return image


def lava_frame(phase: int) -> Image.Image:
    image, d = canvas((32, 16))
    d.rectangle((0, 4, 31, 15), fill=(92, 16, 27, 255))
    wave = [(0, 6), (4, 3 + (phase & 1)), (8, 6), (13, 2 + ((phase + 1) & 1)),
            (18, 6), (24, 3 + ((phase >> 1) & 1)), (31, 5), (31, 15), (0, 15)]
    d.polygon(wave, fill=(223, 48, 24, 255))
    d.line([(0, 7), (5, 5), (10, 8), (15, 4), (21, 8), (27, 5), (31, 7)], fill=(255, 142, 27, 255), width=2)
    d.line([(2 + phase * 2, 6), (7 + phase * 2, 5)], fill=(255, 232, 91, 255), width=1)
    for x in range((phase * 3) % 6, 32, 7):
        d.point((x, 11 + ((x + phase) & 2)), fill=(255, 179, 39, 255))
    d.rectangle((0, 15, 31, 15), fill=(52, 13, 24, 255))
    return image


def shimmer_frame(phase: int) -> Image.Image:
    image, d = canvas((32, 16))
    colors = ((255, 235, 143, 220), (255, 128, 46, 190), (255, 248, 206, 230))
    for x in range(2 + phase * 3, 32, 9):
        d.line([(x, 3), (x, 11)], fill=colors[phase], width=1)
        d.line([(x - 3, 7), (x + 3, 7)], fill=colors[phase], width=1)
        d.point((x - 1, 6), fill=colors[(phase + 1) % 3])
        d.point((x + 1, 8), fill=colors[(phase + 2) % 3])
    return image


GLYPHS: dict[str, tuple[str, ...]] = {
    "0": ("01110", "10001", "10011", "10101", "11001", "10001", "01110"),
    "1": ("00100", "01100", "00100", "00100", "00100", "00100", "01110"),
    "2": ("01110", "10001", "00001", "00010", "00100", "01000", "11111"),
    "3": ("11110", "00001", "00001", "01110", "00001", "00001", "11110"),
    "4": ("00010", "00110", "01010", "10010", "11111", "00010", "00010"),
    "5": ("11111", "10000", "10000", "11110", "00001", "00001", "11110"),
    "6": ("01110", "10000", "10000", "11110", "10001", "10001", "01110"),
    "7": ("11111", "00001", "00010", "00100", "01000", "01000", "01000"),
    "8": ("01110", "10001", "10001", "01110", "10001", "10001", "01110"),
    "9": ("01110", "10001", "10001", "01111", "00001", "00001", "01110"),
    "A": ("01110", "10001", "10001", "11111", "10001", "10001", "10001"),
    "B": ("11110", "10001", "10001", "11110", "10001", "10001", "11110"),
    "C": ("01111", "10000", "10000", "10000", "10000", "10000", "01111"),
    "D": ("11110", "10001", "10001", "10001", "10001", "10001", "11110"),
    "E": ("11111", "10000", "10000", "11110", "10000", "10000", "11111"),
    "F": ("11111", "10000", "10000", "11110", "10000", "10000", "10000"),
    "G": ("01111", "10000", "10000", "10111", "10001", "10001", "01111"),
    "H": ("10001", "10001", "10001", "11111", "10001", "10001", "10001"),
    "I": ("11111", "00100", "00100", "00100", "00100", "00100", "11111"),
    "J": ("00111", "00010", "00010", "00010", "00010", "10010", "01100"),
    "K": ("10001", "10010", "10100", "11000", "10100", "10010", "10001"),
    "L": ("10000", "10000", "10000", "10000", "10000", "10000", "11111"),
    "M": ("10001", "11011", "10101", "10101", "10001", "10001", "10001"),
    "N": ("10001", "11001", "10101", "10011", "10001", "10001", "10001"),
    "O": ("01110", "10001", "10001", "10001", "10001", "10001", "01110"),
    "P": ("11110", "10001", "10001", "11110", "10000", "10000", "10000"),
    "Q": ("01110", "10001", "10001", "10001", "10101", "10010", "01101"),
    "R": ("11110", "10001", "10001", "11110", "10100", "10010", "10001"),
    "S": ("01111", "10000", "10000", "01110", "00001", "00001", "11110"),
    "T": ("11111", "00100", "00100", "00100", "00100", "00100", "00100"),
    "U": ("10001", "10001", "10001", "10001", "10001", "10001", "01110"),
    "V": ("10001", "10001", "10001", "10001", "10001", "01010", "00100"),
    "W": ("10001", "10001", "10001", "10101", "10101", "11011", "10001"),
    "X": ("10001", "10001", "01010", "00100", "01010", "10001", "10001"),
    "Y": ("10001", "10001", "01010", "00100", "00100", "00100", "00100"),
    "Z": ("11111", "00001", "00010", "00100", "01000", "10000", "11111"),
    ".": ("00000", "00000", "00000", "00000", "00000", "00110", "00110"),
    ":": ("00000", "00110", "00110", "00000", "00110", "00110", "00000"),
    "-": ("00000", "00000", "00000", "11111", "00000", "00000", "00000"),
    ">": ("10000", "01000", "00100", "00010", "00100", "01000", "10000"),
    " ": ("00000",) * 7,
}


def font_frame(char: str) -> Image.Image:
    image, d = canvas((8, 10))
    pattern = GLYPHS[char]
    lit = {(x, y) for y, row in enumerate(pattern) for x, bit in enumerate(row) if bit == "1"}
    # One-pixel down-right shadow/outline, then warm face and sparse highlight.
    for x, y in lit:
        d.point((x + 2, y + 2), fill=INK)
    for x, y in lit:
        d.point((x + 1, y + 1), fill=(236, 190, 82, 255))
        if (x - 1, y) not in lit and (x, y - 1) not in lit:
            d.point((x + 1, y + 1), fill=(255, 239, 153, 255))
    return image


def logo_frame() -> Image.Image:
    image, d = canvas((144, 44))
    # Winged backing and lance underline give the wordmark an arcade crest shape.
    d.polygon([(4, 23), (20, 11), (30, 16), (18, 26), (30, 30), (12, 32)], fill=BLACK)
    d.polygon([(140, 23), (124, 11), (114, 16), (126, 26), (114, 30), (132, 32)], fill=BLACK)
    d.polygon([(7, 23), (20, 14), (26, 17), (16, 25), (25, 29), (13, 29)], fill=(185, 67, 38, 255))
    d.polygon([(137, 23), (124, 14), (118, 17), (128, 25), (119, 29), (131, 29)], fill=(185, 67, 38, 255))
    scale = 4
    start_x = 20
    for index, char in enumerate("JOUST"):
        ox = start_x + index * 22
        for y, row in enumerate(GLYPHS[char]):
            for x, bit in enumerate(row):
                if bit == "1":
                    d.rectangle((ox + x * scale - 1, 5 + y * scale - 1,
                                 ox + x * scale + scale, 5 + y * scale + scale), fill=BLACK)
        for y, row in enumerate(GLYPHS[char]):
            for x, bit in enumerate(row):
                if bit == "1":
                    color = (255, 217, 91, 255) if y < 3 else (208, 126, 39, 255)
                    d.rectangle((ox + x * scale, 5 + y * scale,
                                 ox + x * scale + 2, 5 + y * scale + 2), fill=color)
                    d.point((ox + x * scale, 5 + y * scale), fill=(255, 245, 167, 255))
    d.line([(18, 39), (128, 39)], fill=BLACK, width=3)
    d.line([(20, 38), (130, 38)], fill=(226, 155, 55, 255), width=1)
    d.polygon([(128, 35), (143, 38), (128, 42)], fill=BLACK)
    d.polygon([(130, 37), (140, 38), (130, 40)], fill=(217, 226, 214, 255))
    return image


Frame = tuple[str, Image.Image, list[int], list[int] | None]


def pack_sheet(sheet: str, frames: list[Frame], width: int) -> tuple[Image.Image, dict[str, dict[str, object]]]:
    """Shelf-pack frames in input order with a transparent one-pixel gutter."""
    placements: list[tuple[Frame, int, int]] = []
    x = 1
    y = 1
    row_height = 0
    for frame in frames:
        _, image, _, _ = frame
        if x + image.width + 1 > width:
            x = 1
            y += row_height + 2
            row_height = 0
        placements.append((frame, x, y))
        x += image.width + 2
        row_height = max(row_height, image.height)
    height = y + row_height + 1
    atlas = Image.new("RGBA", (width, height), TRANSPARENT)
    manifest_frames: dict[str, dict[str, object]] = {}
    for (name, image, anchor, lance), px, py in placements:
        atlas.alpha_composite(image, (px, py))
        manifest_frames[name] = {
            "sheet": sheet,
            "rect": [px, py, image.width, image.height],
            "anchor": anchor,
            "lance": lance,
        }
    return atlas, manifest_frames


def build_mounted() -> list[Frame]:
    frames: list[Frame] = []
    for player in (1, 2):
        frames.append((f"p{player}_stand", mounted_frame(player, "stand"), [24, 42], [32, -39]))
        for phase in range(4):
            frames.append((f"p{player}_run_{phase}", mounted_frame(player, "run", phase), [24, 42], [32, -39]))
        for phase in range(3):
            frames.append((f"p{player}_flap_{phase}", mounted_frame(player, "flap", phase), [24, 42], [32, -39]))
        frames.append((f"p{player}_brake", mounted_frame(player, "brake"), [24, 42], [32, -39]))
    return frames


def build_creatures() -> list[Frame]:
    frames: list[Frame] = []
    for tier in (1, 2, 3):
        for phase in range(3):
            frames.append((f"buzzard{tier}_flap_{phase}", buzzard_frame(tier, "flap", phase), [23, 30], None))
        frames.append((f"buzzard{tier}_glide", buzzard_frame(tier, "glide"), [23, 30], None))
    for phase in range(2):
        frames.append((f"buzzard_free_{phase}", free_buzzard_frame(phase), [20, 27], None))
        frames.append((f"hatchling_{phase}", hatchling_frame(phase), [12, 23], None))
        frames.append((f"egg_{phase}", egg_frame("whole", phase), [7, 15], None))
    frames.append(("egg_crack", egg_frame("crack"), [7, 15], None))
    for phase in range(2):
        frames.append((f"ptero_fly_{phase}", ptero_frame("fly", phase), [32, 32], None))
    frames.append(("ptero_mouth", ptero_frame("mouth", 1), [32, 32], None))
    for pose in ("rise", "grab", "drag"):
        frames.append((f"troll_{pose}", troll_frame(pose), [14, 43], None))
    return frames


def build_world() -> list[Frame]:
    frames: list[Frame] = [
        ("plat_tile", platform_frame("tile"), [16, 15], None),
        ("plat_edge_l", platform_frame("edge_l"), [16, 15], None),
        ("plat_edge_r", platform_frame("edge_r"), [16, 15], None),
    ]
    for phase in range(3):
        frames.append((f"plat_burn_{phase}", platform_frame("burn", phase), [16, 23], None))
    for phase in range(4):
        frames.append((f"lava_{phase}", lava_frame(phase), [16, 15], None))
    for phase in range(3):
        frames.append((f"shimmer_{phase}", shimmer_frame(phase), [16, 15], None))
    frames.append(("logo", logo_frame(), [72, 43], None))
    return frames


def build_font() -> list[Frame]:
    chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.:-> "
    return [
        ("font_SP" if char == " " else f"font_{char}", font_frame(char), [0, 9], None)
        for char in chars
    ]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    groups = (
        ("mounted", build_mounted(), 350),
        ("creatures", build_creatures(), 302),
        ("world", build_world(), 242),
        ("font", build_font(), 161),
    )
    manifest: dict[str, dict[str, object]] = {"sheets": {}, "frames": {}}
    for sheet, frames, width in groups:
        atlas, packed = pack_sheet(sheet, frames, width)
        filename = f"{sheet}.png"
        atlas.save(OUT / filename, format="PNG", optimize=False, compress_level=9)
        manifest["sheets"][sheet] = filename
        manifest["frames"].update(packed)
    (OUT / "sprites.json").write_text(
        json.dumps(manifest, indent=2, sort_keys=True) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
