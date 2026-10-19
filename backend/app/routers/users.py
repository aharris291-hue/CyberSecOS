"""The learner's own profile."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, Response, status
from sqlalchemy.orm import Session

from ..database import get_db
from ..models import User
from ..schemas import PasswordChange, ProfileUpdate, UserOut
from ..security import UNPROCESSABLE, get_current_user, hash_password, password_problems, verify_password

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


@router.post("/me/password", status_code=status.HTTP_204_NO_CONTENT)
def change_password(
    body: PasswordChange, user: User = Depends(get_current_user), db: Session = Depends(get_db)
) -> Response:
    if not verify_password(body.current_password, user.password_hash):
        raise HTTPException(status.HTTP_400_BAD_REQUEST, "Current password is incorrect")
    problems = password_problems(body.new_password)
    if problems:
        raise HTTPException(UNPROCESSABLE, "Password " + ", ".join(problems))
    user.password_hash = hash_password(body.new_password)
    db.commit()
    return Response(status_code=status.HTTP_204_NO_CONTENT)
