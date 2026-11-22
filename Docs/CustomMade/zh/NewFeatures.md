# 新功能

本文档介绍可选的、非破坏性变更。虽然并非强制，但采纳这些更新可解锁新的客户端功能。

破坏性变更不在此处介绍；请参阅 [Migration.md](Migration.md)。

## 3.0.0

- 客户端现支持 V3 动态隧道。默认的房主隧道模式为 Dynamic（V3），游戏创建窗口与选项窗口中新增的“隧道模式”选择器提供 3 个选项：Dynamic（V3）、Static（V3）与 Legacy（V2）。设置细节见 [INISystem.md](INISystem.md) 中的 `[MultiPlayer]` 隧道设置（`TunnelMode` / `EnableP2P`）。

- 选项窗口的 CnCNet 标签页新增“Allow P2P connections（允许 P2P 连接）”复选框。启用后，客户端会尝试与其他玩家建立直接 P2P 连接。这可以降低延迟并减少服务器负载，但并非在所有网络环境下都能工作。

- 客户端现支持 Tiberian Dawn 与 Dune 2000 游戏类型。在 `ClientDefinitions.ini` 中指定 `[Settings]->ClientGameType=TD` 或 `[Settings]->ClientGameType=D2K` 即可使用相应游戏类型。

- 对于搭配了兼容 spawner 的游戏包，现可使用回放录制与播放。在 `ClientDefinitions.ini` 中设置 `ReplaySupport=true` 以启用。玩家可在选项窗口的 Game 标签页中开启或关闭录制。回放设置见 [INISystem.md](INISystem.md)。

- 使用 WinForms 的 Windows 构建支持可选的 `Resources/ingameicon.ico`。存在该文件时，客户端在启动的游戏运行期间将其作为窗口图标，并在游戏退出后恢复 `Resources/clienticon.ico`。如果可选图标缺失或无法加载，客户端会保留当前图标。

## 2.13.4

- `KeyboardCommands.ini` 现支持 `DisableModifierKeys`。建议为 RA2/YR 的 `PlanningMode`（路径点模式）命令设置该键。见 [INISystem.md](INISystem.md)。

## 2.13.0

- 客户端现支持使用 TTF/OTF 字体文件。详见 [Fonts.md](Fonts.md)。

- 自定义任务支持允许玩家自制战役地图与官方任务一同显示。功能包括补充文件管理（字符串表、调色板、加载画面）、任务预览图与存档集成。参见 [INISystem.md](INISystem.md) 中的 `Custom Mission Map Files` 章节。

- 提供可选的战役标签选择器，允许玩家在任务选择器之前选择一类战役任务。也可用于区分官方任务与自定义任务。参见 [INISystem.md](INISystem.md) 的 `Campaign Tag Selector` 章节。

- 战役选择器现支持显示任务预览图。参见 [INISystem.md](INISystem.md) 的 `[pnlMissionPreview]` 章节。

- `MPMaps.ini` 中的游戏模式配置已扩展。游戏模式段现支持 `UIName`、`DisallowedPlayerSides`、`DisallowedHumanPlayerSides`、`DisallowedComputerPlayerSides`、`ForcedOptions`、`ForcedSpawnIniOptions`、`MapCodeIniName`、`RandomizedMapCodeIniNames` 以及玩家数量覆盖。参见 [INISystem.md](INISystem.md)。

- `GameOptions.ini` 中的 `[CampaignForcedSpawnIniOptions]` 段可单独为战役任务强制设置 spawn.ini 键（与多人游戏分开）。

- 以下控件现可用于将自定义游戏选项广播到 CnCNet 大厅，并在游戏列表与筛选器中显示：`GameSessionCheckBox`、`GameLobbyCheckBox`、`GameSessionDropDown`、`GameLobbyDropDown`。参见 [INISystem.md](INISystem.md)。

- 可关闭游戏大厅列表中的游戏图标。参见 [INISystem.md](INISystem.md) 中的 `ShowGameIconInGameList`。

- 只要用户的键盘 ini 中缺少相应热键，客户端现在会应用模组作者建议的默认热键。这使模组作者可以通过编辑 `KeyboardCommands.ini` 为其模组提供更定制化的体验。

## 2.12.18

- `DTACnCNetClient.ini` 的 `[General]` 段中的 `MainMenuTheme` 键（可能依赖 `GlobalThemeSettings.ini` 文件）现支持多个背景音乐文件，以逗号分隔。客户端会随机选择一个。

