"""Bitmap-font HUD rendering."""

from joust import config


GLYPH_W = 8
GLYPH_STEP = 9


def draw_text(surface, assets, text, x, y, centered=False):
    text = str(text).upper()
    if centered:
        x -= len(text) * GLYPH_STEP // 2
    for char in text:
        name = "font_SP" if char == " " else f"font_{char}"
        try:
            glyph = assets.frame(name)
        except KeyError:
            glyph = assets.frame("font_SP")
        surface.blit(glyph, (x, y))
        x += GLYPH_STEP


def draw(surface, assets, game):
    players = getattr(game, "players", ())
    if players:
        p1 = players[0]
        draw_text(surface, assets, f"P1 {p1.score:06d}  L {p1.lives}", 8, 8)
    if len(players) > 1:
        p2 = players[1]
        text = f"P2 {p2.score:06d}  L {p2.lives}"
        draw_text(surface, assets, text, config.LOGICAL_W - len(text) * GLYPH_STEP - 8, 8)
    if getattr(game, "wave_banner_s", 0.0) > 0.0:
        label = f"WAVE {game.wave_n}"
        kind = getattr(game, "wave_type", "normal")
        if kind != "normal":
            label += f" {kind}"
        draw_text(surface, assets, label, config.LOGICAL_W // 2, 36, centered=True)
    if getattr(game, "paused", False):
        draw_text(surface, assets, "PAUSED", config.LOGICAL_W // 2, config.LOGICAL_H // 2, centered=True)
