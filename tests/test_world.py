from joust.world import World
from joust import config


def test_layout_ten_pieces_and_four_pads():
    w = World()
    assert len(w.platforms) == 10   # 8 visible structures; bottom shelf is 3 collision pieces
    assert len(w.spawn_pads) == 4
    for px, py in w.spawn_pads:
        assert any(p.alive and p.x <= px <= p.x + p.w and abs(p.y - py) < 1 for p in w.platforms)


def test_wrap():
    w = World()
    assert w.wrap_x(-1) == config.LOGICAL_W - 1
    assert w.wrap_x(config.LOGICAL_W + 5) == 5


def test_ground_under_only_on_downward_crossing():
    w = World()
    p = w.platforms[0]
    assert w.ground_under(p.x + p.w / 2, p.y - 1, p.y + 1) is p        # crossed top going down
    assert w.ground_under(p.x + p.w / 2, p.y + 1, p.y - 1) is None     # rising: pass through
    assert w.ground_under(p.x - 10, p.y - 1, p.y + 1) is None          # off span
    assert w.ground_under(p.x + p.w / 2, p.y + 2, p.y + 8) is None     # already below top: no snap


def test_erosion_schedule_is_cumulative_and_permanent():
    w = World()
    alive0 = sum(p.alive for p in w.platforms)
    w.apply_erosion(4); a4 = sum(p.alive for p in w.platforms)
    w.apply_erosion(6); a6 = sum(p.alive for p in w.platforms)
    w.apply_erosion(9); a9 = sum(p.alive for p in w.platforms)
    assert alive0 > a4 > a6 > a9
    w.apply_erosion(3)                       # earlier wave never resurrects
    assert sum(p.alive for p in w.platforms) == a9


def test_grab_band():
    w = World()
    assert w.in_grab_band(w.LAVA_Y - 5)
    assert not w.in_grab_band(w.LAVA_Y - config.TROLL_GRAB_BAND - 1)


def test_erosion_burns_before_platform_stops_colliding():
    world = World()
    platform = next(item for item in world.platforms if item.id == "p8")

    world.apply_erosion(4)

    assert platform.burning
    assert world.ground_under(
        platform.x + platform.w / 2,
        platform.y - 1,
        platform.y + 1,
    ) is platform

    world.update_erosion(config.EROSION_BURN_S + config.DT)

    assert not platform.burning
    assert not platform.alive
    assert world.ground_under(
        platform.x + platform.w / 2,
        platform.y - 1,
        platform.y + 1,
    ) is None
