@echo off
setlocal EnableExtensions EnableDelayedExpansion
title 资源文件名迁移

rem ===========================================================================
rem  XNA CnCNet 客户端资源文件名迁移脚本（中文版）
rem
rem  用法：
rem    MigrateAssetFileNames.bat [游戏目录 | Resources 目录] [/nopause]
rem
rem    - 不传参数：若脚本旁没有 Resources 文件夹，会提示输入游戏目录。
rem    - 可以把游戏文件夹直接拖到本文件上。
rem    - /nopause：结束后不等待按键（用于自动化）。
rem
rem  功能：
rem    1. 重命名旧的隧道协商状态贴图：
rem         negotiation-failed.png   -> negotiationFailed.png
rem         negotiation-failed_c.png -> negotiationFailed_c.png
rem    2. 创建 3.0.0 的 OptionsWindow 标签贴图（78 x 23）：
rem         78pxtab.png / 78pxtab_c.png
rem       来源优先级：78pxbtn.png，其次是 75pxbtn.png（含 _c 变体）。
rem       使用内嵌的 PowerShell 代码把来源贴图缩放为 78 像素宽，
rem       因此本 bat 可以单独复制使用，不依赖其他文件。
rem       已存在的 78pxtab 若宽度不是 78 也会被修正。
rem
rem  不会用普通复制覆盖已有文件，只会修正宽度不正确的 78pxtab。
rem  可安全重复运行。
rem  如需迁移更多文件，在 :process_dir 标签内添加 CALL 行。
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

rem 内嵌图片缩放代码（System.Drawing）。路径与尺寸通过环境变量传递，
rem 避免命令行引号问题。返回码：0=已写入，1=失败，2=尺寸已符合。
set "RZ_CMD=$ErrorActionPreference='Stop'; try { Add-Type -AssemblyName System.Drawing; $i=[System.Drawing.Image]::FromFile($env:RZ_IN); try { $w=[int]$env:RZ_W; $h=if(([int]$env:RZ_H) -gt 0){[int]$env:RZ_H}else{$i.Height}; if($i.Width -eq $w -and $i.Height -eq $h){ if($env:RZ_OUT -ne $env:RZ_IN){ Copy-Item -LiteralPath $env:RZ_IN -Destination $env:RZ_OUT -Force }; exit 2 }; $b=New-Object System.Drawing.Bitmap($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb); try { $g=[System.Drawing.Graphics]::FromImage($b); try { $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy; $g.CompositingQuality=[System.Drawing.Drawing2D.CompositingQuality]::HighQuality; $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality; $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::HighQuality; $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic; $g.DrawImage($i,(New-Object System.Drawing.Rectangle(0,0,$w,$h))) } finally { $g.Dispose() }; $t=$env:RZ_OUT+'.tmp.png'; $b.Save($t,[System.Drawing.Imaging.ImageFormat]::Png) } finally { $b.Dispose() } } finally { $i.Dispose() }; Move-Item -LiteralPath $t -Destination $env:RZ_OUT -Force; exit 0 } catch { exit 1 }"

set "ROOT=%~1"
if not "%ROOT%"=="" goto :resolve
set "ROOT=%~dp0."

:resolve
for %%I in ("%ROOT%") do set "ROOT=%%~fI"

set "RES=%ROOT%\Resources"
if exist "%RES%\*" goto :run
rem 也允许直接传入 Resources 目录或主题目录。
if exist "%ROOT%\*.png" set "RES=%ROOT%"
if exist "%RES%\*" goto :run
if exist "%ROOT%\Themes" set "RES=%ROOT%"
if exist "%RES%\*" goto :run

if not "%~1"=="" goto :err_res

echo [INFO] 本脚本旁边没有找到 Resources 文件夹。
echo [INFO] 请输入游戏目录，直接回车退出。
set "ROOT="
set /p "ROOT=游戏目录: "
if not defined ROOT goto :end
set "ROOT=%ROOT:"=%"
if "%ROOT%"=="" goto :end
goto :resolve

:run
echo 目标: "%RES%"
echo.

call :process_dir "%RES%"
if exist "%RES%\Themes" for /d %%D in ("%RES%\Themes\*") do call :process_dir "%%~fD"
if exist "%RES%\Translations" for /d %%L in ("%RES%\Translations\*") do call :process_dir "%%~fL"
if exist "%RES%\Translations" for /d %%L in ("%RES%\Translations\*") do if exist "%%~fL" for /d %%T in ("%%~fL\*") do call :process_dir "%%~fT"

echo.
echo 统计: 重命名 !RENAMED!  缩放 !RESIZED!  修正 !FIXED!  复制 !COPIED!  跳过 !SKIPPED!  失败 !FAILED!  警告 !WARNED!
echo 全部完成。
goto :end

rem ---------------------------------------------------------------------------
rem  逐目录迁移逻辑。新增迁移项时在此添加 CALL 行。
rem ---------------------------------------------------------------------------
:process_dir
call :rename_if_needed "%~1\negotiation-failed.png" "%~1\negotiationFailed.png"
call :rename_if_needed "%~1\negotiation-failed_c.png" "%~1\negotiationFailed_c.png"
call :ensure_pxtab "%~1" "78pxbtn" "78pxtab" "78"
call :ensure_pxtab "%~1" "75pxbtn" "78pxtab" "78"
exit /b 0

rem ---------------------------------------------------------------------------
rem  确保目标标签贴图存在，且宽度正确。
rem ---------------------------------------------------------------------------
:ensure_pxtab
call :handle_pxtab "%~1" "%~2.png" "%~3.png" "%~4"
call :handle_pxtab "%~1" "%~2_c.png" "%~3_c.png" "%~4"
exit /b 0

:handle_pxtab
rem %1 = 目录, %2 = 来源文件名, %3 = 目标文件名, %4 = 目标宽度
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
        echo [OK] %~3 已是 %~4 像素宽  ^(%~1^)
        exit /b 0
    )
    if "!RC!"=="0" (
        set /a FIXED+=1
        echo [FIX] 已将 %~3 修正为 %~4 像素宽  ^(%~1^)
        exit /b 0
    )
    set /a WARNED+=1
    echo [WARN] 无法检测图片尺寸 ^(PowerShell 退出码 !RC!^): %DST%
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
        echo [RESIZE] %~2 -^> %~3  已缩放为 %~4 像素宽  ^(%~1^)
        exit /b 0
    )
)
if "!RC!"=="2" (
    if exist "%DST%" (
        set /a COPIED+=1
        echo [COPY] %~2 -^> %~3  ^(%~1^)  已复制
        exit /b 0
    )
)

copy /y "%SRC%" "%DST%" >nul
if errorlevel 1 (
    set /a FAILED+=1
    echo [FAIL] 复制失败: %~2 -^> %~3  ^(%~1^)
) else (
    set /a COPIED+=1
    echo [COPY] %~2 -^> %~3  ^(宽度未校验，PowerShell 退出码 !RC!^)
)
exit /b 0

:rename_if_needed
if not exist "%~1" exit /b 0
if exist "%~2" (
    set /a SKIPPED+=1
    echo [SKIP] 目标已存在: %~2
    exit /b 0
)
move /y "%~1" "%~2" >nul
if errorlevel 1 (
    set /a FAILED+=1
    echo [FAIL] 重命名失败: %~1
) else (
    set /a RENAMED+=1
    echo [RENAME] %~1 -^> %~2  已重命名
)
exit /b 0

:err_res
echo [ERROR] 在以下路径未找到 Resources 文件夹: "%ROOT%"
goto :end

:end
echo.
if not defined NO_PAUSE pause
exit /b 0
