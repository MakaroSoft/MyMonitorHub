@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem  Build mymonitorhub_shr.dll  (Windows stub for JNI testing)
rem ============================================================

set SRC=mymonitorhub_shr.c
set OUT=mymonitorhub_shr.dll

rem ---- Resolve JDK path ----

if not "%~1"=="" (
    set "JDK=%~1"
    goto :have_jdk
)

if not "%JAVA_HOME%"=="" (
    set "JDK=%JAVA_HOME%"
    goto :have_jdk
)

rem -- Scan common install roots for a jdk* folder containing jni.h --
for %%R in (
    "C:\Program Files\Eclipse Adoptium"
    "C:\Program Files\Microsoft"
    "C:\Program Files\Amazon Corretto"
    "C:\Program Files\BellSoft"
    "C:\Program Files\Java"
    "C:\Program Files (x86)\Java"
) do (
    if exist "%%~R" (
        for /d %%J in ("%%~R\jdk*") do (
            if exist "%%~J\include\jni.h" (
                set "JDK=%%~J"
                goto :have_jdk
            )
        )
    )
)

echo ERROR: Cannot find a JDK with JNI headers.
echo.
echo Install Eclipse Temurin 8 JDK from:
echo   https://adoptium.net/temurin/releases/?version=8
echo.
echo Or set JAVA_HOME to your JDK root, e.g.:
echo   set "JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-8.0.xxx-hotspot"
echo   build.bat
echo.
echo Or pass the JDK path as the first argument:
echo   build.bat "C:\Program Files\Eclipse Adoptium\jdk-8.0.xxx-hotspot"
exit /b 1

:have_jdk
if not exist "%JDK%\include\jni.h" (
    echo ERROR: "%JDK%" does not contain include\jni.h
    echo Make sure you are pointing to a JDK, not a JRE.
    exit /b 1
)
echo Using JDK: %JDK%

rem ---- Try MinGW/GCC (on PATH, or in known WinGet install location) ----
set "GCC="
where gcc >nul 2>&1
if %ERRORLEVEL% == 0 (
    set "GCC=gcc"
) else (
    rem WinGet installs WinLibs here; check a couple of common variants
    for %%G in (
        "%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs.POSIX.UCRT_Microsoft.Winget.Source_8wekyb3d8bbwe\mingw64\bin\gcc.exe"
        "%LOCALAPPDATA%\Microsoft\WinGet\Packages\BrechtSanders.WinLibs.POSIX.MSVCRT_Microsoft.Winget.Source_8wekyb3d8bbwe\mingw64\bin\gcc.exe"
    ) do (
        if exist %%G ( set "GCC=%%~G" )
    )
)
if not "!GCC!"=="" (
    echo Building with GCC: !GCC!
    "!GCC!" -shared -o "%OUT%" "%SRC%" -I "%JDK%\include" -I "%JDK%\include\win32"
    if !ERRORLEVEL! == 0 goto :success
    echo GCC build failed.
    exit /b 1
)

rem ---- Fall back to MSVC ----
where cl >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo Building with MSVC...
    cl /LD /I "%JDK%\include" /I "%JDK%\include\win32" "%SRC%" /Fe:"%OUT%"
    if !ERRORLEVEL! == 0 goto :success
    echo MSVC build failed.
    exit /b 1
)

echo ERROR: Neither gcc nor cl was found on PATH.
echo.
echo To install MinGW-w64 (recommended^):
echo   winget install MinGW.MinGW
echo   (then open a new command prompt so gcc is on the PATH^)
echo.
echo Or open a Visual Studio Developer Command Prompt and re-run.
exit /b 1

:success
echo.
echo Build successful: %~dp0%OUT%
echo.
echo To run the agent with this stub:
echo   java -Djava.library.path="%~dp0" -jar target\mymonitorhub-java-agent-*.jar
endlocal
