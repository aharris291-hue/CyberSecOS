"""Login and first-run registration for the single CyberSecOS learner."""

from __future__ import annotations

from datetime import timedelta

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy import func, select
from sqlalchemy.orm import Session

from ..config import Settings
from ..database import get_db, utcnow
from ..models import User
from ..schemas import AuthStatus, LoginRequest, RegisterRequest, TokenResponse, UserOut
from ..security import (
    UNPROCESSABLE,
    create_access_token,
    get_settings,
    hash_password,
    password_problems,
    verify_password,
)

router = APIRouter(prefix="/api/auth", tags=["auth"])

INVALID_CREDENTIALS = "Invalid username or password"


def _user_count(db: Session) -> int:
    return db.scalar(select(func.count()).select_from(User)) or 0


def _token_response(user: User, settings: Settings) -> TokenResponse:
    token, expires_in = create_access_token(user, settings)
    return TokenResponse(access_token=token, expires_in=expires_in, user=UserOut.model_validate(user))


@router.get("/status", response_model=AuthStatus)
def auth_status(db: Session = Depends(get_db)) -> AuthStatus:
    """Tells the client whether to show the first-run setup or the login form."""
    return AuthStatus(registered=_user_count(db) > 0)


@router.post("/register", response_model=TokenResponse, status_code=status.HTTP_201_CREATED)
def register(
    body: RegisterRequest, db: Session = Depends(get_db), settings: Settings = Depends(get_settings)
) -> TokenResponse:
    """Create the learner account. CyberSecOS is single-user, so this only works once."""
    if _user_count(db) > 0:
        raise HTTPException(status.HTTP_409_CONFLICT, "An account already exists on this installation")
    problems = password_problems(body.password)
    if problems:
        raise HTTPException(UNPROCESSABLE, "Password " + ", ".join(problems))
    user = User(
        username=body.username.lower(),
        password_hash=hash_password(body.password),
        display_name=body.display_name.strip(),
        last_login_at=utcnow(),
    )
    db.add(user)
    db.commit()
    return _token_response(user, settings)


@router.post("/login", response_model=TokenResponse)
def login(body: LoginRequest, db: Session = Depends(get_db), settings: Settings = Depends(get_settings)) -> TokenResponse:
    user = db.scalar(select(User).where(User.username == body.username.lower()))
    now = utcnow()
    if user is not None and user.locked_until is not None and user.locked_until > now:
        retry_after = int((user.locked_until - now).total_seconds()) + 1
        raise HTTPException(
            status.HTTP_423_LOCKED,
            f"Too many failed attempts. Try again in {retry_after} seconds.",
            headers={"Retry-After": str(retry_after)},
        )
    if user is None or not verify_password(body.password, user.password_hash):
        if user is not None:
            user.failed_login_attempts += 1
            if user.failed_login_attempts >= settings.max_failed_logins:
                user.locked_until = now + timedelta(minutes=settings.lockout_minutes)
                user.failed_login_attempts = 0
            db.commit()
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, INVALID_CREDENTIALS)
    user.failed_login_attempts = 0
    user.locked_until = None
    user.last_login_at = now
    db.commit()
    return _token_response(user, settings)
