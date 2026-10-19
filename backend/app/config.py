"""Runtime configuration, read from environment variables."""

from __future__ import annotations

import os
import secrets
from dataclasses import dataclass, field
from pathlib import Path

DEFAULT_DATA_DIR = Path(__file__).resolve().parent.parent / "data"


def _load_or_create_secret(data_dir: Path) -> str:
    """Return a persistent JWT signing secret stored in the data directory.

    A single-user desktop install has no secret manager, so the secret is
    generated once and kept next to the database with owner-only permissions.
    """
    secret_file = data_dir / ".jwt_secret"
    if secret_file.exists():
        return secret_file.read_text(encoding="utf-8").strip()
    data_dir.mkdir(parents=True, exist_ok=True)
    secret = secrets.token_urlsafe(48)
    secret_file.write_text(secret, encoding="utf-8")
    try:
        secret_file.chmod(0o600)
    except OSError:
        pass
    return secret


@dataclass(frozen=True)
class Settings:
    data_dir: Path
    database_url: str
    jwt_secret: str
    jwt_algorithm: str = "HS256"
    access_token_minutes: int = 12 * 60
    max_failed_logins: int = 5
    lockout_minutes: int = 5
    seed_content: bool = True
    cors_origins: list[str] = field(default_factory=list)

    @classmethod
    def from_env(cls) -> "Settings":
        data_dir = Path(os.environ.get("CYBERSECOS_DATA_DIR", DEFAULT_DATA_DIR))
        database_url = os.environ.get(
            "CYBERSECOS_DATABASE_URL", f"sqlite:///{(data_dir / 'cybersecos.db').as_posix()}"
        )
        jwt_secret = os.environ.get("CYBERSECOS_JWT_SECRET") or _load_or_create_secret(data_dir)
        return cls(
            data_dir=data_dir,
            database_url=database_url,
            jwt_secret=jwt_secret,
            access_token_minutes=int(os.environ.get("CYBERSECOS_TOKEN_MINUTES", 12 * 60)),
            seed_content=os.environ.get("CYBERSECOS_SEED", "1") != "0",
        )
