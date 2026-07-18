from joust.main import GameLoop

def test_fixed_step_accumulates_exactly():
    calls = []
    loop = GameLoop(update=lambda dt: calls.append(dt), render=lambda a: None, headless=True)
    loop.pump(0.05)               # 3 whole steps at 1/60, remainder kept
    assert len(calls) == 3
    assert all(abs(dt - 1 / 60) < 1e-9 for dt in calls)
    loop.pump(0.0001)             # remainder 0.0001+0.05-3/60 < DT -> no step
    assert len(calls) == 3

def test_spiral_of_death_clamp():
    calls = []
    loop = GameLoop(update=lambda dt: calls.append(dt), render=lambda a: None, headless=True)
    loop.pump(2.0)                # huge frame; clamp to MAX_STEPS_PER_FRAME
    assert len(calls) == loop.MAX_STEPS_PER_FRAME
