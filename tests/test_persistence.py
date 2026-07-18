import json
from joust import persistence


def test_round_trip(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    rows = [("AAA", 1000 * i) for i in range(10)]
    persistence.save_scores(rows)
    assert persistence.load_scores() == rows


def test_corrupt_resets(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    d = tmp_path / "JoustClone"; d.mkdir()
    (d / "highscores.json").write_text("{nope")
    assert len(persistence.load_scores()) == 10   # defaults


def test_overflow_score_resets_to_defaults(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    directory = tmp_path / "JoustClone"
    directory.mkdir()
    (directory / "highscores.json").write_text('[["AAA", 1e309]]')

    assert persistence.load_scores() == persistence.DEFAULT_SCORES


def test_atomic_no_tmp_left(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    persistence.save_scores([("AAA", 1)])
    left = [p.name for p in (tmp_path / "JoustClone").iterdir()]
    assert left == ["highscores.json"]
