# 资源文件名迁移脚本 / Asset File-name Migration Scripts

## 目录结构 / Layout

| 路径 / Path | 说明 / Description |
| --- | --- |
| `en/MigrateAssetFileNames.bat` | 英文版迁移脚本，纯 ASCII，可在任意语言的 Windows 上运行<br>English migration script, pure ASCII, runs on any Windows locale |
| `zh/MigrateAssetFileNames.bat` | 中文版迁移脚本，GBK（代码页 936）编码，适合中文 Windows<br>Chinese migration script, GBK (codepage 936), for Chinese Windows |
| `ResizeImageWidth.ps1` | 可选的独立图片缩放工具；迁移脚本已内置缩放代码，不依赖此文件<br>Optional standalone resize tool; the migration scripts embed their own resize code and do not depend on it |

> [!NOTE]
> `zh` 脚本用 GBK 保存，以便在中文 Windows 的默认 cmd 中直接正确显示中文。非中文系统请使用 `en` 版本。
> The `zh` script is saved in GBK so Chinese text displays correctly in the default cmd on Chinese Windows. Use the `en` script on other locales.

> [!NOTE]
> 两个迁移脚本都是**单文件可用**的：缩放代码已内嵌在 bat 中，只有 `powershell` 可用即可，不需要额外的 ps1 文件。`ResizeImageWidth.ps1` 仅供单独调用或参考。
> Both migration scripts are **self-contained**: the resize code is embedded in the .bat files, so only `powershell` needs to be available. `ResizeImageWidth.ps1` is only for standalone use or reference.

## 用途 / Purpose

把旧版客户端的主题资源文件名更新为 3.0.0 客户端使用的新名称，并生成 **78×23 像素** 的 OptionsWindow 标签贴图，避免标签显示为粉色占位方块（图像缺失）。

Updates legacy theme asset file names to the names used by the 3.0.0 client, and produces **78×23 px** OptionsWindow tab textures so tabs do not render as pink placeholder squares (missing images).

## 用法 / Usage

英文版 / English version:

```bat
Docs\Data\Scripts\en\MigrateAssetFileNames.bat "F:\Game\RedAlert2\online"
```

中文版 / Chinese version:

```bat
Docs\Data\Scripts\zh\MigrateAssetFileNames.bat "F:\Game\红色警戒2\online"
```

- 也可以把游戏文件夹直接拖到 bat 文件上。
- 不传参数时：若脚本旁没有 `Resources`，脚本会提示输入游戏目录；直接回车则退出。
- 第二个参数 `/nopause` 可让脚本结束后不等待按键（用于自动化）。

- You can drag and drop a game folder onto the script.
- With no argument, the script asks for a game folder when no `Resources` folder exists next to it; press Enter to quit.
- Pass `/nopause` as the second argument to skip the final key press (for automation).

支持传入游戏根目录、`Resources` 目录或主题目录。脚本会处理：
The script accepts the game root, the `Resources` folder, or a theme folder. It processes:

- `Resources`
- `Resources\Themes\*`
- `Resources\Translations\*`
- `Resources\Translations\<语言>\<主题>`

## 执行内容 / What it does

| 操作 / Action | 说明 / Description |
| --- | --- |
| 重命名 / Rename | `negotiation-failed.png` → `negotiationFailed.png` |
| 重命名 / Rename | `negotiation-failed_c.png` → `negotiationFailed_c.png` |
| 缩放 / Resize | `78pxbtn.png` → `78pxtab.png`（78×23；源已符合尺寸时直接复制 / copy when already 78 px） |
| 缩放 / Resize | `78pxbtn_c.png` → `78pxtab_c.png` |
| 缩放 / Resize | 没有 78px 贴图时，用 `75pxbtn.png` / `75pxbtn_c.png` 缩放到 78 像素宽<br>Falls back to `75pxbtn.png` / `75pxbtn_c.png` and resizes it to 78 px |
| 修正 / Fix | 已存在的 `78pxtab.png` / `78pxtab_c.png` 若宽度不是 78，会就地修正<br>Existing `78pxtab` files are resized in place when their width is not 78 |

日志标记 / Log markers:

- `[RENAME]` 重命名成功
- `[RESIZE]` 新生成并缩放到目标宽度
- `[COPY]` 直接复制（源尺寸已符合，或 PowerShell 不可用时的回退）
- `[FIX]` 已有贴图宽度被修正
- `[OK]` 目标已存在且宽度正确，无需改动
- `[SKIP]` 重命名目标已存在
- `[FAIL]` / `[WARN]` 失败或无法检测

运行结束会输出统计行，便于确认是否真的发生了改动 / A summary line is printed at the end:

```text
统计: 重命名 0  缩放 2  修正 1  复制 0  跳过 5  失败 0  警告 0
Summary: renamed 0  resized 2  fixed 1  copied 0  skipped 5  failed 0  warnings 0
```

