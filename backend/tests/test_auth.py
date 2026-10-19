from datetime import timedelta

import jwt

from app.database import utcnow
from app.models import User

from .conftest import PASSWORD


def _register(client, username="adam", password=PASSWORD):
    return client.post("/api/auth/register", json={"username": username, "password": password, "display_name": "Adam"})


def _login(client, password=PASSWORD, username="adam"):
    return client.post("/api/auth/login", json={"username": username, "password": password})


def test_status_reports_first_run_then_registered(client):
    assert client.get("/api/auth/status").json() == {"registered": False}
    _register(client)
    assert client.get("/api/auth/status").json() == {"registered": True}


def test_register_returns_token_and_profile(client):
    body = _register(client).json()
    assert body["token_type"] == "bearer"
    assert body["user"]["username"] == "adam"
    assert body["user"]["target_role"] == "SOC Analyst"


def test_only_one_account_can_exist(client):
    _register(client)
    response = _register(client, username="someone-else")
    assert response.status_code == 409


def test_weak_password_rejected(client):
    response = _register(client, password="short1")
    assert response.status_code == 422
    assert "at least 10 characters" in response.json()["detail"]
    assert client.get("/api/auth/status").json() == {"registered": False}


def test_password_is_hashed_at_rest(client, db):
    _register(client)
    user = db.query(User).one()
    assert user.password_hash != PASSWORD
    assert user.password_hash.startswith("$2")


def test_login_with_correct_password(client):
    _register(client)
    response = _login(client)
    assert response.status_code == 200
    assert response.json()["user"]["last_login_at"] is not None


def test_login_is_case_insensitive_on_username(client):
    _register(client)
    assert _login(client, username="ADAM").status_code == 200


def test_wrong_password_and_unknown_user_get_same_error(client):
    _register(client)
    wrong = _login(client, password="Wrong-password-1")
    unknown = _login(client, username="nobody")
    assert wrong.status_code == unknown.status_code == 401
    assert wrong.json() == unknown.json()


def test_account_locks_after_repeated_failures(client, settings):
    _register(client)
    for _ in range(settings.max_failed_logins):
        assert _login(client, password="Wrong-password-1").status_code == 401
    locked = _login(client)
    assert locked.status_code == 423
    assert int(locked.headers["Retry-After"]) > 0


def test_lock_expires(client, db, settings):
    _register(client)
    for _ in range(settings.max_failed_logins):
        _login(client, password="Wrong-password-1")
    user = db.query(User).one()
    user.locked_until = utcnow() - timedelta(seconds=1)
    db.commit()
    assert _login(client).status_code == 200


def test_successful_login_resets_failure_counter(client, db):
    _register(client)
    _login(client, password="Wrong-password-1")
    _login(client)
    db.expire_all()
    assert db.query(User).one().failed_login_attempts == 0


def test_protected_endpoint_requires_token(client):
    assert client.get("/api/users/me").status_code == 401


def test_tampered_token_rejected(client, auth_headers):
    token = auth_headers["Authorization"].split()[1]
    forged = jwt.encode(jwt.decode(token, options={"verify_signature": False}), "wrong-key-0123456789-0123456789-0123456789")
    response = client.get("/api/users/me", headers={"Authorization": f"Bearer {forged}"})
    assert response.status_code == 401


def test_expired_token_rejected(client, auth_headers, settings):
    expired = jwt.encode(
        {"sub": "1", "iat": utcnow() - timedelta(hours=2), "exp": utcnow() - timedelta(hours=1)},
        settings.jwt_secret,
        algorithm=settings.jwt_algorithm,
    )
    assert client.get("/api/users/me", headers={"Authorization": f"Bearer {expired}"}).status_code == 401
