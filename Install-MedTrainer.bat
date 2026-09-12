@echo off
setlocal EnableDelayedExpansion
title SS14 MedBay Trainer Installer (Starlight)

set "GIT_TERMINAL_PROMPT=0"

set "INSTALL_DIR=%~dp0"
if "%INSTALL_DIR:~-1%"=="\" set "INSTALL_DIR=%INSTALL_DIR:~0,-1%"

set "STARLIGHT_REPO=https://github.com/ss14Starlight/space-station-14.git"
set "STARLIGHT_BRANCH=starlight-dev"
set "STARLIGHT_COMMIT=1f4aceafba39a2c20b39009fc185dcad2c8ae800"

set "MOD_REPO=https://github.com/etu-brute/ss14-medbaytrainer-starlight.git"
set "MOD_BRANCH=main"
set "MOD_COMMIT="
set "MOD_SUBDIR=medtrainer-basefiles"

cls
echo.
echo  =====================================================
echo    SS14 MEDBAY TRAINER - INSTALLER (Starlight fork)
echo    ported by Brutus / etu_brute
echo  =====================================================
echo.
echo  Install location: %INSTALL_DIR%
echo.

:: -- Check prerequisites ------------------------------------------------
echo [1/6] Checking prerequisites...

dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo  [FAIL] .NET SDK not found.
    echo  Please install .NET SDK 9.0 or later from:
    echo  https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%v"
echo  [OK] .NET SDK %DOTNET_VER% found.

git --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo  [FAIL] Git not found.
    echo  Please install Git from:
    echo  https://git-scm.com/download/win
    echo.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('git --version 2^>nul') do set "GIT_VER=%%v"
echo  [OK] %GIT_VER% found.
echo.

:: -- Clone Starlight ------------------------------------------------------
echo [2/6] Cloning Starlight ^(%STARLIGHT_BRANCH%^) with all dependencies...
echo  This will take several minutes depending on your connection.
echo.
git clone --recurse-submodules --branch %STARLIGHT_BRANCH% --progress "%STARLIGHT_REPO%" "%INSTALL_DIR%\ss14src" 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo  [FAIL] Clone failed. Check your internet connection and try again.
    pause
    exit /b 1
)
echo.
echo  [OK] Starlight cloned.
echo.

echo  Checking out the known-compatible Starlight commit ^(%STARLIGHT_COMMIT%^)...
pushd "%INSTALL_DIR%\ss14src"
git checkout %STARLIGHT_COMMIT% >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo  [FAIL] Could not check out the pinned Starlight commit.
    popd
    pause
    exit /b 1
)
git submodule update --init --recursive >nul 2>&1
popd
echo  [OK] Checked out pinned commit.
echo.

:: -- Download the mod -------------------------------------------------------
echo [3/6] Downloading MedBay Trainer mod files...
git clone --branch %MOD_BRANCH% --progress "%MOD_REPO%" "%INSTALL_DIR%\modsrc" 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo  [FAIL] Could not download the mod files.
    echo  Check your internet connection, and that the mod repository is
    echo  reachable ^(a private repo needs to be made public, or you need
    echo  access to it^).
    echo.
    pause
    exit /b 1
)

if not "%MOD_COMMIT%"=="" (
    pushd "%INSTALL_DIR%\modsrc"
    git checkout %MOD_COMMIT% >nul 2>&1
    popd
)

set "MOD_SRC=%INSTALL_DIR%\modsrc"
if not "%MOD_SUBDIR%"=="" set "MOD_SRC=%INSTALL_DIR%\modsrc\%MOD_SUBDIR%"

if not exist "%MOD_SRC%\Content.Server\MedTraining" (
    echo.
    echo  [FAIL] Downloaded mod files don't look right - missing
    echo  Content.Server\MedTraining under %MOD_SRC%.
    echo  Aborting so nothing gets corrupted.
    echo.
    pause
    exit /b 1
)
echo  [OK] Mod files downloaded.
echo.

echo  Applying mod files over Starlight ^(mod files win on conflicts^)...
xcopy /e /i /y /q "%INSTALL_DIR%\ss14src\*" "%INSTALL_DIR%\_starlight_src\" >nul
xcopy /e /i /y /q "%MOD_SRC%\Content.Server" "%INSTALL_DIR%\_starlight_src\Content.Server" >nul
xcopy /e /i /y /q "%MOD_SRC%\Content.Client" "%INSTALL_DIR%\_starlight_src\Content.Client" >nul
xcopy /e /i /y /q "%MOD_SRC%\Content.Shared" "%INSTALL_DIR%\_starlight_src\Content.Shared" >nul
xcopy /e /i /y /q "%MOD_SRC%\Resources" "%INSTALL_DIR%\_starlight_src\Resources" >nul
xcopy /y /q "%MOD_SRC%\server_config.toml" "%INSTALL_DIR%\_starlight_src\" >nul
xcopy /e /i /y /q "%INSTALL_DIR%\_starlight_src\*" "%INSTALL_DIR%\" >nul
rmdir /s /q "%INSTALL_DIR%\_starlight_src"
rmdir /s /q "%INSTALL_DIR%\ss14src"
rmdir /s /q "%INSTALL_DIR%\modsrc"
echo  [OK] Mod applied on top of Starlight.
echo.

