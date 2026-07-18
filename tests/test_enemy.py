import random

from joust import config
from joust.entities.enemy import Enemy
from joust.world import World


class P:  # minimal player stub
    def __init__(self, x, y):
        self.x, self.y, self.alive = x, y, True


def run(e, w, players, seconds):
    for _ in range(int(seconds / config.DT)):
        e.update(config.DT, w, players)


def test_points_by_tier():
    r = random.Random(7)
    assert Enemy(1, 0, 0, r).points == 500
    assert Enemy(2, 0, 0, r).points == 750
    assert Enemy(3, 0, 0, r).points == 1500


def test_bounder_deterministic_with_seed():
    w = World()
    a = Enemy(1, 320, 100, random.Random(42))
    b = Enemy(1, 320, 100, random.Random(42))
    run(a, w, [], 3)
    run(b, w, [], 3)
    assert (a.x, a.y, a.vx, a.vy) == (b.x, b.y, b.vx, b.vy)


def test_hunter_closes_horizontal_distance():
    w = World()
    e = Enemy(2, 100, 120, random.Random(1))
    p = P(500, 120)
    d0 = abs(e.x - p.x)
    run(e, w, [p], 4)
    assert abs(e.x - p.x) < d0


def test_shadow_lord_faster_and_higher():
    w = World()
    lord = Enemy(3, 100, 200, random.Random(1))
    hunter = Enemy(2, 100, 200, random.Random(1))
    p = P(540, 260)
    run(lord, w, [p], 2)
    run(hunter, w, [p], 2)
    assert abs(lord.x - 100) > abs(hunter.x - 100)  # covered more ground
    e = Enemy(3, 100, 200, random.Random(1))
    run(e, w, [P(540, 300)], 3)
    assert e.y < 300 - 30  # cruises above target


def test_targeting_uses_wrapped_distance():
    w = World()
    e = Enemy(2, 620, 120, random.Random(1))
    p = P(20, 120)
    d0 = abs(((p.x - e.x + config.LOGICAL_W / 2) % config.LOGICAL_W) - config.LOGICAL_W / 2)
    run(e, w, [p], 2)
    distance = abs(
        ((p.x - e.x + config.LOGICAL_W / 2) % config.LOGICAL_W)
        - config.LOGICAL_W / 2
    )
    assert distance < d0


def test_nearest_player_uses_wrapped_horizontal_distance():
    enemy = Enemy(2, 636, 120, random.Random(1))
    across_seam = P(4, 120)
    raw_near = P(500, 120)

    assert enemy._nearest_living_player([raw_near, across_seam]) is across_seam


def test_enemy_flaps_out_of_lava_band():
    world = World()
    enemy = Enemy(2, 320, 330, random.Random(42))

    run(enemy, world, [], 2)

    assert enemy.y < config.AI_LAVA_AVOID_Y