## 故障排查 / Troubleshooting

| 现象 / Symptom | 处理 / Action |
| --- | --- |
| 统计全为 0，且没有 `[RESIZE]`/`[FIX]` | 目标很可能已经是 78 像素宽（查看 `[OK]`）；或目标目录不对（检查输出第一行的“目标/ Target”） |
| 出现 `[WARN] ... PowerShell 退出码 N` | PowerShell 调用失败。确认系统中有 `powershell` 命令（Windows 自带）；`9009` 表示找不到命令，其他负数通常是 PowerShell 启动失败<br>A `[WARN]` means the PowerShell call failed. Verify `powershell` exists; `9009` means the command was not found, other negative codes usually indicate a PowerShell startup failure |
| `[COPY] ... 宽度未校验` | PowerShell 不可用时的回退，文件被直接复制，宽度可能不是 78<br>Fallback used because PowerShell failed; the file was copied as-is and its width may not be 78 |
| 完全没有任何输出 | 用 `/nopause` 以外的正常方式运行，确认脚本窗口有“目标/Target”行；双击运行时脚本会等待按键 |

| Symptom | Action |
| --- | --- |
| Summary is all zeros and no `[RESIZE]`/`[FIX]` | Targets are probably already 78 px wide (see `[OK]`), or the wrong folder was processed (check the `Target` line) |
| `[WARN] ... PowerShell exit N` | The PowerShell call failed. Verify `powershell` exists (included with Windows); `9009` means the command was not found, other negative codes usually indicate a PowerShell startup failure |
| `[COPY] ... width not verified` | Fallback used because PowerShell failed; the file was copied as-is and its width may not be 78 |
| No output at all | Run it normally (not with `/nopause`) and check the `Target` line; double-clicked runs wait for a key press |

## 为什么是缩放/复制而不是重命名 / Why resize or copy instead of rename

`75pxbtn.png` 仍被消息框（`XNAMessageBox`）等控件使用，直接改名会导致这些按钮图像缺失。因此脚本保留原文件，只生成新的 78 像素标签贴图。

`75pxbtn.png` is still used by message boxes (`XNAMessageBox`) and other controls, so renaming it would break those buttons. The script keeps the original file and only creates the new 78 px tab art.

## 安全性 / Safety

- 目标存在且宽度正确时不会改动（`[SKIP]`）。
- 源文件不存在时静默跳过。
- 可以重复运行，结果保持一致。
- 结束后默认等待按键，双击运行不会闪退。
- 若 `powershell` 调用失败（被执行策略阻止等），会回退为直接复制，并标记 `[COPY] ... (width not verified)`。

- Existing correct-size targets are left untouched (`[SKIP]`).
- Missing source files are skipped silently.
- The script is idempotent and safe to re-run.
- It waits for a key press at the end by default, so double-clicking will not close the window immediately.
- If the `powershell` call fails (e.g. blocked by execution policy), the script falls back to a plain copy and logs `[COPY] ... (width not verified)`.

## 单独使用缩放工具 / Using the resize tool standalone

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ResizeImageWidth.ps1 `
    -InputPath "75pxbtn.png" -OutputPath "78pxtab.png" -Width 78
```

可选参数 / Optional parameters:

| 参数 / Parameter | 说明 / Description |
| --- | --- |
| `-Height 23` | 指定高度；默认 `0` 表示保持原高度（标签贴图为 23）<br>Target height; `0` keeps the source height (23 for tab art) |
| `-Mode NearestNeighbor` | 像素风贴图建议用最近邻；默认 `Bicubic`（高质量双三次）<br>Use nearest neighbor for pixel art; default is high-quality bicubic |
| `-OnlyIfDifferent` | 目标已是目标尺寸时不写入，返回码 2<br>Skip writing when the image already has the requested size; exit code 2 |

返回码 / Exit codes: `0` 已写入 / written, `1` 失败 / failure, `2` 已跳过 / skipped.

## 扩展 / Adding more renames

在脚本的 `:process_dir` 标签内添加一行即可 / Add a line inside the `:process_dir` label:

```bat
call :rename_if_needed "%~1\old-name.png" "%~1\new-name.png"
call :ensure_pxtab "%~1" "source" "target" "78"
```

## 注意 / Notes

- 缩放使用 System.Drawing，高度保持不变（23 像素），只调整宽度到 78。
- Resizing uses System.Drawing and keeps the height (23 px), changing only the width to 78.
- 需要 Windows PowerShell（系统自带）。若被执行策略限制，脚本会回退到复制。
- Requires Windows PowerShell (included with Windows). If execution policy blocks it, the script falls back to copying.
- 修改 `zh` 版本时请保持 GBK（936）编码与 CRLF 换行，否则中文会乱码或脚本执行异常。
- When editing the `zh` version, keep GBK (936) encoding and CRLF line endings, otherwise Chinese text or the script itself may break.
