# 从旧版本迁移

> [!NOTE]
> CnCNet 提供了一个名为 `Migration Tool` 的程序，可自动执行下文说明中的所有迁移流程。
> 使用方法：下载最新的 nightly 构建，并以旧客户端路径作为第一个参数运行 `Resources\BinariesNET8\MigrationTool\MigrationTool.exe`。
>
> 示例：`MigrationTool.exe C:\TiberianSunClient`
>
> 建议在运行迁移工具前备份客户端配置。

本文档列出了所有破坏性变更及其处理方式。每个小节对应升级到指定版本所需的迁移步骤。如果你在升级过程中跳过了多个版本，则必须应用所有相应的迁移步骤。

> [!NOTE]
> 更新时始终应删除 `Binaries` 与 `BinariesNET8` 文件夹。有关在模组/游戏包中更新客户端二进制文件的分步流程，请参阅[如何更新到最新客户端版本](HowToUpdate.md)指南。

## 3.0.0

- 客户端的字体渲染已更新，必须迁移你的 `Fonts.ini`：
  - 不再支持逐字体的 `Fallback=N` 键。请将回退字体文件移动到新的 `[FallbackFonts]` 段：把 `Count` 设为回退文件数量，并以 `Fallback0`、`Fallback1` 等列出。路径相对于 `Fonts.ini` 所在目录，与 `Path` 相同。回退列表会应用于所有 TrueType 字体索引。请从 `[FontN]` 段中删除旧的 `Fallback` 键。详见 [Fonts.md](Fonts.md)。
  - `[Fonts]` 段中的 `Count` 键现已忽略，应予以删除。字体索引从 `[Font0]` 开始读取，直到第一个缺失的 `[FontN]` 段为止。
  - 如果你的模组在主题或翻译目录中也附带 `Fonts.ini` 副本，请按上述步骤迁移每个副本。
- 客户端现在默认使用 Dynamic（V3）隧道模式。若需要旧的 V2 行为，请在 `Resources/UserDefaults.ini` 的 `[MultiPlayer]` 中设置 `TunnelMode=2`。
- 将 [negotiating.png](/DXMainClient/Resources/DTA/negotiating.png) 与 [negotiationFailed.png](/DXMainClient/Resources/DTA/negotiationFailed.png) 复制到客户端的 `Resources` 文件夹。
- `OptionsWindow` 新增了 "Storage" 标签页。为了在现有 `576` 像素宽的窗口中容纳七个标签，标签改用 78 像素宽的贴图。请将 [78pxtab.png](/DXMainClient/Resources/DTA/Default%20Theme/78pxtab.png) 与 [78pxtab_c.png](/DXMainClient/Resources/DTA/Default%20Theme/78pxtab_c.png) 复制到客户端的 `Resources` 文件夹，或自行提供 `78pxtab.png`/`78pxtab_c.png`（或 `78pxbtn.png`/`78pxbtn_c.png`）。
- 由于常规 UI 库更新，元素对齐可能略有不同。升级后请检查标签、按钮、列表框与下拉框是否出现裁剪或错位。

## 2.13.0

- `GameLobbyBase` 中的 `PlayerExtraOptionsPanel` 控件已从 `XNAWindow` 改为 `XNAPanel`。`PlayerExtraOptionsPanel.ini` 不再被解析为控件属性，因此该文件中的所有内容应追加到 `GameLobbyBase.ini`。此外，控件 `chkBoxForceRandomTeams` 已更名为 `chkBoxForceNoTeams`，请将 `[chkBoxForceRandomTeams]` 段重命名为 `[chkBoxForceNoTeams]`。
- `Ude.NetStandard.dll` 文件已移动到新路径。请遵循上文中“删除 `Binaries` 与 `BinariesNET8`”的说明。
- `steam_api64.dll` 文件已移动到新路径。请遵循上文中“删除 `Binaries` 与 `BinariesNET8`”的说明。

## 2.12.12

