# CyberSecOS on Windows: Quick Start

## Option A: install it (easiest)

Nothing else is needed: .NET and the CyberSecOS server are bundled, and Python
is not required.

1. Open the repository's **Actions** tab on GitHub, open the latest successful
   **CI** run, and download the **CyberSecOS-Setup** artifact at the bottom of
   the page (you must be signed in to GitHub). Builds are kept for 30 days.
2. Unzip it and run **`CyberSecOS-Setup-<version>.exe`**. It installs for your
   Windows user only, so it does not ask for administrator rights.
   If SmartScreen warns about an unrecognised app, choose
   **More info → Run anyway**: the build is not code-signed yet.
3. Leave **Launch CyberSecOS** ticked on the last page, or start it later from
   the Start menu (or the desktop shortcut, if you chose one).
4. A small "Starting CyberSecOS..." window shows while the server starts. The
   very first start loads the whole course catalogue and can take up to about
   half a minute.
5. Create your account on the first screen, then sign in with it from then on.

The app starts its own server when it opens and stops it when you close the
app. Your data (database, signing key and `server.log`) lives in
`%LOCALAPPDATA%\CyberSecOS\data`. Uninstalling keeps it; delete that folder too
if you want a clean slate. Installing a newer build over an old one keeps your
progress.

A portable build (**CyberSecOS-windows-portable**, no installer) is attached to
the same CI run: unzip it anywhere and run `CyberSecOS.exe`.

## Option B: build from source

Requires Git, Python 3.11+ and the .NET 9 SDK (https://dotnet.microsoft.com/download/dotnet/9.0).

```powershell
git clone https://github.com/aharris291-hue/CyberSecOS.git
cd CyberSecOS

# Terminal 1: API
cd backend
python -m pip install -r requirements.txt
python -m uvicorn app.main:create_app --factory --port 8000

# Terminal 2: desktop app
cd frontend
dotnet run --project src/CyberSecOS.Desktop
```

To build the installer yourself, see "Packaging the Windows installer" in
`docs/10_DEVELOPMENT.md`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| "The CyberSecOS server stopped while starting" | Open `%LOCALAPPDATA%\CyberSecOS\data\server.log` for the reason; antivirus quarantining `cybersecos-server.exe` is the usual one |
| "Cannot reach the CyberSecOS server" on the login screen | Close and reopen the app; in a source build, start the API (Terminal 1) first |
| Another program uses port 8000 | Nothing to do: the installed app moves its server to a free port automatically |
| Forgot your password | Delete `%LOCALAPPDATA%\CyberSecOS\data\cybersecos.db` to start over (this erases progress) |
