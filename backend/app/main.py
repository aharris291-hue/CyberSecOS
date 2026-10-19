"""FastAPI application factory."""

from __future__ import annotations

from fastapi import FastAPI

from . import __version__
from .config import Settings


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or Settings.from_env()
    app = FastAPI(title="CyberSecOS API", version=__version__)
    app.state.settings = settings

    @app.get("/api/health", tags=["system"])
    def health() -> dict[str, str]:
        return {"status": "ok", "version": __version__}

    return app