- `DTAConfig` 库已移除，其功能合并到客户端的其他部分。因此，如果使用自动更新器，必须在 `updateexec` 文件的 `[Delete]` 段追加以下行，以避免更新过程中出现问题：

  `updateexec` 中：
  ```ini
  [Delete]
  ; append those lines in the section
  Resources\Binaries\Windows\DTAConfig.dll
  Resources\Binaries\Windows\DTAConfig.pdb
  Resources\Binaries\OpenGL\DTAConfig.dll
  Resources\Binaries\OpenGL\DTAConfig.pdb
  Resources\Binaries\XNA\DTAConfig.dll
  Resources\Binaries\XNA\DTAConfig.pdb
  Resources\BinariesNET8\Windows\DTAConfig.dll
  Resources\BinariesNET8\Windows\DTAConfig.pdb
  Resources\BinariesNET8\OpenGL\DTAConfig.dll
  Resources\BinariesNET8\OpenGL\DTAConfig.pdb
  Resources\BinariesNET8\UniversalGL\DTAConfig.dll
  Resources\BinariesNET8\UniversalGL\DTAConfig.pdb
  Resources\BinariesNET8\XNA\DTAConfig.dll
  Resources\BinariesNET8\XNA\DTAConfig.pdb
  ```

## 2.12.10

- `CoopBriefingBox` 的 `FontIndex` 属性已从 3 改为 0，消除了除字体 0 和 1 之外的所有硬编码字体用法。通常可以忽略此变更；但如果你确实想为地图简报使用不同字体，请查阅 [INISystem.md](INISystem.md) 中 `CoopBriefingBox` 的文档。

## 2.12.6

- 颜色下拉框现在默认同时显示文本与颜色。要恢复仅文本的行为，请在 `GameLobbyBase.ini` 的 `[ddPlayerColor0]` 到 `[ddPlayerColor7]` 段中设置 `ItemsDrawMode=Text`。

- 建议在升级到此版本后，移除 `[GameCreationWindow]` 与 `[GameCreationWindow_Advanced]` 的 `Size` 属性（可能定义在 `GenericWindow.ini` 或 `GameCreationWindow.ini` 中）。

## 2.12.0