## 2.12.17

- 此版本包含 DirectDraw 兼容性修复器，可帮助用户移除游戏可执行文件中存在问题的兼容性设置。因此建议将游戏可执行文件添加到 `ClientDefinitions.ini`。示例：

```ini
[Settings]
CompatibilityCheckExecutables=CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe ; comma-separated list of strings to check for DirectDraw compatibility mode issues
```

- 新增大厅设置更新窗口，允许房主修改房间名、最大玩家数、技能等级与密码。要启用此功能，请编辑 `CnCNetGameLobby.ini`。首先在现有 `[MultiplayerGameLobby]` 段中添加 `$CCMP100=btnGameLobbySettings:XNAClientButton`（编号可能因你的配置而异）：

```ini
[MultiplayerGameLobby]
$CCMP100=btnGameLobbySettings:XNAClientButton
```

然后添加以下 `[btnGameLobbySettings]` 段：

```ini
[btnGameLobbySettings]
Text=Lobby Settings
Location=0,0
Size=133,23
DistanceFromBottomBorder=13
DistanceFromRightBorder=300
Visible=false
Enabled=false
```

## 2.12.15

- 客户端现支持长路径感知，可处理超过 260 个字符的地图文件路径（下载自定义地图时可能出现）。但长路径感知**也**必须在**玩家机器**上启用。如果你使用 Inno Setup 分发模组，可在 Inno Setup 脚本中包含以下内容：

```iss
[Registry]
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Control\FileSystem; ValueType: dword; ValueName: LongPathsEnabled; ValueData: 1; MinVersion: 10.0.14393
```

或者，可通过提供以下 `.reg` 文件指导玩家启用：

```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
"LongPathsEnabled"=dword:00000001
```

## 2.12.13

- SpriteFont 文件已修订。请下载新的 [SpriteFont0.xnb](/DXMainClient/Resources/DTA/SpriteFont0.xnb) 与 [SpriteFont1.xnb](/DXMainClient/Resources/DTA/SpriteFont1.xnb)，替换 `Resources` 文件夹中的旧文件。客户端不再依赖其余字体文件（SpriteFont2、3、4、5……），除非你在 `.ini` 文件中显式指定为 `FontIndex`；若未使用可删除。

## 2.12.12

- `CampaignSelector` 现支持使用 `CampaignCheckBox` 与 `CampaignDropDown` 组件配置游戏选项与强制 spawn 选项。`ClientDefinitions.ini` 的 `[Settings]` 段中还可使用 `SaveSkirmishGameOptions` 与 `SaveCampaignGameOptions` 键。

## 2.12.10

- `VersionWriter.exe` 已更新，新增设置：`ExcludeHiddenAndSystemFiles`、`ApplyTimestampOnVersion`、`NoCopyMode` 以及 `[ExcludeDirectories]` 段。见 [Updater.md](Updater.md)。

## 2.12.8

- 你可以在 `Resources` 文件夹中创建 `UserDefaults.ini` 文件，以覆盖选项窗口中的默认设置。示例：

```ini
[Video]
IntegerScaledClient=True
BorderlessWindowedClient=False

[Audio]
ClientVolume=0.3
PlayMainMenuMusic=False

[MultiPlayer]
NotifyOnUserListChange=False
```

建议将 `IntegerScaledClient=True` 作为默认值。

- 对于 `XNAClientColorDropDown` 组件，可使用 `DisabledItemTexture` 键。

## 2.12.7

- `GameOptions.ini` 的 `[RandomSelectors]` 段中定义的随机选择器现接受重复值，可用于调整每个阵营的随机权重。

## 2.12.6

- 可在 `Translation.ini` 文件中指定 `MapEncoding` 键。但除非你完全理解其作用，否则**不应指定它**。例如，中文翻译**不应**选择 GB2312/GBK/GB18030/BIG5。此功能主要面向 Tiberian Sun，绝不应在 Red Alert 2 中使用。

- `XNAClientColorDropDown` 组件现在有三种绘制模式。参见 [INISystem.md](INISystem.md) 中的 `XNAClientColorDropDown`。

## 2.12.5

- 新增非活跃房主检测功能。要启用，请在 `ClientDefinitions.ini` 的 `[Settings]` 段中为正整数指定 `InactiveHostWarningMessageSeconds` 与 `InactiveHostKickSeconds`。

