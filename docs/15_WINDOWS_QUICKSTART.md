# CyberSecOS on Windows: Quick Start

## Option A: download the ready-made build (easiest)

1. Install **Python 3.11 or newer** from https://www.python.org/downloads/ and
   tick **"Add python.exe to PATH"** in the installer. (The desktop app itself
   needs nothing else; .NET is bundled.)
2. Open the repository's **Actions** tab on GitHub, open the latest successful
   **CI** run, and download the **CyberSecOS-windows** artifact at the bottom of
   the page (you must be signed in to GitHub). Builds are kept for 30 days.
3. Unzip it anywhere, for example `C:\CyberSecOS`.
4. Double-click **`Start CyberSecOS.bat`**. The first run installs the backend
   (about a minute), then starts the API in a minimised window and opens the app.
5. Create your account on the first screen, then sign in with it from then on.

Your data (database and signing key) lives in `backend\data` inside that
folder. Keep the folder to keep your progress.

If Windows SmartScreen warns about an unrecognised app, choose
**More info → Run anyway**: the build is not code-signed.

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

## Troubleshooting

| Symptom | Fix |
|---|---|
| "Cannot reach the CyberSecOS server" on the login screen | Start the API (step 4, or Terminal 1), then press Sign in again |
| "Python 3.11 or newer is required" | Install Python with "Add python.exe to PATH" ticked, then rerun the launcher |
| Port 8000 already in use | Stop the other program, or run the API on another port and set `ApiBaseUrl` in `app\appsettings.json` |
| Forgot your password | Delete `backend\data\cybersecos.db` to start over (this erases progress) |
