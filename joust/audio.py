"""Small pygame mixer facade for generated sound effects."""

from pathlib import Path

import pygame

from joust import config


SFX_DIR = Path(__file__).resolve().parents[1] / "assets" / "sfx"
COOLDOWN_MS = 60
_sounds = {}
_last_played = {}


def init():
    if pygame.mixer.get_init() is None:
        pygame.mixer.init()
    pygame.mixer.set_num_channels(config.MIXER_CHANNELS)
    _sounds.clear()
    _last_played.clear()
    for path in SFX_DIR.glob("*.wav"):
        _sounds[path.stem] = pygame.mixer.Sound(str(path))


def play(name):
    sound = _sounds.get(name)
    if sound is None:
        return
    now = pygame.time.get_ticks()
    if now - _last_played.get(name, -COOLDOWN_MS) < COOLDOWN_MS:
        return
    _last_played[name] = now
    sound.play()
