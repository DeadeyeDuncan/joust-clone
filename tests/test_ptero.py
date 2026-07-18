from joust.entities.pterodactyl import Ptero, PteroDirector
from joust.world import World
from joust import config

class T:
    def __init__(self, x, y):
        self.x, self.y, self.alive = x, y, True

def test_flies_toward_target_and_opens_mouth_close():
    w = World()
    p = Ptero(side=-1)
    t = T(320, 180)
    d0 = abs(p.x - t.x)
    for _ in range(120):
        p.update(config.DT, w, t)
    assert abs(p.x - t.x) < d0
    p.x = t.x + 50
    p.update(config.DT, w, t)
    assert p.mouth_open
    mx, my, mw, mh = p.mouth_rect()
    bx, by, bw, bh = p.rect()
    assert mw * mh < bw * bh / 8

def test_director_timers():
    d = PteroDirector(wave_is_ptero=False)
    assert d.update(config.DT, 1.0, enemies_left=3) == []
    spawned = d.update(config.DT, config.PTERO_FIRST_S + 0.1, enemies_left=3)
    assert len(spawned) == 1
    assert d.update(config.DT, config.PTERO_FIRST_S + 0.2, enemies_left=3) == []

def test_ptero_wave_spawns_three_at_start():
    d = PteroDirector(wave_is_ptero=True)
    assert len(d.update(config.DT, 0.0, enemies_left=0)) == 3

def test_lance_kill_only_in_open_mouth():
    w = World()
    p = Ptero(side=-1)
    t = T(320, 180)
    p.x, p.y = t.x + 50, 180
    p.update(config.DT, w, t)
    assert p.mouth_open
    mx, my, mw, mh = p.mouth_rect()
    assert p.check_lance(mx + mw / 2, my + mh / 2) == "kill" and not p.alive
    q = Ptero(side=-1)
    q.x, q.y = t.x + 300, 180          # far: mouth closed
    q.update(config.DT, w, t)
    assert not q.mouth_open
    qx, qy, qw, qh = q.mouth_rect()
    assert q.check_lance(qx + qw / 2, qy + qh / 2) is None and q.alive
