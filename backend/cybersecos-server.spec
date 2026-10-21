# PyInstaller spec for the standalone CyberSecOS API (cybersecos-server.exe).
# Build from the backend folder:  pyinstaller --noconfirm cybersecos-server.spec
# Output: dist/cybersecos-server/ (one folder, so start-up stays fast).
from PyInstaller.utils.hooks import collect_data_files, collect_submodules

datas = [("app/seed", "app/seed")] + collect_data_files("reportlab")
hiddenimports = (
    collect_submodules("app")
    + collect_submodules("uvicorn")
    + collect_submodules("reportlab")
)

a = Analysis(
    ["server.py"],
    pathex=["."],
    datas=datas,
    hiddenimports=hiddenimports,
    excludes=["tkinter", "pytest"],
)
pyz = PYZ(a.pure)
exe = EXE(
    pyz,
    a.scripts,
    [],
    exclude_binaries=True,
    name="cybersecos-server",
    console=True,
    upx=False,
)
coll = COLLECT(exe, a.binaries, a.datas, name="cybersecos-server", upx=False)
