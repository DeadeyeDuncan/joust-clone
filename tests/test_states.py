import pygame
import pytest

from joust import config
from joust.states import PlayState, _blit


class SeamAsset:
    def __init__(self):
        self.surface = pygame.Surface((10, 10), pygame.SRCALPHA)
        self.surface.fill((255, 255, 255, 255))

    def frame(self, name):
        return self.surface

    def anchor(self, name):
        return (5, 5)


@pytest.mark.parametrize(
    ("x", "wrapped_pixel"),
    ((2, (config.LOGICAL_W - 1, 50)), (config.LOGICAL_W - 2, (0, 50))),
)
def test_blit_draws_second_copy_across_horizontal_seam(x, wrapped_pixel):
    target = pygame.Surface((config.LOGICAL_W, config.LOGICAL_H), pygame.SRCALPHA)

    _blit(SeamAsset(), target, "frame", x, 55)

    assert target.get_at(wrapped_pixel).a == 255


def test_blit_does_not_draw_second_copy_for_non_wrapping_entity():
    target = pygame.Surface((config.LOGICAL_W, config.LOGICAL_H), pygame.SRCALPHA)

    _blit(SeamAsset(), target, "frame", -2, 55, wraps=False)

    assert target.get_at((config.LOGICAL_W - 1, 50)).a == 0


def test_wave_buzzards_spawn_on_distant_pads_when_available():
    state = PlayState.__new__(PlayState)
    state.setup_logic(two_player=False)
    player_x = state.players[0].x

    distances = [
        abs(
            ((enemy.x - player_x + config.LOGICAL_W / 2) % config.LOGICAL_W)
            - config.LOGICAL_W / 2
        )
        for enemy in state.enemies
    ]

    assert distances
    assert all(distance >= config.ENEMY_SPAWN_MIN_DIST for distance in distances)
