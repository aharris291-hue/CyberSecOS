"""The standalone server entry point used by the Windows installer."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import server

BACKEND = Path(__file__).resolve().parent.parent


def test_installed_data_lives_in_local_app_data(monkeypatch, tmp_path):
    monkeypatch.setenv("LOCALAPPDATA", str(tmp_path))
    assert server.default_data_dir() == tmp_path / "CyberSecOS" / "data"


def test_parent_watch_returns_when_process_has_exited():
    child = subprocess.Popen([sys.executable, "-c", "pass"])
    child.wait()
    server._wait_for_process_exit(child.pid)


def test_bundle_spec_ships_the_seed_content():
    spec = (BACKEND / "cybersecos-server.spec").read_text(encoding="utf-8")
    assert '("app/seed", "app/seed")' in spec
