@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Asset File Name Migration

rem ===========================================================================
rem  XNA CnCNet client asset file-name migration helper (English)
rem
rem  Usage:
rem    MigrateAssetFileNames.bat [game folder | Resources folder] [/nopause]
rem
rem    - No argument: asks for a game folder if no Resources folder is found
rem      next to this script.
rem    - A folder can be dragged and dropped onto this file.
rem    - /nopause: do not wait for a key press at the end (for automation).
rem
rem  Actions:
rem    1. Rename legacy tunnel negotiation textures:
rem         negotiation-failed.png   -> negotiationFailed.png
rem         negotiation-failed_c.png -> negotiationFailed_c.png
rem    2. Create the 3.0.0 OptionsWindow tab textures (78 x 23):
rem         78pxtab.png / 78pxtab_c.png
rem       Source priority: 78pxbtn.png, then 75pxbtn.png (with _c variants).
rem       Source art is resized to 78 px width with embedded PowerShell code,
rem       so this .bat works as a standalone file.
rem       Existing 78pxtab files are checked and resized if their width is not 78.
rem
rem  Existing files are never overwritten by plain copies; only wrong-width
rem  78pxtab files are corrected. The script is safe to re-run.
rem  To migrate more files, add CALL lines in the :process_dir label.
rem ===========================================================================

set "NO_PAUSE="
if /i "%~2"=="/nopause" set "NO_PAUSE=1"

set /a RENAMED=0
set /a RESIZED=0
set /a FIXED=0
set /a COPIED=0
set /a SKIPPED=0
set /a FAILED=0
set /a WARNED=0

rem Embedded image resizer (System.Drawing). RZ_IN, RZ_OUT, RZ_W, RZ_H are
rem passed through environment variables to avoid command-line quoting issues.
rem Exit codes: 0 = written, 1 = failed, 2 = already at the requested size.
set "RZ_CMD=$ErrorActionPreference='Stop'; try { Add-Type -AssemblyName System.Drawing; $i=[System.Drawing.Image]::FromFile($env:RZ_IN); try { $w=[int]$env:RZ_W; $h=if(([int]$env:RZ_H) -gt 0){[int]$env:RZ_H}else{$i.Height}; if($i.Width -eq $w -and $i.Height -eq $h){ if($env:RZ_OUT -ne $env:RZ_IN){ Copy-Item -LiteralPath $env:RZ_IN -Destination $env:RZ_OUT -Force }; exit 2 }; $b=New-Object System.Drawing.Bitmap($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb); try { $g=[System.Drawing.Graphics]::FromImage($b); try { $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy; $g.CompositingQuality=[System.Drawing.Drawing2D.CompositingQuality]::HighQuality; $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality; $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::HighQuality; $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic; $g.DrawImage($i,(New-Object System.Drawing.Rectangle(0,0,$w,$h))) } finally { $g.Dispose() }; $t=$env:RZ_OUT+'.tmp.png'; $b.Save($t,[System.Drawing.Imaging.ImageFormat]::Png) } finally { $b.Dispose() } } finally { $i.Dispose() }; Move-Item -LiteralPath $t -Destination $env:RZ_OUT -Force; exit 0 } catch { exit 1 }"

set "ROOT=%~1"
if not "%ROOT%"=="" goto :resolve
set "ROOT=%~dp0."

:resolve
for %%I in ("%ROOT%") do set "ROOT=%%~fI"

set "RES=%ROOT%\Resources"
if exist "%RES%\*" goto :run
rem Accept a Resources folder or a theme folder as the target.
if exist "%ROOT%\*.png" set "RES=%ROOT%"
if exist "%RES%\*" goto :run
if exist "%ROOT%\Themes" set "RES=%ROOT%"
if exist "%RES%\*" goto :run

if not "%~1"=="" goto :err_res

echo [INFO] No Resources folder was found next to this script.
echo [INFO] Enter the game folder, or press Enter to quit.
set "ROOT="
set /p "ROOT=Game folder: "
if not defined ROOT goto :end
set "ROOT=%ROOT:"=%"
if "%ROOT%"=="" goto :end
goto :resolve

:run
echo Target: "%RES%"
echo.

