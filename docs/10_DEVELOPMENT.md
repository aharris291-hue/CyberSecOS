# Development Guide

## Repository layout

```
backend/                 FastAPI + SQLAlchemy API (Python 3.11+)
  app/
    main.py              Application factory (create_app)
    config.py            Environment-driven settings
    routers/             HTTP endpoints, one module per feature
    services/            Business logic used by the routers
    seed/                Course content and certification catalog (YAML)
  tests/                 PyTest suite
frontend/                WPF desktop client (.NET 9, MVVM)
  src/CyberSecOS.Core    Platform-independent API client, models and view models
  src/CyberSecOS.Desktop WPF views, theming and app startup
  tests/                 xUnit tests for CyberSecOS.Core
docs/                    Requirements and project documentation
```

The view models live in `CyberSecOS.Core` (plain `net9.0`) so they can be unit
tested on any OS. `CyberSecOS.Desktop` only contains XAML views and wiring.

## Running the backend

```bash
cd backend
python -m pip install -r requirements-dev.txt
python -m uvicorn app.main:create_app --factory --host 127.0.0.1 --port 8000
```

Interactive API docs are served at http://127.0.0.1:8000/docs.

Environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `CYBERSECOS_DATA_DIR` | `backend/data` | Where the SQLite database and signing secret live |
| `CYBERSECOS_DATABASE_URL` | `sqlite:///<data dir>/cybersecos.db` | SQLAlchemy URL |
| `CYBERSECOS_JWT_SECRET` | generated into `<data dir>/.jwt_secret` | Token signing key |
| `CYBERSECOS_TOKEN_MINUTES` | `720` | Session length |
| `CYBERSECOS_SEED` | `1` | Load bundled course content at startup (`0` to skip) |

## Running the desktop app (Windows)

```powershell
cd frontend
dotnet run --project src/CyberSecOS.Desktop
```

The client talks to `http://127.0.0.1:8000/` by default. Override it in
`src/CyberSecOS.Desktop/appsettings.json` or with `CYBERSECOS_API_URL`.

## Packaging the Windows installer

The Windows CI job builds the installer; to do the same by hand on Windows
(Python 3.12, the .NET 9 SDK and Inno Setup 6 installed):

```powershell
cd backend
python -m pip install -r requirements.txt pyinstaller
pyinstaller --noconfirm cybersecos-server.spec        # -> backend/dist/cybersecos-server/
cd ../frontend
dotnet publish src/CyberSecOS.Desktop/CyberSecOS.Desktop.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o ../dist/CyberSecOS
Copy-Item -Recurse ../backend/dist/cybersecos-server ../dist/CyberSecOS/server
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" ../installer/CyberSecOS.iss   # -> dist/CyberSecOS-Setup-0.1.0.exe
```

At launch the desktop app looks for `server\cybersecos-server.exe` next to
itself (`LocalBackend` in Core). If a server already answers on the configured
address it uses that one, so `dotnet run` against a hand-started API is
unchanged. Otherwise it starts the bundled server with its data in
`%LOCALAPPDATA%\CyberSecOS\data` (log in `server.log` there), on port 8000 or a
free port if 8000 is taken, and stops it on exit.

## Tests

```bash
cd backend && python -m pytest          # API, services, content standards
cd frontend && dotnet test CyberSecOS.sln  # view models and API client
```

The WPF project builds on Linux/macOS thanks to `EnableWindowsTargeting`, but
it only runs on Windows. CI runs the backend on Ubuntu and the frontend on
Windows (`.github/workflows/ci.yml`).
