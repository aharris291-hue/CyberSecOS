"""Password hashing, access tokens and the current-user dependency."""

from __future__ import annotations

from datetime import timedelta

import os

import bcrypt
import jwt
from fastapi import Depends, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from sqlalchemy.orm import Session

from .config import Settings
from .database import get_db, utcnow
from .models import User

_bearer = HTTPBearer(auto_error=False)

MIN_PASSWORD_LENGTH = 10
# Literal so it works across Starlette versions (the constant was renamed).
UNPROCESSABLE = 422


def hash_password(password: str) -> str:
    return bcrypt.hashpw(password.encode("utf-8"), bcrypt.gensalt(rounds=int(os.environ.get("CYBERSECOS_BCRYPT_ROUNDS", "12")))).decode("ascii")


def verify_password(password: str, password_hash: str) -> bool:
    try:
        return bcrypt.checkpw(password.encode("utf-8"), password_hash.encode("ascii"))
    except ValueError:
        return False


def password_problems(password: str) -> list[str]:
    """Return human-readable reasons a password is too weak (empty when acceptable)."""
    problems = []
    if len(password) < MIN_PASSWORD_LENGTH:
        problems.append(f"must be at least {MIN_PASSWORD_LENGTH} characters")
    if len(password.encode("utf-8")) > 72:
        problems.append("must be at most 72 bytes")
    if not any(c.isalpha() for c in password):
        problems.append("must contain a letter")
    if not any(c.isdigit() for c in password):
        problems.append("must contain a digit")
    return problems


def create_access_token(user: User, settings: Settings) -> tuple[str, int]:
    """Return a signed JWT for the user and its lifetime in seconds."""
    lifetime = timedelta(minutes=settings.access_token_minutes)
    now = utcnow()
    payload = {"sub": str(user.id), "ver": user.token_version, "iat": now, "exp": now + lifetime}
    token = jwt.encode(payload, settings.jwt_secret, algorithm=settings.jwt_algorithm)
    return token, int(lifetime.total_seconds())


def get_settings(request: Request) -> Settings:
    return request.app.state.settings


def get_current_user(
    credentials: HTTPAuthorizationCredentials | None = Depends(_bearer),
    db: Session = Depends(get_db),
    settings: Settings = Depends(get_settings),
) -> User:
    unauthorized = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Not authenticated",
        headers={"WWW-Authenticate": "Bearer"},
    )
    if credentials is None:
        raise unauthorized
    try:
        payload = jwt.decode(credentials.credentials, settings.jwt_secret, algorithms=[settings.jwt_algorithm])
        user_id = int(payload["sub"])
        token_version = int(payload["ver"])
    except (jwt.PyJWTError, KeyError, ValueError):
        raise unauthorized from None
    user = db.get(User, user_id)
    if user is None:
        raise unauthorized
    # A password change bumps token_version, so tokens issued earlier no longer match.
    if token_version != user.token_version:
        raise unauthorized
    return user
