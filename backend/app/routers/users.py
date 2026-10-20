"""The learner's own profile."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from ..config import Settings
from ..database import get_db
from ..models import User
from ..schemas import PasswordChange, ProfileUpdate, TokenResponse, UserOut
from ..security import (
    UNPROCESSABLE,
    create_access_token,
    get_current_user,
    get_settings,
    hash_password,
    password_problems,
    verify_password,
)

router = APIRouter(prefix="/api/users", tags=["users"])


@router.get("/me", response_model=UserOut)
def read_me(user: User = Depends(get_current_user)) -> User:
    return user


@router.patch("/me", response_model=UserOut)
def update_me(body: ProfileUpdate, user: User = Depends(get_current_user), db: Session = Depends(get_db)) -> User:
    for field, value in body.model_dump(exclude_unset=True, exclude_none=True).items():
        setattr(user, field, value.strip() if isinstance(value, str) else value)
    db.commit()
    return user


@router.post("/me/password", response_model=TokenResponse)
def change_password(
    body: PasswordChange,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
    settings: Settings = Depends(get_settings),
) -> TokenResponse:
    if not verify_password(body.current_password, user.password_hash):
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "Current password is incorrect")
    problems = password_problems(body.new_password)
    if problems:
        raise HTTPException(UNPROCESSABLE, "Password " + ", ".join(problems))
    user.password_hash = hash_password(body.new_password)
    # Invalidate every token issued before this change, then hand back a fresh
    # one so the session that made the change stays signed in.
    user.token_version += 1
    db.commit()
    token, expires_in = create_access_token(user, settings)
    return TokenResponse(access_token=token, expires_in=expires_in, user=UserOut.model_validate(user))
