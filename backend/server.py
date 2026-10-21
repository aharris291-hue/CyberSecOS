"""Standalone entry point for the packaged CyberSecOS API.

The Windows installer ships this file frozen by PyInstaller (see
``cybersecos-server.spec``) so learners need no Python install. The desktop app
starts it, passes ``--parent-pid`` and stops it on exit; if the desktop app is
killed instead, the server notices its parent is gone and exits too.
"""

from __future__ import annotations

import argparse
import os
import sys
import threading
import time
from pathlib import Path


def default_data_dir() -> Path:
    """Per-user data folder for an installed build, so the install folder can stay read-only."""
    base = os.environ.get("LOCALAPPDATA") or os.environ.get("XDG_DATA_HOME") or Path.home() / ".local" / "share"
    return Path(base) / "CyberSecOS" / "data"


def _wait_for_process_exit(pid: int) -> None:
    if sys.platform == "win32":
        import ctypes

        synchronize, infinite = 0x00100000, 0xFFFFFFFF
        kernel32 = ctypes.windll.kernel32
        handle = kernel32.OpenProcess(synchronize, False, pid)
        if not handle:
            return  # Already gone.
        kernel32.WaitForSingleObject(handle, infinite)
        kernel32.CloseHandle(handle)
        return
    while True:
        try:
            os.kill(pid, 0)
        except ProcessLookupError:
            return
        except PermissionError:
            pass
        time.sleep(1)


def watch_parent(pid: int) -> None:
    """Exit the server once the process that launched it has exited."""

    def run() -> None:
        _wait_for_process_exit(pid)
        os._exit(0)

    threading.Thread(target=run, name="parent-watch", daemon=True).start()


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Run the CyberSecOS API.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--parent-pid", type=int, help="exit when this process exits")
    args = parser.parse_args(argv)

    if getattr(sys, "frozen", False):
        os.environ.setdefault("CYBERSECOS_DATA_DIR", str(default_data_dir()))
    if args.parent_pid:
        watch_parent(args.parent_pid)

    import uvicorn

    from app.main import create_app

    uvicorn.run(create_app(), host=args.host, port=args.port, log_level="info")


if __name__ == "__main__":
    main()