- 客户端现已统一不同游戏类型的构建。游戏类型必须在 `ClientDefinitions.ini` 中定义。请在 `ClientDefinitions.ini` 的 `[Settings]` 段中指定 `ClientGameType`，例如 `ClientGameType=Ares`。可用值列表见[此文件](https://github.com/CnCNet/xna-cncnet-client/blob/0554d7974cb741170c881116568144265e6cbabb/ClientCore/Enums/ClientType.cs)。

- `GameOptionsPanel` 中的 `trbScrollRate` 组件从最初起在 INI 配置文件中被误命名为 `trbClientVolume`。此问题已修复。除非你的配置中显式修改过该组件（很少见），否则无需操作。

## 2.11.0.0 及更早版本

- `CustomSettingFileCheckBox` 与 `CustomSettingFileDropDown` 已更名为 `FileSettingCheckBox` 与 `FileSettingDropDown`。这需要在 `OptionsWindow.ini` 中调整控件名称。如果控件通过 `FileX` 定义了文件，`FileSettingCheckBox` 会回退到旧行为。

- 更新器不再硬编码下载镜像或自定义组件列表。该信息现在必须在 `UpdaterConfig.ini` 中设置（示例包含在客户端仓库的默认资源中）。作为参考，原先硬编码的信息可在此处以 `UpdaterConfig.ini` 使用的格式找到[链接](https://gist.github.com/Starkku/1d52f0040d7a00d79e57afc2fba5f97b)。

- 第二阶段更新器不再硬编码重启客户端时要检查的启动器可执行文件列表。它现在只从 `ClientDefinitions.ini` 读取 `LauncherExe` 键；如果读取或启动失败，客户端在更新后不会自动重启。

- 更新器 DLL 文件名已从 `DTAUpdater.dll` 改为 `ClientUpdater.dll`；第二阶段更新器从 `clientupdt.dat` 改为 .NET 4.8 的 `SecondStageUpdater.exe`，并从基础目录移动到 `Resources/Binaries/Updater`。

    - **注意：** 如果希望最终用户能通过旧客户端进行更新，必须在客户端基础目录中保留旧第二阶段更新器（`clientupdt.dat`）的副本。换言之，*不要*通过任一[更新服务器脚本](Updater.md)修改或删除 `clientupdt.dat`。

- 构建脚本现在会自动把第二阶段更新器复制到 `Resources/Binaries/Updater` 文件夹。

- 为支持在 Linux 上启动游戏，必须正确设置 `ClientDefinitions.ini` 中的 `UnixGameExecutableName`（默认 `wine-dta.sh`）。例如，用 wine 启动游戏时，该文件可包含 `wine gamemd-spawn.exe $*`，其中 `gamemd-spawn.exe` 替换为游戏可执行文件。注意用户可能需要执行一次 `chmod +x wine-dta.sh` 才能启动它。

- 跨平台的 `UniversalGL` 构建不支持 `*.cur` 鼠标光标文件。为确保显示预期光标而非缺失贴图（粉色方块），所有主题都需要包含 `cursor.png` 文件。现有的 `*.cur` 文件仍会被仅限 Windows 的构建使用。

- 为 MonoGame WindowsDX 发布时，MonoGame MCGB 编辑器会把 MainMenuTheme 转换为 `MainMenuTheme.wma`。MonoGame DesktopGL 只支持 `*.ogg` 格式。为确保 MainMenuTheme 在 WindowsDX 与 DesktopGL 客户端版本中都可用，需要手动转换并添加缺失的 ogg 文件到每个主题。之后每个主题应同时包含 `MainMenuTheme.wma` 与 `MainMenuTheme.ogg`。客户端会在运行时切换到正确的格式。

- XNAUI 的更新[修复了一个 bug](https://github.com/Rampastring/Rampastring.XNAUI/commit/6857704734241895f9cbb2c79fbd0286c350c313)，该 bug 可能导致边框无法绘制。然而，你的模组可能依赖此 bug，因此升级后窗口中会出现不需要的边框。此时请为窗口手动指定 `DrawBorders=false`。例如，在 `GenericWindow.ini` 中添加以下行，以关闭*某些*窗口（如消息框）的边框。你仍需根据需要在 ini 文件中为更多窗口指定该属性。

  ```ini
  [GenericWindow]
  DrawBorders=false
  ```

- [Tiberian Sun Client v6 变更](https://github.com/CnCNet/xna-cncnet-client/pull/275) 破坏了兼容性。你需要使用新的 `INItializableWindow` 格式重新实现 `SkirmishLobby`、`LANLobby` 与 `CnCNetLobby` 的 ini 文件。另外，如果你依赖 `[ExtraControls]`，请在 `GenericWindow.ini` 中添加 `[$ExtraControls]` 段。在 `DTACnCNetClient.ini` 文件的 `[ParserConstants]` 段中定义常量，供 `INItializableWindow` 配置使用。详见[本指南](Migration-INI.md)。

- 新的[玩家状态指示器功能](https://github.com/CnCNet/xna-cncnet-client/pull/251)取代了游戏大厅中旧的“玩家已准备”指示器。这需要：
  - 将 `PlayerReadyBox*` 标签重命名为 `PlayerStatusIndicator*`（其 `X` 与 `Y` 的默认值现为 `0` 和 `0`，而非 `7` 和 `4`）；
  - 提供以下新贴图（放在 `Resources` 文件夹和/或主题子文件夹中，示例见[此处](https://github.com/CnCNet/cncnet-yr-client-package/pull/61)）：
    - `statusEmpty.png`；
    - `statusUnavailable.png`；
    - `statusAI.png`；
    - `statusClear.png`；
    - `statusOk.png`；
    - `statusInProgress.png`；
    - `statusWarning.png`；
    - `statusError.png`；

- [Tiberian Sun Client v6 变更](https://github.com/CnCNet/xna-cncnet-client/pull/275) 将许可证改为 GPLv3。这意味着如果你的客户端是私有分支，你必须停止发布修改后的客户端，或以 GPLv3 许可证公开修改后的源代码。

- 游戏大厅中的 `BtnSaveLoadGameOptions` 已为保持一致性重命名为 `btnSaveLoadGameOptions`。处理示例见 TSC 的[此变更](https://github.com/CnCNet/cncnet-ts-client-package/commit/2ac97c68978431e94e320299e0168119f75a849f)。
