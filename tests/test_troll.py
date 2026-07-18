from joust.entities.troll import Troll
from joust.world import World
from joust import config
from joust.states import PlayState

class M:
    def __init__(self, x, y):
        self.x, self.y, self.vx, self.vy, self.alive = x, y, 0.0, 0.0, True

def test_latches_lowest_in_band_and_drags():
    w, t = World(), Troll()
    hi = M(300, w.LAVA_Y - 200)
    lo = M(310, w.LAVA_Y - 10)
    t.update(config.DT, w, [hi, lo])
    assert t.victim is lo
    t.update(config.DT, w, [hi, lo])
    assert lo.vy == config.TROLL_DRAG_VY

def test_flap_fight_breaks_free():
    w, t = World(), Troll()
    m = M(300, w.LAVA_Y - 10)
    t.update(config.DT, w, [m])
    for _ in range(4):
        t.fight()
    for _ in range(30):
        t.update(config.DT, w, [m])
        m.y += m.vy * config.DT
    assert t.victim is None and m.y < w.LAVA_Y - config.TROLL_GRAB_BAND

def test_drag_to_lava_consumes():
    w, t = World(), Troll()
    m = M(300, w.LAVA_Y - 6)
    t.update(config.DT, w, [m])
    for _ in range(600):
        t.update(config.DT, w, [m])
        m.y += m.vy * config.DT
        if t.consumed:
            break
    assert t.consumed is m


def test_killing_latched_player_releases_troll_before_respawn_flap():
    state = PlayState.__new__(PlayState)
    state.setup_logic(two_player=False)
    player = state.players[0]
    player.y = state.world.LAVA_Y - 10
    state.troll.update(config.DT, state.world, [player])
    assert state.troll.victim is player

    state._kill_player(player)
    assert state.troll.victim is None

    state._flap_requests.add(player.pid)
    state._apply_input()
    assert player.vy == config.FLAP_VY
