import pygame

from joust import config
from joust.states import AttractState, GameOverState, HighScoreEntryState, PlayState


def make_2p():
    ps = PlayState.__new__(PlayState)
    ps.setup_logic(two_player=True)
    return ps


def test_pvp_joust_scores_winner_and_kills_loser():
    ps = make_2p()
    p1, p2 = ps.players
    lives0 = p2.lives
    ps.apply_pvp_joust(winner=p1, loser=p2)
    assert p1.score == config.SCORE_PVP and p2.lives == lives0 - 1
    assert ps.jousted_teammate is True


def test_team_bonus_only_when_clean():
    ps = make_2p()
    ps.wave_n = 6
    ps.jousted_teammate = False
    ps.deaths_this_wave = {1: 0, 2: 1}
    b = ps.wave_end_bonuses()
    assert b == {1: config.BONUS_TEAM, 2: config.BONUS_TEAM}


def test_game_over_when_both_dead():
    ps = make_2p()
    for p in ps.players:
        p.lives = 0
    assert ps.is_game_over()


def test_extra_life_on_each_20k_crossing():
    ps = make_2p()
    p1 = ps.players[0]
    lives0 = p1.lives
    ps.award(1, 19500)
    assert p1.lives == lives0
    ps.award(1, 600)
    assert p1.lives == lives0 + 1
    ps.award(1, 19900)
    assert p1.lives == lives0 + 2


def test_eliminated_player_cannot_regain_life_from_award():
    ps = make_2p()
    p1 = ps.players[0]
    p1.score = 19000
    p1.lives = 0
    p1.alive = False

    ps.award(1, config.BONUS_TEAM)

    assert p1.score == 19000 + config.BONUS_TEAM
    assert p1.lives == 0


def test_wave_bonus_skips_eliminated_player_and_preserves_game_over():
    ps = make_2p()
    p1, p2 = ps.players
    ps.wave_n = config.TEAM_WAVE_FIRST
    ps.deaths_this_wave = {1: 0, 2: 0}
    ps.jousted_teammate = False
    p1.score = 19000
    p1.lives = 0
    p1.alive = False

    ps._finish_wave()

    assert p1.score == 19000
    assert p1.lives == 0
    p2.lives = 0
    p2.alive = False
    assert ps.is_game_over()


def test_attract_start_selects_player_count_from_pad_index():
    state = AttractState.__new__(AttractState)
    selected = []
    state.joysticks = [object(), object()]
    state._start = selected.append

    state.handle_event(pygame.event.Event(pygame.JOYBUTTONDOWN, joy=0, button=7))
    state.handle_event(pygame.event.Event(pygame.JOYBUTTONDOWN, joy=1, button=6))

    assert selected == [False, True]


def test_attract_ignores_pad_not_enumerated_on_entry():
    state = AttractState.__new__(AttractState)
    selected = []
    state.joysticks = [object()]
    state._start = selected.append

    state.handle_event(pygame.event.Event(pygame.JOYBUTTONDOWN, joy=1, button=7))

    assert selected == []


def test_game_over_pad_start_enters_qualifying_high_score(monkeypatch):
    switched = []
    state = GameOverState.__new__(GameOverState)
    state.loop = None
    state.machine = type("Machine", (), {"switch": switched.append})()
    state.assets = None
    state.score = 100
    monkeypatch.setattr("joust.states.persistence.load_scores", lambda: [])

    state.handle_event(pygame.event.Event(pygame.JOYBUTTONDOWN, joy=0, button=7))

    assert isinstance(switched[-1], HighScoreEntryState)


def test_high_score_entry_accepts_pad_hat_and_button(monkeypatch):
    switched = []
    saved = []
    state = HighScoreEntryState.__new__(HighScoreEntryState)
    state.loop = None
    state.machine = type("Machine", (), {"switch": switched.append})()
    state.assets = None
    state.score = 100
    state.initials = [0, 0, 0]
    state.position = 0
    monkeypatch.setattr("joust.states.persistence.load_scores", lambda: [])
    monkeypatch.setattr("joust.states.persistence.save_scores", saved.append)

    state.handle_event(pygame.event.Event(pygame.JOYHATMOTION, joy=0, hat=0, value=(0, 1)))
    state.handle_event(pygame.event.Event(pygame.JOYHATMOTION, joy=0, hat=0, value=(1, 0)))
    state.handle_event(pygame.event.Event(pygame.JOYBUTTONDOWN, joy=0, button=0))

    assert state.initials == [1, 0, 0]
    assert state.position == 1
    assert saved == [[("BAA", 100)]]


class FakePad:
    def __init__(self, axis=0.0, hat=(0, 0)):
        self.axis = axis
        self.hat = hat

    def get_numaxes(self):
        return 1

    def get_axis(self, index):
        assert index == 0
        return self.axis

    def get_numhats(self):
        return 1

    def get_hat(self, index):
        assert index == 0
        return self.hat


def test_centered_pad_does_not_disable_keyboard_movement():
    ps = make_2p()
    ps.joysticks = [FakePad()]
    ps._held.add(pygame.K_RIGHT)

    ps._apply_input()

    assert ps.players[0]._dir == 1


def test_pad_axis_takes_priority_over_hat_and_maps_by_index():
    ps = make_2p()
    ps.joysticks = [FakePad(axis=-0.5, hat=(1, 0)), FakePad(hat=(1, 0))]

    ps._apply_input()

    assert [player._dir for player in ps.players] == [-1, 1]
