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
    assert response.status_code == 204
    assert client.post("/api/auth/login", json={"username": "adam", "password": PASSWORD}).status_code == 401
    assert client.post("/api/auth/login", json={"username": "adam", "password": new_password}).status_code == 200


def test_change_password_requires_current_password(client, auth_headers):
    response = client.post(
        "/api/users/me/password",
        headers=auth_headers,
        json={"current_password": "not-it-123456", "new_password": "Another-Secret-77"},
    )
    assert response.status_code == 400