:: -- Build ------------------------------------------------------------------
echo [4/6] Building - this will take several minutes...
echo.

:: Kill any stuck Roslyn build-server process first. Content.Client and
:: Content.Server both compile Content.Shared; if a prior build was
:: interrupted (or a server/client from an earlier run is still open),
:: VBCSCompiler can be left holding a lock on Content.Shared.dll and the
:: second build fails with CS2012 "file may be locked by VBCSCompiler".
dotnet build-server shutdown >nul 2>&1

dotnet build "%INSTALL_DIR%\Content.Server\Content.Server.csproj" --configuration Tools /p:WarningLevel=0
if %ERRORLEVEL% neq 0 (
    echo  [FAIL] Server build failed.
    pause
    exit /b 1
)
echo  [OK] Server built.
echo.

dotnet build-server shutdown >nul 2>&1

dotnet build "%INSTALL_DIR%\Content.Client\Content.Client.csproj" --configuration Tools /p:WarningLevel=0
if %ERRORLEVEL% neq 0 (
    echo  [FAIL] Client build failed.
    echo  If this is CS2012 "file may be locked by VBCSCompiler", close any
    echo  running server/client windows and run this installer again.
    pause
    exit /b 1
)
echo  [OK] Client built.
echo.

:: -- Create launch scripts ---------------------------------------------------
echo [5/6] Creating launch scripts...

:: StartServer.bat
> "%INSTALL_DIR%\StartServer.bat" (
    echo @echo off
    echo cd /d "%%~dp0"
    echo dotnet run --project "%%~dp0Content.Server\Content.Server.csproj" --configuration Tools -- --config-file "%%~dp0server_config.toml"
    echo pause
)

:: StartClient.bat
> "%INSTALL_DIR%\StartClient.bat" (
    echo @echo off
    echo cd /d "%%~dp0"
    echo dotnet run --project "%%~dp0Content.Client\Content.Client.csproj" --configuration Tools
    echo pause
)

:: Run Medical Trainer.bat - no cd needed, full project path handles spaces fine
> "%INSTALL_DIR%\Run Medical Trainer.bat" (
    echo @echo off
    echo set "DIR=%%~dp0"
    echo if "%%DIR:~-1%%"=="^\^" set "DIR=%%DIR:~0,-1%%"
    echo start "MedBay Trainer - Server" cmd /k dotnet run --project "%%DIR%%\Content.Server\Content.Server.csproj" --configuration Tools -- --config-file "%%DIR%%\server_config.toml"
    echo timeout /t 5 /nobreak ^>nul
    echo start "MedBay Trainer - Client" cmd /k dotnet run --project "%%DIR%%\Content.Client\Content.Client.csproj" --configuration Tools
)

echo  [OK] Launch scripts created.
echo.

:: -- Clean up Starlight's own dev-only files ---------------------------------
echo [6/6] Cleaning up unused files...

for %%F in (
    "runclient.bat" "runclient.sh"
    "runclient-Release.bat" "runclient-Release.sh"
    "runclient-Tools.bat" "runclient-Tools.sh"
    "runserver.bat" "runserver.sh"
    "runserver-Release.bat" "runserver-Release.sh"
    "runserver-Tools.bat" "runserver-Tools.sh"
    "runclientserver-Release.bat" "runclientserver-Tools.bat"
    "RUN_THIS.py" "MAP_FIX.py" "Starlightify.py" "auto_render.py"
    "shell.nix" "flake.nix" "flake.lock"
    "SECURITY.md" "CODE_OF_CONDUCT.md" "CONTRIBUTING.md"
    "bors.toml" "omnisharp.json" "SpaceStation14.slnx.DotSettings"
) do (
    if exist "%INSTALL_DIR%\%%~F" del /q "%INSTALL_DIR%\%%~F" >nul 2>&1
)

echo  [OK] Cleaned up. Only the mod's launch scripts and the game files remain.
echo.

:: -- Done ---------------------------------------------------------------------
echo  =====================================================
echo    INSTALLATION COMPLETE!
echo  =====================================================
echo.
echo  Installed to: %INSTALL_DIR%
echo.
echo  To play:
echo    - Run "Run Medical Trainer.bat" to launch both at once
echo    - Or run StartServer.bat and StartClient.bat separately
echo.
echo  If you really-realllyyy like the mod, help me fuel my addiction to orange chicken and tip me at Ko-fi.com/etu_brute
pause
endlocal
