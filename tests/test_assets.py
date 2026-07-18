import pygame, pytest
from joust import assets
from tests.test_gen_sprites import REQUIRED


def test_load_provides_every_required_frame():
    pygame.init()
    a = assets.load()
    for name in REQUIRED:
        surf = a.frame(name)
        assert surf.get_width() > 0 and surf.get_height() > 0
        ax, ay = a.anchor(name)
        assert 0 <= ax <= surf.get_width() and 0 <= ay <= surf.get_height()
    assert a.lance("p1_stand") is not None
    assert a.lance("egg_0") is None


def test_corrupt_manifest_triggers_regen_then_error(tmp_path, monkeypatch):
    (tmp_path / "sprites").mkdir(parents=True)
    (tmp_path / "sprites" / "sprites.json").write_text("{broken")
    monkeypatch.setattr(assets, "ASSET_DIR", tmp_path)
    calls = []
    monkeypatch.setattr(assets, "regenerate", lambda: calls.append(1))  # attempts, fixes nothing
    with pytest.raises(assets.AssetError):
        assets.load()
    assert calls == [1]          # regeneration attempted exactly once


def test_malformed_manifest_schema_uses_stale_path(tmp_path, monkeypatch):
    (tmp_path / "sprites").mkdir(parents=True)
    (tmp_path / "sprites" / "sprites.json").write_text('{"sheets": [], "frames": {}}')
    monkeypatch.setattr(assets, "ASSET_DIR", tmp_path)
    monkeypatch.setattr(assets, "REQUIRED", ())
    monkeypatch.setattr(assets, "REQUIRED_SFX", ())
    calls = []
    monkeypatch.setattr(assets, "regenerate", lambda: calls.append(1))
    with pytest.raises(assets.AssetError):
        assets.load()
    assert calls == [1]


def test_audio_init_sets_eight_channels():
    import pygame
    from joust import audio
    pygame.init()
    audio.init()
    assert pygame.mixer.get_num_channels() == 8
