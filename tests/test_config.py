from joust import config

def test_playfield_and_timestep():
    assert (config.LOGICAL_W, config.LOGICAL_H) == (640, 360)
    assert config.FPS == 60 and abs(config.DT - 1 / 60) < 1e-9

def test_physics_constants_present_and_sane():
    assert config.GRAVITY > 0
    assert config.FLAP_VY < 0          # y-down: flap is negative
    assert 0 < config.DRAG_X < 1
    assert config.MAX_VX > 0 and config.THRUST_AX > 0

def test_score_table_verbatim():
    assert config.SCORE_TIERS == (500, 750, 1500)
    assert config.EGG_CHAIN == (250, 500, 750, 1000)
    assert config.SCORE_PTERO == 1000
    assert config.BONUS_SURVIVAL == 3000 and config.BONUS_TEAM == 3000
    assert config.EXTRA_LIFE_EVERY == 20000
    assert config.SCORE_PVP == 500
