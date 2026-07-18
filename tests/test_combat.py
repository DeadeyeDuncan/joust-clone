from joust import combat, config


class Stub:
    def __init__(self, x, y, lance_y, vx=0, mounted=True):
        self.x, self.y, self.lance_y, self.vx, self.vy = x, y, lance_y, vx, 0
        self.mounted = mounted

    def rect(self):
        return (self.x - 20, self.y - 32, 40, 32)


def test_higher_lance_wins():
    assert combat.resolve(90, 100) == "a"       # smaller y = higher
    assert combat.resolve(110, 100) == "b"


def test_tie_band_inclusive():
    assert combat.resolve(100, 100 + config.TIE_PX) == "tie"
    assert combat.resolve(100, 100 + config.TIE_PX + 1) == "a"   # a at y=100 is higher


def test_resolve_pair_matrix():
    hi, lo = Stub(0, 0, 80), Stub(0, 0, 120)
    assert combat.resolve_pair(hi, lo) == "a"
    assert combat.resolve_pair(lo, hi) == "b"
    assert combat.resolve_pair(Stub(0, 0, 100), Stub(0, 0, 102)) == "tie"
    walker = Stub(0, 0, 100, mounted=False)
    assert combat.resolve_pair(hi, walker) == "collect_a"
    assert combat.resolve_pair(walker, hi) == "collect_b"
    assert combat.resolve_pair(walker, Stub(0, 0, 0, mounted=False)) is None


def test_bounce_repels():
    a, b = Stub(100, 100, 80, vx=50), Stub(120, 100, 80, vx=-50)
    combat.bounce(a, b)
    assert a.vx < 0 < b.vx and a.vy < 0 and b.vy < 0


def test_collide_aabb():
    assert combat.collide(Stub(100, 100, 0), Stub(130, 100, 0))
    assert not combat.collide(Stub(100, 100, 0), Stub(200, 100, 0))
