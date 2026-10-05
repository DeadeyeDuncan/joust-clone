"""Generator regression checks; run with python -m unittest discover -s unity/tools."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    "progress_site", Path(__file__).with_name("build-progress-site.py"))
site = importlib.util.module_from_spec(spec)
spec.loader.exec_module(site)


class ScreenshotTests(unittest.TestCase):
    def test_fresh_clone_keeps_tracked_screenshots(self):
        with tempfile.TemporaryDirectory() as root:
            destination = Path(root) / "shots"
            destination.mkdir()
            (destination / "arena.png").write_bytes(b"existing capture")
            (destination / "notes.txt").write_text("not an image")
            with patch.object(site, "SHOTS_SRC", str(Path(root) / "missing")), \
                 patch.object(site, "SHOTS_DST", str(destination)):
                self.assertEqual(site.copy_screenshots(), ["arena.png"])

    def test_new_capture_replaces_existing_image(self):
        with tempfile.TemporaryDirectory() as root:
            source, destination = Path(root) / "captures", Path(root) / "shots"
            source.mkdir()
            destination.mkdir()
            (source / "arena.png").write_bytes(b"new capture")
            (destination / "arena.png").write_bytes(b"old capture")
            with patch.object(site, "SHOTS_SRC", str(source)), \
                 patch.object(site, "SHOTS_DST", str(destination)):
                self.assertEqual(site.copy_screenshots(), ["arena.png"])
                self.assertEqual((destination / "arena.png").read_bytes(), b"new capture")
