import random
from joust.waves import wave_type, composition, egg_wave_layout, WaveDirector
from joust.world import World

def test_schedule_and_precedence():
    assert wave_type(1, False) == "normal"
    assert wave_type(5, False) == "survival"
    assert wave_type(8, False) == "egg"
    assert wave_type(11, False) == "ptero"
    assert wave_type(6, True) == "team"
    assert wave_type(6, False) == "normal"          # team is 2P-only
    # collision: W18 = egg(8+5+5) AND ptero(11+7) -> egg wins, ptero skips to W25
    assert wave_type(18, False) == "egg"
    # W25 is survival (5+5k) AND ptero's deferred slot -> survival wins, ptero skips to W32
    assert wave_type(25, False) == "survival"
    assert wave_type(32, False) == "ptero"
    assert wave_type(15, False) == "survival"

def test_composition_progression():
    r = random.Random(3)
    assert composition(1, r) == [1, 1, 1]
    assert composition(2, r) == [1, 1, 1, 1]
    assert composition(3, r) == [1, 1, 2, 2]
    for n in (4, 5, 6):
        c = composition(n, random.Random(n))
        assert 4 <= len(c) <= 6 and c.count(2) >= c.count(1) and 3 not in c
    c7 = composition(7, random.Random(7))
    assert 1 <= c7.count(3) <= 2

def test_wave_seven_adds_lords_to_mixed_group():
    for seed in range(10):
        c = composition(7, random.Random(seed))
        assert 5 <= len(c) <= 8
        assert c.count(2) >= c.count(1)

def test_egg_wave_layout_on_living_platforms():
    w = World()
    w.apply_erosion(9)
    pts = egg_wave_layout(w)
    assert len(pts) == 6
    for x, y in pts:
        assert any(p.alive and p.x <= x <= p.x + p.w and abs((p.y - 5) - y) < 6 for p in w.platforms)

def test_end_bonuses():
    d = WaveDirector(two_player=True)
    b = d.end_bonuses(5, deaths_by_player={1: 0, 2: 1}, jousted_teammate=False)
    assert b == {1: 3000, 2: 0}
    b = d.end_bonuses(6, deaths_by_player={1: 0, 2: 0}, jousted_teammate=True)
    assert b == {1: 0, 2: 0}
    b = d.end_bonuses(6, deaths_by_player={1: 1, 2: 0}, jousted_teammate=False)
    assert b == {1: 3000, 2: 3000}
