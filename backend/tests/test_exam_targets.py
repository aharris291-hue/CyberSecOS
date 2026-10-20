"""Exam target-date tracking endpoints."""

from datetime import date, timedelta


def test_no_target_returns_null(client, auth_headers):
    resp = client.get("/api/certifications/secplus/exam/target", headers=auth_headers)
    assert resp.status_code == 200
    assert resp.json() is None


def test_target_requires_login(client):
    assert client.get("/api/certifications/secplus/exam/target").status_code == 401


def test_unknown_certification_is_404(client, auth_headers):
    assert client.get("/api/certifications/nope/exam/target", headers=auth_headers).status_code == 404
    assert (
        client.put(
            "/api/certifications/nope/exam/target",
            headers=auth_headers,
            json={"exam_date": "2026-12-01"},
        ).status_code
        == 404
    )


def test_set_and_get_target(client, auth_headers):
    future = (date.today() + timedelta(days=30)).isoformat()
    put = client.put(
        "/api/certifications/secplus/exam/target",
        headers=auth_headers,
        json={"exam_date": future},
    )
    assert put.status_code == 200
    body = put.json()
    assert body["certification_id"] == "secplus"
    assert body["exam_date"] == future
    assert body["days_until_exam"] == 30

    got = client.get("/api/certifications/secplus/exam/target", headers=auth_headers).json()
    assert got["exam_date"] == future
    assert got["days_until_exam"] == 30


def test_setting_again_updates_in_place(client, auth_headers):
    first = (date.today() + timedelta(days=10)).isoformat()
    second = (date.today() + timedelta(days=20)).isoformat()
    client.put("/api/certifications/secplus/exam/target", headers=auth_headers, json={"exam_date": first})
    client.put("/api/certifications/secplus/exam/target", headers=auth_headers, json={"exam_date": second})
    got = client.get("/api/certifications/secplus/exam/target", headers=auth_headers).json()
    assert got["exam_date"] == second
    assert got["days_until_exam"] == 20


def test_clear_target(client, auth_headers):
    future = (date.today() + timedelta(days=5)).isoformat()
    client.put("/api/certifications/secplus/exam/target", headers=auth_headers, json={"exam_date": future})
    assert client.delete("/api/certifications/secplus/exam/target", headers=auth_headers).status_code == 204
    assert client.get("/api/certifications/secplus/exam/target", headers=auth_headers).json() is None


def test_past_date_gives_negative_countdown(client, auth_headers):
    past = (date.today() - timedelta(days=3)).isoformat()
    body = client.put(
        "/api/certifications/secplus/exam/target",
        headers=auth_headers,
        json={"exam_date": past},
    ).json()
    assert body["days_until_exam"] == -3


def test_target_surfaces_in_dashboard_readiness(client, auth_headers):
    future = (date.today() + timedelta(days=45)).isoformat()
    client.put("/api/certifications/secplus/exam/target", headers=auth_headers, json={"exam_date": future})
    dashboard = client.get("/api/dashboard", headers=auth_headers).json()
    secplus = next(c for c in dashboard["certifications"] if c["id"] == "secplus")
    assert secplus["target_date"] == future
    assert secplus["days_until_exam"] == 45
    other = next(c for c in dashboard["certifications"] if c["id"] != "secplus")
    assert other["target_date"] is None
