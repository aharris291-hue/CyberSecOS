from __future__ import annotations

import os
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app

# Fast hashing keeps the suite quick; production uses the bcrypt default cost.
os.environ.setdefault("CYBERSECOS_BCRYPT_ROUNDS", "4")


@pytest.fixture()
def settings(tmp_path: Path) -> Settings:
    return Settings(
        data_dir=tmp_path,
        database_url=f"sqlite:///{(tmp_path / 'test.db').as_posix()}",
        jwt_secret="test-secret-not-for-production-use-0123456789",
    )


@pytest.fixture()
def client(settings: Settings) -> TestClient:
    with TestClient(create_app(settings)) as test_client:
        yield test_client


@pytest.fixture()
def db(client: TestClient):
    """A session bound to the same database the app under test uses."""
    session = client.app.state.session_factory()
    try:
        yield session
    finally:
        session.close()


PASSWORD = "Correct-Horse-42"


@pytest.fixture()
def auth_headers(client: TestClient) -> dict[str, str]:
    """Register the learner and return a bearer header for them."""
    response = client.post(
        "/api/auth/register", json={"username": "adam", "password": PASSWORD, "display_name": "Adam"}
    )
    assert response.status_code == 201, response.text
    return {"Authorization": f"Bearer {response.json()['access_token']}"}