## 2.12.4

- 客户端现在会在打开来自聊天消息的未知 HTTP/HTTPS 链接前显示警告。可通过 `ClientDefinitions.ini` 的 `[Settings]` 段中的 `TrustedDomains` 键覆盖默认可信域名列表。参见 [INISystem.md](INISystem.md)。

## 2.12.2

- 客户端现支持从多张图片中随机选择一张加载画面。参见 [INISystem.md](INISystem.md) 中的 `LoadingScreen` 章节。

## 2.11.7.0

- 此前只能通过合作地图设置、游戏模式设置或游戏选项复选框限制阵营选择。此版本允许通过游戏模式段中的 `DisallowedHumanPlayerSides` 与 `DisallowedComputerPlayerSides` 分别禁用人类玩家或 AI 玩家的特定阵营。`INI\MPMaps.ini` 示例：

```ini
[Standard]                          ; any game mode section
; (...)
DisallowedPlayerSides=7             ; already exists - disallows sides for all players
DisallowedHumanPlayerSides=1,2,3    ; new - disallows sides for human players only
DisallowedComputerPlayerSides=4,5,6 ; new - disallows sides for computer players only
```

- 默认 CnCNet 服务 URL 已升级为 HTTPS。如果你在 `ClientDefinitions.ini` 或 `NetworkDefinitions.ini` 中使用非 HTTPS URL（尤其是以 `cncnet.org` 或 `moddb.com` 结尾的域名），请更新为 HTTPS。

## 2.11.2.0

- 在 2.11.0.0 与 2.11.1.0 版本中，客户端二进制文件附带了 `ClientUpdater.xml` 与 `SecondStageUpdater.xml` 文件。这些文件并非必需，可安全删除。

## 2.11.1.0

- 非全屏模式下，客户端现在会从推荐列表中提供多个整数缩放分辨率。建议模组作者更新 `ClientDefinitions.ini` 中的 `RecommendedResolutions` 设置，使列出的分辨率不小于 `{MinimumRenderWidth}x{MinimumRenderHeight}` 且不大于 `{MaximumRenderWidth}x{MaximumRenderHeight}`。

- 文档已更新，建议模组作者即使面向最终用户分发，也保留与 `*.exe`、`*.dll` 文件对应的 `*.pdb` 文件（例如 `clientdx.pdb`、`ClientCore.pdb` 等）。保留这些文件可提供更详细的错误日志，极大帮助排查问题。

- 文档已更新，建议中文翻译者使用 `zh-Hans` 或 `zh-Hant` 作为翻译文件夹名称。

## 2.11.0.0

- 已实现本地化系统。见 [Translation.md](Translation.md)。

- 客户端的 OpenGL 版本现在可以加载与对应 `.wma` 文件放在一起的 `.ogg` 背景音乐文件。

- 多项网络相关定义现在可通过 `Resources` 文件夹中的 `NetworkDefinitions.ini` 文件自定义。示例如下。

```ini
[Settings]
CnCNetTunnelListURL=https://cncnet.org/master-list
CnCNetPlayerCountURL=https://api.cncnet.org/status
CnCNetMapDBDownloadURL=https://mapdb.cncnet.org
CnCNetMapDBUploadURL=https://mapdb.cncnet.org/upload
DisableDiscordIntegration=False

; https://gamesurge.net/servers
[IRCServers]
1=irc.gamesurge.net|GameSurge|6667
2=LAN-Team.DE.EU.GameSurge.net|GameSurge Nuremberg, Germany|6660,6666,6667,6668,6669
3=Stockholm.SE.EU.GameSurge.net|GameSurge Stockholm, Sweden|6666,6669,7000,8080
4=NuclearFallout.WA.US.GameSurge.net|GameSurge Seattle, WA|6667,5960
5=Prothid.NY.US.GameSurge.Net|GameSurge NYC, NY|5960,6660,6666,6667,6668,6669,6697
6=192.223.27.109|GameSurge IP 192.223.27.109|5960,6660,6666,6667,6668,6669
7=162.248.94.123|GameSurge IP 162.248.94.123|6667,5960
8=128.140.107.226|GameSurge IP 128.140.107.226|6660,6666,6667,6668,6669
9=188.240.145.60|GameSurge IP 188.240.145.60|6660,6666,6667,6668,6669
```
