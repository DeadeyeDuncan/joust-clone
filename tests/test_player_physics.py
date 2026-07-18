from joust.entities.player import Player
from joust.world import World
from joust import config


def steps(p, w, n, dt=config.DT):
    for _ in range(n):
        p.update(dt, w)


def test_gravity_pulls_when_airborne():
    w, p = World(), Player(1, 320, 100)
    v0 = p.vy
    steps(p, w, 10)
    assert p.vy > v0


def test_flap_is_impulse_not_hold():
    w, p = World(), Player(1, 320, 100)
    p.flap()
    assert abs(p.vy - config.FLAP_VY) < 1e-6
    vy_after_flap = p.vy
    steps(p, w, 5)
    assert p.vy > vy_after_flap


def test_thrust_caps_at_max_vx():
    w, p = World(), Player(1, 320, 100)
    p.set_dir(1)
    steps(p, w, 600)
    assert abs(p.vx) <= config.MAX_VX + 1e-6


def test_lands_on_platform_and_runs():
    w = World()
    plat = w.platforms[0]
    p = Player(1, plat.x + plat.w / 2, plat.y - 30)
    steps(p, w, 120)
    assert p.grounded and abs(p.y - plat.y) < 1e-6


def test_skid_brake_state_on_reversal():
    w = World()
    plat = w.platforms[7]
    p = Player(1, plat.x + plat.w / 2, plat.y)
    p.grounded = True
    p.vx = 150
    p.set_dir(-1)
    p.update(config.DT, w)
    assert p.state == "brake"


def test_horizontal_wrap():
    w, p = World(), Player(1, 2, 100)
    p.vx = -100
    steps(p, w, 5)
    assert p.x > config.LOGICAL_W - 20


def test_ceiling_clamps():
    w, p = World(), Player(1, 320, 4)
    p.vy = -300
    steps(p, w, 10)
    assert p.y >= 0


def test_rect_contract():
    p = Player(1, 100, 200)
    assert p.rect() == (
        100 - config.MOUNT_W / 2,
        200 - config.MOUNT_H,
        config.MOUNT_W,
        config.MOUNT_H,
    )


def test_spawn_invulnerability_clears_on_move_or_timeout():
    w = World()
    p = Player(1, 320, 100)
    assert p.invulnerable
    p.set_dir(1)
    assert not p.invulnerable
    q = Player(1, 320, 100)
    steps(q, w, int(config.SPAWN_INVULN_S / config.DT) + 2)
    assert not q.invulnerable
