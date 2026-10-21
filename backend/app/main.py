"""FastAPI application factory."""

from __future__ import annotations

from collections.abc import AsyncIterator
from contextlib import asynccontextmanager

from fastapi import FastAPI

from . import __version__
from .config import Settings
from .database import create_db_engine, create_session_factory, init_db
from .routers import analytics, auth, courses, dashboard, exams, labs, planner, progress, users
from .services.seed import seed_database


def create_app(settings: Settings | None = None) -> FastAPI:
    settings = settings or Settings.from_env()

    @asynccontextmanager
    async def lifespan(app: FastAPI) -> AsyncIterator[None]:
        settings.data_dir.mkdir(parents=True, exist_ok=True)
        engine = create_db_engine(settings.database_url)
        init_db(engine)
        app.state.engine = engine
        app.state.session_factory = create_session_factory(engine)
        if settings.seed_content:
            with app.state.session_factory() as session:
                seed_database(session)
        try:
            yield
        finally:
            engine.dispose()

    app = FastAPI(title="CyberSecOS API", version=__version__, lifespan=lifespan)
    app.state.settings = settings

    @app.get("/api/health", tags=["system"])
    def health() -> dict[str, str]:
        return {"status": "ok", "version": __version__}

    app.include_router(auth.router)
    app.include_router(users.router)
    app.include_router(courses.router)
    app.include_router(progress.router)
    app.include_router(labs.router)
    app.include_router(dashboard.router)
    app.include_router(exams.router)
    app.include_router(planner.router)
    app.include_router(analytics.router)

    return app
