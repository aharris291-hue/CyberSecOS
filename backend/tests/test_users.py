from .conftest import PASSWORD


def test_read_profile(client, auth_headers):
    body = client.get("/api/users/me", headers=auth_headers).json()
    assert body["display_name"] == "Adam"
    assert body["weekly_goal_hours"] == 10


def test_update_profile(client, auth_headers):
    response = client.patch(
        "/api/users/me",
        headers=auth_headers,
        json={"display_name": "  Adam H  ", "target_role": "Detection Engineer", "weekly_goal_hours": 15},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["display_name"] == "Adam H"
    assert body["target_role"] == "Detection Engineer"
    assert body["weekly_goal_hours"] == 15


def test_weekly_goal_is_bounded(client, auth_headers):
    response = client.patch("/api/users/me", headers=auth_headers, json={"weekly_goal_hours": 0})
    assert response.status_code == 422


def test_change_password(client, auth_headers):
    new_password = "Another-Secret-77"
    response = client.post(
        "/api/users/me/password",
        headers=auth_headers,
        json={"current_password": PASSWORD, "new_password": new_password},
    )
    assert response.status_code == 200
    # A fresh token comes back so the caller stays signed in.
    assert response.json()["access_token"]
    assert client.post("/api/auth/login", json={"username": "adam", "password": PASSWORD}).status_code == 401
    assert client.post("/api/auth/login", json={"username": "adam", "password": new_password}).status_code == 200


def test_change_password_invalidates_old_tokens(client, auth_headers):
    """Tokens minted before the change stop authenticating; the returned one works."""
    changed = client.post(
        "/api/users/me/password",
        headers=auth_headers,
        json={"current_password": PASSWORD, "new_password": "Another-Secret-77"},
    )
    assert changed.status_code == 200

    # The token used to make the change (an older session) is now rejected.
    assert client.get("/api/users/me", headers=auth_headers).status_code == 401

    # The fresh token returned by the change still works.
    new_headers = {"Authorization": f"Bearer {changed.json()['access_token']}"}
    assert client.get("/api/users/me", headers=new_headers).status_code == 200


def test_new_token_survives_a_second_change(client, auth_headers):
    first = client.post(
        "/api/users/me/password",
        headers=auth_headers,
        json={"current_password": PASSWORD, "new_password": "Another-Secret-77"},
    ).json()
    first_headers = {"Authorization": f"Bearer {first['access_token']}"}

    second = client.post(
        "/api/users/me/password",
        headers=first_headers,
        json={"current_password": "Another-Secret-77", "new_password": "Third-Secret-88"},
    )
    assert second.status_code == 200
    # The first new token is now stale; the second one works.
    assert client.get("/api/users/me", headers=first_headers).status_code == 401
    second_headers = {"Authorization": f"Bearer {second.json()['access_token']}"}
    assert client.get("/api/users/me", headers=second_headers).status_code == 200


def test_change_password_requires_current_password(client, auth_headers):
    response = client.post(
        "/api/users/me/password",
        headers=auth_headers,
        json={"current_password": "not-it-123456", "new_password": "Another-Secret-77"},
    )
    assert response.status_code == 400
    # A rejected change must not invalidate the current session.
    assert client.get("/api/users/me", headers=auth_headers).status_code == 200
