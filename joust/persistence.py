"""High-score persistence."""

from __future__ import annotations

import json
import os
from pathlib import Path


DEFAULT_SCORES = [("AAA", 0) for _ in range(10)]


def _path():
    base = Path(os.environ.get("APPDATA", Path.home()))
    return base / "JoustClone" / "highscores.json"


def load_scores():
    try:
        data = json.loads(_path().read_text(encoding="utf-8"))
        rows = [(str(initials)[:3].upper(), int(score)) for initials, score in data]
        if not rows:
            raise ValueError("empty score table")
        if any(score < 0 or score > 9_999_999 for _, score in rows):
            raise ValueError("score outside valid range")
        return rows[:10]
    except (OSError, TypeError, ValueError, OverflowError, json.JSONDecodeError):
        return list(DEFAULT_SCORES)


def save_scores(rows):
    path = _path()
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(".tmp")
    temporary.write_text(json.dumps(list(rows)), encoding="utf-8")
    os.replace(temporary, path)