call :process_dir "%RES%"
if exist "%RES%\Themes" for /d %%D in ("%RES%\Themes\*") do call :process_dir "%%~fD"
if exist "%RES%\Translations" for /d %%L in ("%RES%\Translations\*") do call :process_dir "%%~fL"
if exist "%RES%\Translations" for /d %%L in ("%RES%\Translations\*") do if exist "%%~fL" for /d %%T in ("%%~fL\*") do call :process_dir "%%~fT"

echo.
echo Summary:
echo   renamed !RENAMED!  resized !RESIZED!  fixed !FIXED!  copied !COPIED!  skipped !SKIPPED!  failed !FAILED!  warnings !WARNED!
echo All done.
goto :end

rem ---------------------------------------------------------------------------
rem  Per-directory migration. Add new rename/copy calls here.
rem ---------------------------------------------------------------------------
:process_dir
call :rename_if_needed "%~1\negotiation-failed.png" "%~1\negotiationFailed.png"
call :rename_if_needed "%~1\negotiation-failed_c.png" "%~1\negotiationFailed_c.png"
call :ensure_pxtab "%~1" "78pxbtn" "78pxtab" "78"
call :ensure_pxtab "%~1" "75pxbtn" "78pxtab" "78"
exit /b 0

rem ---------------------------------------------------------------------------
rem  Ensures <target> tab art exists with the requested width.
rem ---------------------------------------------------------------------------
:ensure_pxtab
call :handle_pxtab "%~1" "%~2.png" "%~3.png" "%~4"
call :handle_pxtab "%~1" "%~2_c.png" "%~3_c.png" "%~4"
exit /b 0

:handle_pxtab
rem %1 = directory, %2 = source name, %3 = target name, %4 = target width
set "SRC=%~1\%~2"
set "DST=%~1\%~3"
if not exist "%SRC%" if not exist "%DST%" exit /b 0

if exist "%DST%" (
    set "RZ_IN=%DST%"
    set "RZ_OUT=%DST%"
    set "RZ_W=%~4"
    set "RZ_H=0"
    powershell -NoProfile -ExecutionPolicy Bypass -Command "%RZ_CMD%" >nul 2>nul
    set "RC=!ERRORLEVEL!"
    if "!RC!"=="2" (
        set /a SKIPPED+=1
        echo [OK] %~3 already %~4 px  ^(%~1^)
        exit /b 0
    )
    if "!RC!"=="0" (
        set /a FIXED+=1
        echo [FIX] Resized %~3 to %~4 px  ^(%~1^)
        exit /b 0
    )
    set /a WARNED+=1
    echo [WARN] Cannot check image size ^(PowerShell exit !RC!^): %DST%
    exit /b 0
)

set "RZ_IN=%SRC%"
set "RZ_OUT=%DST%"
set "RZ_W=%~4"
set "RZ_H=0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "%RZ_CMD%" >nul 2>nul
set "RC=!ERRORLEVEL!"
if "!RC!"=="0" (
    if exist "%DST%" (
        set /a RESIZED+=1
        echo [RESIZE] %~2 -^> %~3  %~4 px  ^(%~1^)
        exit /b 0
    )
)
if "!RC!"=="2" (
    if exist "%DST%" (
        set /a COPIED+=1
        echo [COPY] %~2 -^> %~3  ^(%~1^)
        exit /b 0
    )
)

copy /y "%SRC%" "%DST%" >nul
if errorlevel 1 (
    set /a FAILED+=1
    echo [FAIL] Copy failed: %~2 -^> %~3  ^(%~1^)
) else (
    set /a COPIED+=1
    echo [COPY] %~2 -^> %~3  ^(width not verified, PowerShell exit !RC!^)
)
exit /b 0

:rename_if_needed
if not exist "%~1" exit /b 0
if exist "%~2" (
    set /a SKIPPED+=1
    echo [SKIP] Target already exists: %~2
    exit /b 0
)
move /y "%~1" "%~2" >nul
if errorlevel 1 (
    set /a FAILED+=1
    echo [FAIL] Rename failed: %~1
) else (
    set /a RENAMED+=1
    echo [RENAME] %~1 -^> %~2
)
exit /b 0

:err_res
echo [ERROR] No Resources folder found under: "%ROOT%"
goto :end

:end
echo.
if not defined NO_PAUSE pause
exit /b 0
