from joust.entities.egg import Egg, EggChain
from joust.world import World
from joust import config


def run(e, w, seconds):
    out = None
    for _ in range(int(seconds / config.DT)):
        out = e.update(config.DT, w) or out
    return out


def test_falls_bounces_rests():
    w = World()
    plat = w.platforms[0]
    e = Egg(1, plat.x + plat.w / 2, plat.y - 60, 0, 0)
    run(e, w, 3)
    assert e.state == "resting" and abs(e.y - plat.y) < 2


def test_lava_dooms_egg():
    w = World()
    e = Egg(1, 600, 320, 0, 50)        # x=600 is right of p8 span (424-544), open lava
    run(e, w, 2)
    assert e.state == "dead" and e.doomed


def test_hatch_then_remount_escalates_once():
    w = World()
    plat = w.platforms[0]
    e = Egg(2, plat.x + 10, plat.y - 5, 0, 0)
    req = run(e, w, config.EGG_HATCH_S + config.HATCHLING_WAIT_S + 2.0)
    assert req == ("spawn_enemy", 3, e.x, e.y)
    assert e.state == "dead"
    assert run(e, w, 1) is None        # fires exactly once


def test_tier_caps_at_3():
    assert Egg(3, 0, 0, 0, 0).next_tier == 3


def test_collect_during_hatchling_still_allowed():
    w = World()
    plat = w.platforms[0]
    e = Egg(1, plat.x + 10, plat.y - 5, 0, 0)
    run(e, w, config.EGG_HATCH_S + 0.5)
    assert e.state == "hatchling"
    e.collect()
    assert e.state == "dead" and not e.doomed


def test_chain():
    c = EggChain()
    assert [c.value() for _ in range(5)] == [250, 500, 750, 1000, 1000]
    c.reset()
    assert c.value() == 250
