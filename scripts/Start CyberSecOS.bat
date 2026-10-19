@echo off
rem Starts the CyberSecOS API in the background, then opens the desktop app.
rem First run creates a Python virtual environment and installs dependencies.
setlocal
cd /d "%~dp0"

set "PY="
where py >nul 2>nul && set "PY=py -3"
if not defined PY where python >nul 2>nul && set "PY=python"
if not defined PY (
  echo Python 3.11 or newer is required.
  echo Install it from https://www.python.org/downloads/ and tick "Add python.exe to PATH", then run this again.
  pause
  exit /b 1
)

if not exist "backend\.venv\Scripts\python.exe" (
  echo First run: installing the CyberSecOS backend. This takes a minute...
  %PY% -m venv "backend\.venv" || goto :fail
  "backend\.venv\Scripts\python.exe" -m pip install --quiet --disable-pip-version-check -r "backend\requirements.txt" || goto :fail
)

curl -s -o nul http://127.0.0.1:8000/api/health && goto :launch
start "CyberSecOS API" /min /d "%~dp0backend" "%~dp0backend\.venv\Scripts\python.exe" -m uvicorn app.main:create_app --factory --host 127.0.0.1 --port 8000

echo Starting the CyberSecOS API...
for /l %%i in (1,1,30) do (
  curl -s -o nul http://127.0.0.1:8000/api/health && goto :launch
  timeout /t 1 /nobreak >nul
)
echo The API did not start. Check the "CyberSecOS API" window for errors.
pause
exit /b 1

:launch
start "" "%~dp0app\CyberSecOS.exe"
exit /b 0

:fail
echo Setup failed. See the messages above.
pause
exit /b 1
