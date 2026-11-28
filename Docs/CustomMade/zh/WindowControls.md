# 窗口与面板控件参考

本手册按窗口/面板列出源码中定义的控件，用于查找控件用途、重写 INI 布局。

## 阅读说明

- 控件名即 INI 段名，可直接在对应窗口 INI 中查找或新增。
- “类型”是源码使用的控件类；“创建方式”见表格末列：`代码` 表示源码 `new` 创建，`源码读取` 表示源码通过 `FindChild` 按名称读取 INI 控件。
- 继承自基类的控件不在子类重复列出，请查看基类小节。
- 仅列源码创建或源码通过 `FindChild` 引用的控件；默认主题 INI 里的 `$CC` / 额外控件属于 MOD 自定义内容，不在本清单内。
- INI 文件名按类名/窗口 `Name` 推断，主题或 MOD 可以覆盖。

## 布局重写

重写布局时保留段名，修改以下键即可：

- 位置与尺寸：`Location`、`Size`、`X`、`Y`、`Width`、`Height`
- 表达式布局：`$X`、`$Y`、`$Width`、`$Height`（受键顺序影响）
- 背景与边框：`BackgroundTexture`、`SolidColorBackgroundTexture`、`DrawBorders`
- 代码创建的控件同样通过同名段配置；INI 创建的控件必须保留 `$CC` 或额外控件声明。

## 目录

**通用窗口与面板**

- [ExtrasWindow](#extraswindow)
- [GameInProgressWindow](#gameinprogresswindow)
- [GameLoadingWindow](#gameloadingwindow)
- [HotkeyConfigurationWindow](#hotkeyconfigurationwindow)
- [LoadingScreen](#loadingscreen)
- [MainMenu](#mainmenu)
- [ManualUpdateQueryWindow](#manualupdatequerywindow)
- [OptionsWindow](#optionswindow)
- [PrivacyNotification](#privacynotification)
- [ReplaysPanel](#replayspanel)
- [StatisticsWindow](#statisticswindow)
- [TopBar](#topbar)
- [UpdateQueryWindow](#updatequerywindow)
- [UpdateWindow](#updatewindow)
- [XNAClientScrollPanel](#xnaclientscrollpanel)
- [XNAMessageBox](#xnamessagebox)

**多人游戏与大厅**

- [ChoiceNotificationBox](#choicenotificationbox)
- [CnCNetGameLoadingLobby](#cncnetgameloadinglobby)
- [CnCNetGameLobby](#cncnetgamelobby)
- [CnCNetLobby](#cncnetlobby)
- [CnCNetLoginWindow](#cncnetloginwindow)
- [CoopBriefingBox](#coopbriefingbox)
- [GameCreationWindow](#gamecreationwindow)
- [GameFiltersPanel](#gamefilterspanel)
- [GameFiltersScrollPanel](#gamefiltersscrollpanel)
- [GameInformationIconOnlyPanel](#gameinformationicononlypanel)
- [GameInformationIconPanel](#gameinformationiconpanel)
- [GameInformationPanel](#gameinformationpanel)
- [GameLoadingLobbyBase](#gameloadinglobbybase)
- [GameLobbyBase](#gamelobbybase)
- [LANGameCreationWindow](#langamecreationwindow)
- [LANGameLoadingLobby](#langameloadinglobby)
- [LANGameLobby](#langamelobby)
- [LANLobby](#lanlobby)
- [LoadOrSaveGameOptionPresetWindow](#loadorsavegameoptionpresetwindow)
- [MapPreviewBox](#mappreviewbox)
- [MapSharingConfirmationPanel](#mapsharingconfirmationpanel)
- [MultiplayerGameLobby](#multiplayergamelobby)
- [PasswordRequestWindow](#passwordrequestwindow)
- [PlayerAIQuickOptionsPanel](#playeraiquickoptionspanel)
- [PlayerExtraOptionsPanel](#playerextraoptionspanel)
- [PlayerNameOptionsPanel](#playernameoptionspanel)
- [PlayerSlotsScrollPanel](#playerslotsscrollpanel)
- [PrivateMessageNotificationBox](#privatemessagenotificationbox)
- [PrivateMessagingPanel](#privatemessagingpanel)
- [PrivateMessagingWindow](#privatemessagingwindow)
- [SkirmishLobby](#skirmishlobby)
- [TeamStartMappingPanel](#teamstartmappingpanel)
- [TeamStartMappingsPanel](#teamstartmappingspanel)
- [TunnelNegotiationStatusPanel](#tunnelnegotiationstatuspanel)
- [TunnelSelectionWindow](#tunnelselectionwindow)

**战役**

- [CampaignSelector](#campaignselector)
- [CampaignTagSelector](#campaigntagselector)
- [CheaterWindow](#cheaterwindow)

**选项窗口面板**

- [AudioOptionsPanel](#audiooptionspanel)
- [CnCNetOptionsPanel](#cncnetoptionspanel)
- [ComponentsPanel](#componentspanel)
- [DisplayOptionsPanel](#displayoptionspanel)
- [GameOptionsPanel](#gameoptionspanel)
- [StorageOptionsPanel](#storageoptionspanel)
- [StorageScrollPanel](#storagescrollpanel)
- [UpdaterOptionsPanel](#updateroptionspanel)

## 通用窗口与面板

### ExtrasWindow

附加内容窗口。

- 源码：`DXMainClient\DXGUI\Generic\ExtrasWindow.cs`　基类：`XNAWindow`　INI：`ExtrasWindow.ini`　控件：4 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnExCancel` | 返回 | `XNAClientButton` | 代码 |
| `btnExCredits` | 制作者名单 | `XNAClientButton` | 代码 |
| `btnExMapEditor` | 地图编辑器 | `XNAClientButton` | 代码 |
| `btnExStatistics` | 统计 | `XNAClientButton` | 代码 |

### GameInProgressWindow

- 源码：`DXMainClient\DXGUI\Generic\GameInProgressWindow.cs`　基类：`XNAPanel`　INI：`GameInProgressWindow.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### GameLoadingWindow

读取存档与回放窗口。

- 源码：`DXMainClient\DXGUI\Generic\GameLoadingWindow.cs`　基类：`XNAWindow`　INI：`GameLoadingWindow.ini`　控件：5 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnDelete` | 删除按钮 | `XNAClientButton` | 代码 |
| `btnLaunch` | 启动游戏/任务 | `XNAClientButton` | 代码 |
| `lbSaveGameList` | 保存房间列表 | `XNAMultiColumnListBox` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |

### HotkeyConfigurationWindow

热键配置窗口。

- 源码：`ClientGUI\HotkeyConfigurationWindow.cs`　基类：`XNAWindow`　INI：`HotkeyConfigurationWindow.ini`　控件：18 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnAssign` | 分配热键 | `XNAClientButton` | 代码 |
| `btnExit` | 退出 | `XNAClientButton` | 代码 |
| `btnResetAllToDefaults` | 重置全部热键 | `XNAClientButton` | 代码 |
| `btnResetKey` | 重置当前热键 | `XNAClientButton` | 代码 |
| `btnSave` | 保存按钮 | `XNAClientButton` | 代码 |
| `ddCategory` | 分类下拉框 | `XNAClientDropDown` | 代码 |
| `HotkeyInfoPanel` | 热键信息面板 | `XNAPanel` | 代码 |
| `lbHotkeys` | 热键列表框 | `XNAMultiColumnListBox` | 代码 |
| `lblCategory` | 分类标签 | `XNALabel` | 代码 |
| `lblCommandCaption` | 命令标签 | `XNALabel` | 代码 |
| `lblCurrentHotkey` | 当前热键标签 | `XNALabel` | 代码 |
| `lblCurrentHotkeyValue` | 当前热键值标签 | `XNALabel` | 代码 |
| `lblCurrentlyAssignedTo` | 当前已分配标签 | `XNALabel` | 代码 |
| `lblDefaultHotkeyValue` | 默认热键值标签 | `XNALabel` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblNewHotkey` | 新建热键标签 | `XNALabel` | 代码 |
| `lblNewHotkeyValue` | 新建热键值标签 | `XNALabel` | 代码 |
| `lblOriginalHotkey` | 原始热键标签 | `XNALabel` | 代码 |

### LoadingScreen

启动加载画面窗口。

- 源码：`DXMainClient\DXGUI\Generic\LoadingScreen.cs`　基类：`XNAWindow`　INI：`LoadingScreen.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### MainMenu

主菜单窗口，负责战役、读档、遭遇战、CnCNet、局域网等入口。

- 源码：`DXMainClient\DXGUI\Generic\MainMenu.cs`　基类：`XNAWindow, ISwitchable`　INI：`MainMenu.ini`　控件：15 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCnCNet` | 在线游戏 | `XNAClientButton` | 代码 |
| `btnCredits` | 制作者名单 | `XNAClientButton` | 代码 |
| `btnExit` | 退出 | `XNAClientButton` | 代码 |
| `btnExtras` | 附加内容 | `XNAClientButton` | 代码 |
| `btnLan` | 局域网 | `XNAClientButton` | 代码 |
| `btnLoadGame` | 载入存档 | `XNAClientButton` | 代码 |
| `btnMapEditor` | 地图编辑器 | `XNAClientButton` | 代码 |
| `btnNewCampaign` | 战役模式 | `XNAClientButton` | 代码 |
| `btnOptions` | 设置 | `XNAClientButton` | 代码 |
| `btnSkirmish` | 遭遇战 | `XNAClientButton` | 代码 |
| `btnStatistics` | 统计 | `XNAClientButton` | 代码 |
| `lblCnCNetPlayerCount` | CnCNet 在线人数 | `XNALabel` | 代码 |
| `lblCnCNetStatus` | CnCNet 状态文字 | `XNALabel` | 代码 |
| `lblUpdateStatus` | 更新状态 | `XNALinkLabel` | 代码 |
| `lblVersion` | 客户端版本号 | `XNALinkLabel` | 代码 |

### ManualUpdateQueryWindow

手动更新询问窗口。

- 源码：`DXMainClient\DXGUI\Generic\ManualUpdateQueryWindow.cs`　基类：`XNAWindow`　INI：`ManualUpdateQueryWindow.ini`　控件：3 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `btnDownload` | 下载按钮 | `XNAClientButton` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |

### OptionsWindow

选项窗口，包含显示、音频、游戏、CnCNet、存储、更新和组件标签页。

- 源码：`DXMainClient\DXGUI\Generic\OptionsWindow.cs`　基类：`XNAWindow`　INI：`OptionsWindow.ini`　控件：3 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnSave` | 保存按钮 | `XNAClientButton` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |

### PrivacyNotification

- 源码：`DXMainClient\DXGUI\Generic\PrivacyNotification.cs`　基类：`XNAWindow`　INI：`PrivacyNotification.ini`　控件：6 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnOK` | 确定按钮 | `XNAClientButton` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblExplanation` | 文本标签 | `XNALabel` | 代码 |
| `lblMoreInformation` | 信息标签 | `XNALabel` | 代码 |
| `lblPrivacyPolicy` | 文本标签 | `XNALinkLabel` | 代码 |
| `lblTermsAndConditions` | 文本标签 | `XNALinkLabel` | 代码 |

### ReplaysPanel

- 源码：`DXMainClient\DXGUI\Generic\LoadGamePanels\ReplaysPanel.cs`　基类：`XNAPanel`　INI：`ReplaysPanel.ini`　控件：11 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `ddGameSpeed` | 回放播放速度 | `XNAClientDropDown` | 代码 |
| `ddSeekCheckpoints` | 回放跳转关键帧 | `XNAClientDropDown` | 代码 |
| `ddWatchAs` | 观战视角 | `XNAClientDropDown` | 代码 |
| `lblGameSpeed` | 游戏速度标签 | `XNALabel` | 代码 |
| `lblPlayback` | 文本标签 | `XNALabel` | 代码 |
| `lblReleases` | 文本标签 | `XNALinkLabel` | 代码 |
| `lblSeekCheckpoints` | 跳转关键帧标签 | `XNALabel` | 代码 |
| `lblWatchAs` | 观战视角标签 | `XNALabel` | 代码 |
| `lbReplayList` | 回放列表 | `XNAMultiColumnListBox` | 代码 |
| `replayContextMenu` | 回放右键菜单 | `XNAContextMenu` | 代码 |
| `tbDetails` | 详情文本 | `XNATextBlock` | 代码 |

### StatisticsWindow

对局统计窗口。

- 源码：`DXMainClient\DXGUI\Generic\StatisticsWindow.cs`　基类：`XNAWindow`　INI：`StatisticsWindow.ini`　控件：31 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClearStatistics` | 统计按钮 | `XNAClientButton` | 代码 |
| `btnReturnToMenu` | 返回主菜单 | `XNAClientButton` | 代码 |
| `chkIncludeSpectatedGames` | 包含观战对局 | `XNAClientCheckBox` | 代码 |
| `cmbGameClassFilter` | 筛选下拉框 | `XNAClientDropDown` | 代码 |
| `cmbGameModeFilter` | 游戏模式筛选下拉框 | `XNAClientDropDown` | 代码 |
| `lbGameList` | 房间列表 | `XNAMultiColumnListBox` | 代码 |
| `lbGameStatistics` | 统计列表框 | `XNAMultiColumnListBox` | 代码 |
| `lblAverageAILevelValue` | 值标签 | `XNALabel` | 代码 |
| `lblAverageAllyCountValue` | 全部数量值标签 | `XNALabel` | 代码 |
| `lblAverageEconomyValue` | 值标签 | `XNALabel` | 代码 |
| `lblAverageEnemyCountValue` | 数量值标签 | `XNALabel` | 代码 |
| `lblAverageGameLengthValue` | 值标签 | `XNALabel` | 代码 |
| `lblFavouriteSideValue` | 阵营值标签 | `XNALabel` | 代码 |
| `lblFilter` | 筛选标签 | `XNALabel` | 代码 |
| `lblGameMode` | 游戏模式 | `XNALabel` | 代码 |
| `lblGames` | 文本标签 | `XNALabel` | 代码 |
| `lblGamesFinishedValue` | 值标签 | `XNALabel` | 代码 |
| `lblGamesStartedValue` | 起始位置值标签 | `XNALabel` | 代码 |
| `lblKillLossRatioValue` | 值标签 | `XNALabel` | 代码 |
| `lblKillsPerGameValue` | 值标签 | `XNALabel` | 代码 |
| `lblLossesPerGameValue` | 值标签 | `XNALabel` | 代码 |
| `lblLossesValue` | 值标签 | `XNALabel` | 代码 |
| `lblTotalKillsValue` | 值标签 | `XNALabel` | 代码 |
| `lblTotalLossesValue` | 值标签 | `XNALabel` | 代码 |
| `lblTotalScoreValue` | 得分值标签 | `XNALabel` | 代码 |
| `lblTotalTimePlayedValue` | 时间值标签 | `XNALabel` | 代码 |
| `lblWinLossRatioValue` | 值标签 | `XNALabel` | 代码 |
| `lblWinsValue` | 值标签 | `XNALabel` | 代码 |
| `panelGameStatistics` | 统计面板 | `XNAPanel` | 代码 |
| `panelTotalStatistics` | 统计面板 | `XNAPanel` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |

### TopBar

- 源码：`DXMainClient\DXGUI\Generic\TopBar.cs`　基类：`XNAPanel`　INI：`TopBar.ini`　控件：10 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCnCNetLobby` | 在线按钮 | `XNAClientButton` | 代码 |
| `btnLogout` | 注销 | `XNAClientButton` | 代码 |
| `btnMainButton` | 主菜单按钮 | `XNAClientButton` | 代码 |
| `btnOptions` | 设置 | `XNAClientButton` | 代码 |
| `btnPrivateMessages` | 打开私信 | `XNAClientButton` | 代码 |
| `lblCnCNetPlayerCount` | CnCNet 在线人数 | `XNALabel` | 代码 |
| `lblCnCNetStatus` | CnCNet 状态文字 | `XNALabel` | 代码 |
| `lblConnectionStatus` | 连接状态标签 | `XNALabel` | 代码 |
| `lblDate` | 日期标签 | `XNALabel` | 代码 |
| `lblTime` | 时间标签 | `XNALabel` | 代码 |

### UpdateQueryWindow

更新询问窗口。

- 源码：`DXMainClient\DXGUI\Generic\UpdateQueryWindow.cs`　基类：`XNAWindow`　INI：`UpdateQueryWindow.ini`　控件：5 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnNo` | 否按钮 | `XNAClientButton` | 代码 |
| `btnYes` | 是按钮 | `XNAClientButton` | 代码 |
| `lblChangelogLink` | 文本标签 | `XNALinkLabel` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblUpdateSize` | 更新尺寸标签 | `XNALabel` | 代码 |

### UpdateWindow

更新执行窗口。

- 源码：`DXMainClient\DXGUI\Generic\UpdateWindow.cs`　基类：`XNAWindow`　INI：`UpdateWindow.ini`　控件：9 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `lblCurrentFile` | 当前标签 | `XNALabel` | 代码 |
| `lblCurrentFileProgressPercentage` | 当前标签 | `XNALabel` | 代码 |
| `lblCurrentFileProgressPercentageValue` | 当前值标签 | `XNALabel` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblTotalProgressPercentage` | 文本标签 | `XNALabel` | 代码 |
| `lblTotalProgressPercentageValue` | 值标签 | `XNALabel` | 代码 |
| `lblUpdaterStatus` | 更新状态标签 | `XNALabel` | 代码 |
| `prgCurrentFile` | 当前进度条 | `XNAProgressBar` | 代码 |
| `prgTotal` | 总进度条 | `XNAProgressBar` | 代码 |

### XNAClientScrollPanel

- 源码：`ClientGUI\XNAClientScrollPanel.cs`　基类：`XNAScrollPanel, ICompositeControl`　INI：`XNAClientScrollPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### XNAMessageBox

通用消息框。

- 源码：`ClientGUI\XNAMessageBox.cs`　基类：`XNAWindow`　INI：`XNAMessageBox.ini`　控件：4 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAButton` | 代码 |
| `btnNo` | 否按钮 | `XNAButton` | 代码 |
| `btnOK` | 确定按钮 | `XNAButton` | 代码 |
| `btnYes` | 是按钮 | `XNAButton` | 代码 |

## 多人游戏与大厅

### ChoiceNotificationBox

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\ChoiceNotificationBox.cs`　基类：`XNAPanel`　INI：`ChoiceNotificationBox.ini`　控件：6 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `affirmativeButton` | 确认按钮 | `XNAClientButton` | 代码 |
| `gameIconPanel` | 游戏图标面板 | `XNAPanel` | 代码 |
| `lblChoiceText` | 文本标签 | `XNALabel` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblSender` | 文本标签 | `XNALabel` | 代码 |
| `negativeButton` | 取消按钮 | `XNAClientButton` | 代码 |

### CnCNetGameLoadingLobby

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetGameLoadingLobby.cs`　基类：`GameLoadingLobbyBase, IV3NegotiationHost`　INI：`CnCNetGameLoadingLobby.ini`　控件：1 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnChangeTunnel` | 更换隧道 | `XNAClientButton` | 代码 |

### CnCNetGameLobby

CnCNet 大厅的隧道与房间设置入口。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\CnCNetGameLobby.cs`　基类：`MultiplayerGameLobby, IV3NegotiationHost`　INI：`CnCNetGameLobby.ini`　控件：4 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `_negotiationStatusPanel` | 协商状态面板 | `TunnelNegotiationStatusPanel` | 代码 |
| `btnChangeTunnel` | 更换隧道 | `XNAClientButton` | 源码读取 |
| `btnGameLobbySettings` | 房间设置 | `XNAClientButton` | 源码读取 |
| `btnNegotiationStatus` | 连接协商状态 | `XNAClientButton` | 源码读取 |

### CnCNetLobby

CnCNet 房间列表大厅。

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetLobby.cs`　基类：`XNAWindow, ISwitchable`　INI：`CnCNetLobby.ini`　控件：17 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnGameFilterOptions` | 筛选设置按钮 | `XNAClientToggleButton` | 代码 |
| `btnGameSortAlpha` | 游戏列表按字母排序 | `XNAClientStateButton<SortDirection>` | 代码 |
| `btnJoinGame` | 加入房间 | `XNAClientButton` | 代码 |
| `btnLogout` | 注销 | `XNAClientButton` | 代码 |
| `btnNewGame` | 创建房间 | `XNAClientButton` | 代码 |
| `ddColor` | 聊天颜色选择 | `XNAClientDropDown` | 代码 |
| `ddCurrentChannel` | 频道选择 | `XNAClientDropDown` | 代码 |
| `lbChatMessages` | 聊天记录 | `ChatListBox` | 代码 |
| `lbGameList` | 房间列表 | `GameListBox` | 代码 |
| `lblColor` | “您的颜色”标题 | `XNALabel` | 代码 |
| `lblCurrentChannel` | 当前频道标题 | `XNALabel` | 代码 |
| `lblOnline` | 在线标签 | `XNALabel` | 代码 |
| `lblOnlineCount` | 在线数量标签 | `XNALabel` | 代码 |
| `lbPlayerList` | 玩家列表 | `PlayerListBox` | 代码 |
| `panelGameFilters` | 筛选面板 | `GameFiltersPanel` | 代码 |
| `tbChatInput` | 聊天输入 | `XNAChatTextBox` | 代码 |
| `tbGameSearch` | 游戏搜索 | `XNASuggestionTextBox` | 代码 |

### CnCNetLoginWindow

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetLoginWindow.cs`　基类：`XNAWindow`　INI：`CnCNetLoginWindow.ini`　控件：8 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnConnect` | 连接按钮 | `XNAClientButton` | 代码 |
| `chkAutoConnect` | 连接复选框 | `XNAClientCheckBox` | 代码 |
| `chkPersistentMode` | 持久模式复选框 | `XNAClientCheckBox` | 代码 |
| `chkRememberMe` | 记住我 | `XNAClientCheckBox` | 代码 |
| `lblConnectToCnCNet` | 连接在线标签 | `XNALabel` | 代码 |
| `lblPlayerName` | 名称标签 | `XNALabel` | 代码 |
| `tbPlayerName` | 名称输入框 | `XNATextBox` | 代码 |

### CoopBriefingBox

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\CoopBriefingBox.cs`　基类：`XNAPanel`　INI：`CoopBriefingBox.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### GameCreationWindow

创建多人房间窗口，含房间名、密码、最大玩家数和隧道选择。

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\GameCreationWindow.cs`　基类：`XNAWindow`　INI：`GameCreationWindow.ini`　控件：18 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnAutoSelectTunnel` | 自动选择隧道按钮 | `XNAClientButton` | 代码 |
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnCreateGame` | 创建房间按钮 | `XNAClientButton` | 代码 |
| `btnDisplayAdvancedOptions` | 显示高级选项设置按钮 | `XNAClientButton` | 代码 |
| `btnLoadMPGame` | 载入多人存档按钮 | `XNAClientButton` | 代码 |
| `btnSaveDefaultTunnel` | 保存默认隧道按钮 | `XNAClientButton` | 代码 |
| `ddMaxPlayers` | 最大玩家数下拉框 | `XNAClientDropDown` | 代码 |
| `ddSkillLevel` | 技能等级下拉框 | `XNAClientDropDown` | 代码 |
| `ddTunnelMode` | 隧道模式下拉框 | `XNADropDown` | 代码 |
| `lblMaxPlayers` | 最大玩家数标签 | `XNALabel` | 代码 |
| `lblPassword` | 密码标签 | `XNALabel` | 代码 |
| `lblRoomName` | 房间名标签 | `XNALabel` | 代码 |
| `lblSkillLevel` | 技能等级标签 | `XNALabel` | 代码 |
| `lblTunnelServer` | 隧道服务器标签 | `XNALabel` | 代码 |
| `lbTunnelList` | 隧道列表 | `TunnelListBox` | 代码 |
| `pnlTunnelListDisabledOverlay` | 隧道列表禁用面板 | `XNAPanel` | 代码 |
| `tbGameName` | 名称输入框 | `XNATextBox` | 代码 |
| `tbPassword` | 密码输入框 | `XNATextBox` | 代码 |

### GameFiltersPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameFiltersPanel.cs`　基类：`XNAPanel`　INI：`GameFiltersPanel.ini`　控件：12 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `BottomButtonPanel` | 底部按钮面板 | `XNAPanel` | 代码 |
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnResetDefaults` | 重置默认按钮 | `XNAClientButton` | 代码 |
| `btnSave` | 保存按钮 | `XNAClientButton` | 代码 |
| `chkBoxFriendsOnly` | 仅好友 | `XNAClientCheckBox` | 代码 |
| `chkBoxHideIncompatibleGames` | 隐藏不兼容游戏 | `XNAClientCheckBox` | 代码 |
| `chkBoxHideLockedGames` | 隐藏锁定游戏 | `XNAClientCheckBox` | 代码 |
| `chkBoxHidePasswordedGames` | 密码复选框 | `XNAClientCheckBox` | 代码 |
| `ddMaxPlayerCount` | 在线人数下拉框 | `XNAClientDropDown` | 代码 |
| `FiltersScrollPanel` | 筛选面板 | `GameFiltersScrollPanel` | 代码 |
| `lblMaxPlayerCount` | 在线人数标签 | `XNALabel` | 代码 |
| `lblTitle` | 文本标签 | `XNALabel` | 代码 |

### GameFiltersScrollPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameFiltersPanel.cs`　基类：`XNAScrollPanel`　INI：`GameFiltersScrollPanel.ini`　控件：12 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `BottomButtonPanel` | 底部按钮面板 | `XNAPanel` | 代码 |
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnResetDefaults` | 重置默认按钮 | `XNAClientButton` | 代码 |
| `btnSave` | 保存按钮 | `XNAClientButton` | 代码 |
| `chkBoxFriendsOnly` | 仅好友 | `XNAClientCheckBox` | 代码 |
| `chkBoxHideIncompatibleGames` | 隐藏不兼容游戏 | `XNAClientCheckBox` | 代码 |
| `chkBoxHideLockedGames` | 隐藏锁定游戏 | `XNAClientCheckBox` | 代码 |
| `chkBoxHidePasswordedGames` | 密码复选框 | `XNAClientCheckBox` | 代码 |
| `ddMaxPlayerCount` | 在线人数下拉框 | `XNAClientDropDown` | 代码 |
| `FiltersScrollPanel` | 筛选面板 | `GameFiltersScrollPanel` | 代码 |
| `lblMaxPlayerCount` | 在线人数标签 | `XNALabel` | 代码 |
| `lblTitle` | 文本标签 | `XNALabel` | 代码 |

### GameInformationIconOnlyPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameInformationIconOnlyPanel.cs`　基类：`XNAPanel`　INI：`GameInformationIconOnlyPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### GameInformationIconPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameInformationIconPanel.cs`　基类：`XNAPanel`　INI：`GameInformationIconPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### GameInformationPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameInformationPanel.cs`　基类：`XNAPanel`　INI：`GameInformationPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### GameLoadingLobbyBase

多人读档大厅的共享布局。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLoadingLobbyBase.cs`　基类：`XNAWindow, ISwitchable`　INI：`GameLoadingLobbyBase.ini`　控件：12 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnLeaveGame` | 返回主菜单 | `XNAClientButton` | 代码 |
| `btnLoadGame` | 载入存档 | `XNAClientButton` | 代码 |
| `ddSavedGame` | 保存下拉框 | `XNAClientDropDown` | 代码 |
| `lbChatMessages` | 聊天记录 | `ChatListBox` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblGameMode` | 游戏模式 | `XNALabel` | 代码 |
| `lblGameModeValue` | 游戏模式值标签 | `XNALabel` | 代码 |
| `lblMapName` | 地图名称 | `XNALabel` | 代码 |
| `lblMapNameValue` | 地图名称值标签 | `XNALabel` | 代码 |
| `lblPlayerName` | 名称标签 | `XNALabel` | 代码 |
| `lblSavedGameTime` | 保存时间标签 | `XNALabel` | 代码 |
| `tbChatInput` | 聊天输入 | `XNATextBox` | 代码 |

### GameLobbyBase

所有游戏大厅的共享布局，包括地图列表、地图预览、玩家选项和游戏选项。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\GameLobbyBase.cs`　基类：`INItializableWindow`　INI：`GameLobbyBase.ini`　控件：31 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnLaunchGame` | 启动游戏 | `GameLaunchButton` | 源码读取 |
| `btnLeaveGame` | 返回主菜单 | `XNAClientButton` | 源码读取 |
| `btnMapSortAlphabetically` | 地图按字母排序 | `XNAClientStateButton<SortDirection>` | 代码 |
| `btnPickRandomMap` | 随机地图 | `XNAClientButton` | 源码读取 |
| `btnPlayerAIQuickOptionsOpen` | 打开 AI 快捷选项 | `XNAClientButton` | 源码读取 |
| `btnPlayerExtraOptionsOpen` | 打开玩家额外选项 | `XNAClientButton` | 源码读取 |
| `btnPlayerNameOptionsOpen` | 打开名称选项 | `XNAClientButton` | 源码读取 |
| `btnSaveLoadGameOptions` | 保存/载入选项预设 | `XNAClientButton` | 源码读取 |
| `ddDifficulty` | 难度（可选） | `XNAClientDropDown` | 源码读取 |
| `ddGameMode` | 游戏模式筛选 | `XNAClientDropDown` | 源码读取 |
| `ddPlayerColor` | 颜色 | `XNAClientColorDropDown` | 代码 |
| `ddPlayerName` | 玩家名 | `XNAClientDropDown` | 代码 |
| `ddPlayerSide` | 阵营 | `XNAClientDropDown` | 代码 |
| `ddPlayerStart` | 起始位置 | `XNAClientDropDown` | 代码 |
| `ddPlayerTeam` | 队伍 | `XNAClientDropDown` | 代码 |
| `lblDifficulty` | 难度标签 | `XNALabel` | 源码读取 |
| `lblGameMode` | 游戏模式 | `XNALabel` | 源码读取 |
| `lblGameModeSelect` | 游戏模式筛选标题 | `XNALabel` | 源码读取 |
| `lblMapAuthor` | 地图作者 | `XNALabel` | 源码读取 |
| `lblMapName` | 地图名称 | `XNALabel` | 源码读取 |
| `lblMapSize` | 地图尺寸 | `XNALabel` | 源码读取 |
| `lbMapList` | 地图列表 | `XNAMultiColumnListBox` | 源码读取 |
| `loadSaveGameOptionsMenu` | 选项预设菜单 | `XNAContextMenu` | 代码 |
| `mapContextMenu` | 地图列表右键菜单 | `XNAContextMenu` | 代码 |
| `MapPreviewBox` | 地图预览 | `MapPreviewBox` | 源码读取 |
| `PlayerAIQuickOptionsPanel` | 玩家  快捷选项面板 | `PlayerAIQuickOptionsPanel` | 源码读取 |
| `PlayerExtraOptionsPanel` | 玩家额外选项面板 | `PlayerExtraOptionsPanel` | 源码读取 |
| `PlayerNameOptionsPanel` | 玩家名称选项面板 | `PlayerNameOptionsPanel` | 代码 |
| `PlayerOptionsPanel` | 玩家选项容器 | `XNAPanel` | 源码读取 |
| `searchContextMenu` | 搜索框右键菜单 | `XNAContextMenu` | 代码 |
| `tbMapSearch` | 地图搜索 | `XNASuggestionTextBox` | 源码读取 |

### LANGameCreationWindow

- 源码：`DXMainClient\DXGUI\Multiplayer\LANGameCreationWindow.cs`　基类：`XNAWindow`　INI：`LANGameCreationWindow.ini`　控件：4 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAButton` | 代码 |
| `btnLoadGame` | 载入存档 | `XNAButton` | 代码 |
| `btnNewGame` | 创建房间 | `XNAButton` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |

### LANGameLoadingLobby

- 源码：`DXMainClient\DXGUI\Multiplayer\LANGameLoadingLobby.cs`　基类：`GameLoadingLobbyBase`　INI：`LANGameLoadingLobby.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### LANGameLobby

局域网大厅，布局全部继承 MultiplayerGameLobby。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\LANGameLobby.cs`　基类：`MultiplayerGameLobby`　INI：`LANGameLobby.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### LANLobby

局域网/在线大厅的共享布局；实际控件由 CnCNetLobby 创建。

- 源码：`DXMainClient\DXGUI\Multiplayer\LANLobby.cs`　基类：`XNAWindow`　INI：`LANLobby.ini`　控件：9 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnJoinGame` | 加入房间 | `XNAClientButton` | 代码 |
| `btnMainMenu` | 返回主菜单 | `XNAClientButton` | 代码 |
| `btnNewGame` | 创建房间 | `XNAClientButton` | 代码 |
| `ddColor` | 聊天颜色选择 | `XNAClientDropDown` | 代码 |
| `lbChatMessages` | 聊天记录 | `ChatListBox` | 代码 |
| `lbGameList` | 房间列表 | `GameListBox` | 代码 |
| `lblColor` | “您的颜色”标题 | `XNALabel` | 代码 |
| `lbPlayerList` | 玩家列表 | `XNAListBox` | 代码 |
| `tbChatInput` | 聊天输入 | `XNAChatTextBox` | 代码 |

### LoadOrSaveGameOptionPresetWindow

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\LoadOrSaveGameOptionPresetWindow.cs`　基类：`XNAWindow`　INI：`LoadOrSaveGameOptionPresetWindow.ini`　控件：7 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnDelete` | 删除按钮 | `XNAClientButton` | 代码 |
| `btnLoadSave` | 保存按钮 | `XNAClientButton` | 代码 |
| `ddPresetSelect` | 预设下拉框 | `XNAClientDropDown` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblNewPresetName` | 新建预设名称标签 | `XNALabel` | 代码 |
| `lblPresetName` | 预设名称标签 | `XNALabel` | 代码 |
| `tbNewPresetName` | 新建预设名称输入框 | `XNATextBox` | 代码 |

### MapPreviewBox

大厅地图预览框。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\MapPreviewBox.cs`　基类：`XNAPanel, ICompositeControl`　INI：`MapPreviewBox.ini`　控件：1 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `mainContextMenu` | 主右键菜单 | `XNAContextMenu` | 代码 |

### MapSharingConfirmationPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\MapSharingConfirmationPanel.cs`　基类：`XNAPanel`　INI：`MapSharingConfirmationPanel.ini`　控件：2 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnDownload` | 下载按钮 | `XNAClientButton` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |

### MultiplayerGameLobby

多人大厅的聊天、锁定与自动准备控件。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\MultiplayerGameLobby.cs`　基类：`GameLobbyBase, ISwitchable`　INI：`MultiplayerGameLobby.ini`　控件：9 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnLockGame` | 锁定房间 | `XNAClientButton` | 源码读取 |
| `chkAutoReady` | 自动准备 | `XNAClientCheckBox` | 源码读取 |
| `lbChatMessages` | 聊天记录 | `ChatListBox` | 源码读取 |
| `lbChatMessages_Host` | 聊天记录_列表框 | `ChatListBox` | 代码 |
| `lbChatMessages_Player` | 玩家聊天记录 | `ChatListBox` | 代码 |
| `playerStatusIndicator` | 玩家状态指示器 | `XNAPlayerSlotIndicator` | 代码 |
| `tbChatInput` | 聊天输入 | `XNAChatTextBox` | 源码读取 |
| `tbChatInput_Host` | 聊天输入_输入框 | `XNAChatTextBox` | 代码 |
| `tbChatInput_Player` | 玩家聊天输入 | `XNAChatTextBox` | 代码 |

### PasswordRequestWindow

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\PasswordRequestWindow.cs`　基类：`XNAWindow`　INI：`PasswordRequestWindow.ini`　控件：4 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnOK` | 确定按钮 | `XNAClientButton` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `tbPassword` | 密码输入框 | `XNATextBox` | 代码 |

### PlayerAIQuickOptionsPanel

AI 快捷选项面板。

- 源码：`DXMainClient\DXGUI\Multiplayer\PlayerAIQuickOptionsPanel.cs`　基类：`XNAPanel`　INI：`PlayerAIQuickOptionsPanel.ini`　控件：9 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `btnResetQuickOptions` | 重置快捷选项按钮 | `XNAClientButton` | 代码 |
| `chkAutoAssignAIStarts` | 起始位置复选框 | `XNAClientCheckBox` | 代码 |
| `chkFPPlayer` | 第一人称玩家 | `XNAClientCheckBox` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblRandomHeader` | 标题标签 | `XNALabel` | 代码 |
| `pnlFormatPainter` | 格式刷面板 | `XNAPanel` | 代码 |
| `pnlQuickActions` | 快捷操作面板 | `XNAPanel` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |

### PlayerExtraOptionsPanel

玩家额外选项面板：强制阵营/颜色/起始位置、队伍起始映射。

- 源码：`DXMainClient\DXGUI\Multiplayer\PlayerExtraOptionsPanel.cs`　基类：`XNAPanel`　INI：`PlayerExtraOptionsPanel.ini`　控件：11 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `btnHelp` | 帮助 | `XNAClientButton` | 代码 |
| `chkBoxForceNoTeams` | 否队伍复选框 | `XNAClientCheckBox` | 代码 |
| `chkBoxForceRandomColors` | 颜色复选框 | `XNAClientCheckBox` | 代码 |
| `chkBoxForceRandomSides` | 阵营复选框 | `XNAClientCheckBox` | 代码 |
| `chkBoxForceRandomStarts` | 起始位置复选框 | `XNAClientCheckBox` | 代码 |
| `chkBoxUseTeamStartMappings` | 队伍起始映射复选框 | `XNAClientCheckBox` | 代码 |
| `ddTeamStartMappingPreset` | 队伍起始映射预设下拉框 | `XNAClientDropDown` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblPreset` | 预设标签 | `XNALabel` | 代码 |
| `teamStartMappingsPanel` | 起始位置面板 | `TeamStartMappingsPanel` | 代码 |

### PlayerNameOptionsPanel

玩家名称选项面板。

- 源码：`DXMainClient\DXGUI\Multiplayer\PlayerNameOptionsPanel.cs`　基类：`XNAPanel`　INI：`PlayerNameOptionsPanel.ini`　控件：12 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `chkAllowCustomNames` | 允许自定义名称复选框 | `XNAClientCheckBox` | 代码 |
| `chkEnableCustomName` | 启用自定义名称复选框 | `XNAClientCheckBox` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblLobbyName` | 大厅名称标签 | `XNALabel` | 代码 |
| `lblLobbyNameValue` | 大厅名称值标签 | `XNALabel` | 代码 |
| `lblNewName` | 新名称标签 | `XNALabel` | 代码 |
| `lblOtherHeader` | 其他玩家标题标签 | `XNALabel` | 代码 |
| `playerSlotsScrollPanel` | 玩家槽位滚动面板 | `PlayerSlotsScrollPanel` | 代码 |
| `pnlOtherSlot` | 其他玩家槽位面板 | `XNAPanel` | 代码 |
| `tbCustomName` | 名称输入框 | `XNATextBox` | 代码 |
| `tbOther` | 其他玩家名称输入 | `XNATextBox` | 代码 |

### PlayerSlotsScrollPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\PlayerNameOptionsPanel.cs`　基类：`XNAScrollPanel`　INI：`PlayerSlotsScrollPanel.ini`　控件：11 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `chkAllowCustomNames` | 允许自定义名称复选框 | `XNAClientCheckBox` | 代码 |
| `chkEnableCustomName` | 启用自定义名称复选框 | `XNAClientCheckBox` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblLobbyName` | 大厅名称标签 | `XNALabel` | 代码 |
| `lblLobbyNameValue` | 大厅名称值标签 | `XNALabel` | 代码 |
| `lblNewName` | 新名称标签 | `XNALabel` | 代码 |
| `lblOtherHeader` | 其他玩家标题标签 | `XNALabel` | 代码 |
| `pnlOtherSlot` | 其他玩家槽位面板 | `XNAPanel` | 代码 |
| `tbCustomName` | 名称输入框 | `XNATextBox` | 代码 |
| `tbOther` | 其他玩家名称输入 | `XNATextBox` | 代码 |

### PrivateMessageNotificationBox

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessageNotificationBox.cs`　基类：`XNAPanel`　INI：`PrivateMessageNotificationBox.ini`　控件：6 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `gameIconPanel` | 游戏图标面板 | `XNAPanel` | 代码 |
| `lblHeader` | 标题标签 | `XNALabel` | 代码 |
| `lblHint` | 文本标签 | `XNALabel` | 代码 |
| `lblMessage` | 文本标签 | `XNALabel` | 代码 |
| `lblSender` | 文本标签 | `XNALabel` | 代码 |
| `linePanel` | 分隔线面板 | `XNAPanel` | 代码 |

### PrivateMessagingPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessagingPanel.cs`　基类：`DarkeningPanel`　INI：`PrivateMessagingPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### PrivateMessagingWindow

私信窗口。

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessagingWindow.cs`　基类：`XNAWindow, ISwitchable`　INI：`PrivateMessagingWindow.ini`　控件：7 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `lblMessages` | 文本标签 | `XNALabel` | 代码 |
| `lblPlayers` | 文本标签 | `XNALabel` | 代码 |
| `lblPrivateMessaging` | 文本标签 | `XNALabel` | 代码 |
| `lbMessages` | 消息列表 | `ChatListBox` | 代码 |
| `lbUserList` | 用户列表 | `XNAListBox` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |
| `tbMessageInput` | 私信输入 | `XNATextBox` | 代码 |

### SkirmishLobby

遭遇战大厅，布局全部继承 GameLobbyBase。

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\SkirmishLobby.cs`　基类：`GameLobbyBase, ISwitchable`　INI：`SkirmishLobby.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### TeamStartMappingPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\TeamStartMappingPanel.cs`　基类：`XNAPanel`　INI：`TeamStartMappingPanel.ini`　控件：1 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `ddTeams` | 队伍下拉框 | `XNAClientDropDown` | 代码 |

### TeamStartMappingsPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\TeamStartMappingsPanel.cs`　基类：`XNAPanel`　INI：`TeamStartMappingsPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### TunnelNegotiationStatusPanel

- 源码：`DXMainClient\DXGUI\Multiplayer\GameLobby\TunnelNegotiationStatusPanel.cs`　基类：`XNAPanel`　INI：`TunnelNegotiationStatusPanel.ini`　控件：6 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnClose` | 关闭按钮 | `XNAClientButton` | 代码 |
| `btnRenegotiateAll` | 全部按钮 | `XNAClientButton` | 代码 |
| `lblTitle` | 文本标签 | `XNALabel` | 代码 |
| `lbPairs` | 玩家连接对列表 | `XNAMultiColumnListBox` | 代码 |
| `matrixPanel` | 隧道连接矩阵 | `XNAPanel` | 代码 |
| `tabControl` | 标签页控件 | `XNAClientTabControl` | 代码 |

### TunnelSelectionWindow

隧道选择窗口。

- 源码：`DXMainClient\DXGUI\Multiplayer\CnCNet\TunnelSelectionWindow.cs`　基类：`XNAWindow`　INI：`TunnelSelectionWindow.ini`　控件：8 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnApply` | 应用按钮 | `XNAClientButton` | 代码 |
| `btnAutoSelectTunnel` | 自动选择隧道按钮 | `XNAClientButton` | 代码 |
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnSaveDefaultTunnel` | 保存默认隧道按钮 | `XNAClientButton` | 代码 |
| `ddMode` | 隧道模式选择 | `XNADropDown` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lbTunnelList` | 隧道列表 | `TunnelListBox` | 代码 |
| `pnlTunnelListDisabledOverlay` | 隧道列表禁用面板 | `XNAPanel` | 代码 |

## 战役

### CampaignSelector

战役任务选择窗口，包含任务列表、描述和难度选择。

- 源码：`DXMainClient\DXGUI\Campaign\CampaignSelector.cs`　基类：`XNAWindow`　INI：`CampaignSelector.ini`　控件：17 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnLaunch` | 启动游戏/任务 | `XNAClientButton` | 代码 |
| `btnReturn` | 返回 | `XNAClientButton` | 代码 |
| `lbCampaignList` | 战役任务列表 | `XNAListBox` | 代码 |
| `lblDifficultyLevel` | 难度标题 | `XNALabel` | 代码 |
| `lblEasy` | 简单 | `XNALabel` | 代码 |
| `lblGameSpeed` | 游戏速度标签 | `XNALabel` | 代码 |
| `lblHard` | 困难 | `XNALabel` | 代码 |
| `lblMissionDescriptionHeader` | 任务描述标题 | `XNALabel` | 代码 |
| `lblNormal` | 普通 | `XNALabel` | 代码 |
| `lblSelectCampaign` | 文本标签 | `XNALabel` | 代码 |
| `pnlMissionPreview` | 任务预览面板 | `XNAPanel` | 代码 |
| `spnMissionDescription` | 说明文字滚动面板 | `XNAScrollPanel` | 代码 |
| `tbMissionDescription` | 任务描述 | `XNATextBlock` | 代码 |
| `trbDifficultySelector` | 难度滑块 | `XNATrackbar` | 代码 |
| `trbGameSpeedSelector` | 游戏速度滑动条 | `XNATrackbar` | 代码 |
| `trMissionDescription` | 说明文字控件 | `XNATextRenderer` | 代码 |

### CampaignTagSelector

- 源码：`DXMainClient\DXGUI\Campaign\CampaignTagSelector.cs`　基类：`INItializableWindow`　INI：`CampaignTagSelector.ini`　控件：2 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 源码读取 |
| `btnShowAllMission` | 显示全部任务 | `XNAClientButton` | 源码读取 |

### CheaterWindow

作弊/文件修改检测提示窗口。

- 源码：`DXMainClient\DXGUI\Campaign\CheaterWindow.cs`　基类：`XNAWindow`　INI：`CheaterScreen.ini`　控件：5 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnCancel` | 取消按钮 | `XNAClientButton` | 代码 |
| `btnYes` | 是按钮 | `XNAClientButton` | 代码 |
| `imagePanel` | 提示图标面板 | `XNAPanel` | 代码 |
| `lblCheater` | 文本标签 | `XNALabel` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |

## 选项窗口面板

### AudioOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\AudioOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`AudioOptionsPanel.ini`　控件：17 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `chkMainMenuMusic` | 音乐复选框 | `XNAClientCheckBox` | 代码 |
| `chkPlaySoundOnGameHosted` | 声音复选框 | `XNAClientCheckBox` | 代码 |
| `chkScoreShuffle` | 得分复选框 | `XNAClientCheckBox` | 代码 |
| `chkStopGameLobbyMessageAudio` | 停止游戏大厅消息音效 | `XNAClientCheckBox` | 代码 |
| `chkStopMusicOnMenu` | 菜单停止音乐复选框 | `XNAClientCheckBox` | 代码 |
| `lblClientVolume` | 音量标签 | `XNALabel` | 代码 |
| `lblClientVolumeValue` | 音量值标签 | `XNALabel` | 代码 |
| `lblScoreVolume` | 得分音量标签 | `XNALabel` | 代码 |
| `lblScoreVolumeValue` | 得分音量值标签 | `XNALabel` | 代码 |
| `lblSoundVolume` | 声音音量标签 | `XNALabel` | 代码 |
| `lblSoundVolumeValue` | 声音音量值标签 | `XNALabel` | 代码 |
| `lblVoiceVolume` | 语音音量标签 | `XNALabel` | 代码 |
| `lblVoiceVolumeValue` | 语音音量值标签 | `XNALabel` | 代码 |
| `trbClientVolume` | 音量滑动条 | `XNATrackbar` | 代码 |
| `trbScoreVolume` | 得分音量滑动条 | `XNATrackbar` | 代码 |
| `trbSoundVolume` | 声音音量滑动条 | `XNATrackbar` | 代码 |
| `trbVoiceVolume` | 语音音量滑动条 | `XNATrackbar` | 代码 |

### CnCNetOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\CnCNetOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`CnCNetOptionsPanel.ini`　控件：19 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `chkAllowGameInvitesFromFriendsOnly` | 全部复选框 | `XNAClientCheckBox` | 代码 |
| `chkConnectOnStartup` | 启动时连接复选框 | `XNAClientCheckBox` | 代码 |
| `chkDisablePrivateMessagePopup` | 私信弹窗复选框 | `XNAClientCheckBox` | 代码 |
| `chkDiscordIntegration` | 集成复选框 | `XNAClientCheckBox` | 代码 |
| `chkEnableP2P` | 允许2复选框 | `XNAClientCheckBox` | 代码 |
| `chkNotifyOnUserListChange` | 否用户列表 | `XNAClientCheckBox` | 代码 |
| `chkPersistentMode` | 持久模式复选框 | `XNAClientCheckBox` | 代码 |
| `chkPingUnofficialTunnels` | 延迟检测非官方复选框 | `XNAClientCheckBox` | 代码 |
| `chkSkipLoginWindow` | 跳过登录复选框 | `XNAClientCheckBox` | 代码 |
| `chkSteamIntegration` | 集成复选框 | `XNAClientCheckBox` | 代码 |
| `chkWriteInstallPathToRegistry` | 将安装路径写入注册表 | `XNAClientCheckBox` | 代码 |
| `ddAllowPrivateMessagesFrom` | 允许私信范围 | `XNAClientDropDown` | 代码 |
| `ddTunnelMode` | 隧道模式下拉框 | `XNAClientDropDown` | 代码 |
| `gameIcon` | 游戏图标 | `XNAPanel` | 代码 |
| `gameListPanel` | 游戏列表面板 | `XNAPanel` | 代码 |
| `gamePanel` | 游戏信息面板 | `XNAPanel` | 代码 |
| `lblAllPrivateMessagesFrom` | 全部私信标签 | `XNALabel` | 代码 |
| `lblFollowedGames` | 文本标签 | `XNALabel` | 代码 |
| `lblTunnelMode` | 隧道模式标签 | `XNALabel` | 代码 |

### ComponentsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\ComponentsPanel.cs`　基类：`XNAOptionsPanel`　INI：`ComponentsPanel.ini`　控件：0 个

无自有控件；控件全部来自基类或 INI 额外控件。

### DisplayOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\DisplayOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`DisplayOptionsPanel.ini`　控件：25 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnGameCompatibilityFix` | 兼容性修复按钮 | `XNAClientButton` | 代码 |
| `btnMapEditorCompatibilityFix` | 地图编辑器兼容性修复按钮 | `XNAClientButton` | 代码 |
| `chkBackBufferInVRAM` | 后台缓冲显存复选框 | `XNAClientCheckBox` | 代码 |
| `chkBorderlessClient` | 无边框客户端复选框 | `XNAClientCheckBox` | 代码 |
| `chkBorderlessWindowedMode` | 无边框窗口模式复选框 | `XNAClientCheckBox` | 代码 |
| `chkEnableBackgroundVideo` | 背景视频复选框 | `XNAClientCheckBox` | 代码 |
| `chkEnableBackgroundVideoSound` | 背景视频声音复选框 | `XNAClientCheckBox` | 代码 |
| `chkIntegerScaledClient` | 整数缩放复选框 | `XNAClientCheckBox` | 代码 |
| `chkUseTTFFonts` | 使用字体复选框 | `XNAClientCheckBox` | 代码 |
| `chkWindowedMode` | 窗口模式复选框 | `XNAClientCheckBox` | 代码 |
| `ddClientResolution` | 客户端分辨率下拉框 | `XNAClientPreferredItemDropDown` | 代码 |
| `ddClientTheme` | 客户端主题下拉框 | `XNAClientDropDown` | 代码 |
| `ddDetailLevel` | 细节等级选择 | `XNAClientDropDown` | 代码 |
| `ddIngameResolution` | 游戏内分辨率下拉框 | `XNAClientDropDown` | 代码 |
| `ddRenderer` | 渲染器下拉框 | `XNAClientDropDown` | 代码 |
| `ddTranslation` | 语言下拉框 | `XNAClientDropDown` | 代码 |
| `lblClientResolution` | 客户端分辨率标签 | `XNALabel` | 代码 |
| `lblClientTheme` | 客户端主题标签 | `XNALabel` | 代码 |
| `lblCompatibilityFixes` | 兼容性修复标签 | `XNALabel` | 代码 |
| `lblDetailLevel` | 细节等级标签 | `XNALabel` | 代码 |
| `lblGameCompatibilityFix` | 兼容性修复标签 | `XNALabel` | 代码 |
| `lblIngameResolution` | 游戏内分辨率标签 | `XNALabel` | 代码 |
| `lblMapEditorCompatibilityFix` | 地图编辑器兼容性修复标签 | `XNALabel` | 代码 |
| `lblRenderer` | 渲染器标签 | `XNALabel` | 代码 |
| `lblTranslation` | 语言标签 | `XNALabel` | 代码 |

### GameOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\GameOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`GameOptionsPanel.ini`　控件：14 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnConfigureHotkeys` | 热键按钮 | `XNAClientButton` | 代码 |
| `chkAltToUndeploy` | 取消部署复选框 | `SettingCheckBox` | 代码 |
| `chkBlackChatBackground` | 黑色聊天背景复选框 | `SettingCheckBox` | 代码 |
| `chkRecordReplays` | 录制回放复选框 | `XNAClientCheckBox` | 代码 |
| `chkScrollCoasting` | 惯性复选框 | `SettingCheckBox` | 代码 |
| `chkShowHiddenObjects` | 显示隐藏对象复选框 | `SettingCheckBox` | 代码 |
| `chkTargetLines` | 目标连线复选框 | `SettingCheckBox` | 代码 |
| `chkTooltips` | 提示复选框 | `SettingCheckBox` | 代码 |
| `lblNotice` | 否标签 | `XNALabel` | 代码 |
| `lblPlayerName` | 名称标签 | `XNALabel` | 代码 |
| `lblScrollRate` | 滚动速度标签 | `XNALabel` | 代码 |
| `lblScrollRateValue` | 滚动速度值标签 | `XNALabel` | 代码 |
| `tbPlayerName` | 名称输入框 | `XNATextBox` | 代码 |
| `trbScrollRate` | 滚动速度滑动条 | `XNATrackbar` | 代码 |

### StorageOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\StorageOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`StorageOptionsPanel.ini`　控件：37 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `bottomMargin` | 底部留白面板 | `XNAPanel` | 代码 |
| `lblFolderSize` | 尺寸标签 | `XNALabel` | 代码 |
| `lblFolderSizeSuffix` | 尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogAge` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogAgeSuffix` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogFolderSize` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogFolderSizeSuffix` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogRetentionHint` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogsHeader` | 日志标题标签 | `XNALabel` | 代码 |
| `lblKeptLogFiles` | 日志标签 | `XNALabel` | 代码 |
| `lblKeptLogFilesSuffix` | 日志标签 | `XNALabel` | 代码 |
| `lblKeptReplays` | 回放标签 | `XNALabel` | 代码 |
| `lblKeptReplaysSuffix` | 回放标签 | `XNALabel` | 代码 |
| `lblKeptSavedGames` | 保存标签 | `XNALabel` | 代码 |
| `lblKeptSavedGamesSuffix` | 保存标签 | `XNALabel` | 代码 |
| `lblKeyframesHeader` | 按键标题标签 | `XNALabel` | 代码 |
| `lblKeyframeSize` | 按键尺寸标签 | `XNALabel` | 代码 |
| `lblKeyframeSizeSuffix` | 按键尺寸标签 | `XNALabel` | 代码 |
| `lblLogFolderSize` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblLogFolderSizeSuffix` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblLogsHeader` | 日志标题标签 | `XNALabel` | 代码 |
| `lblReplaysHeader` | 回放标题标签 | `XNALabel` | 代码 |
| `lblReplayUsage` | 回放标签 | `XNALabel` | 代码 |
| `lblSavedGameFolderSize` | 保存尺寸标签 | `XNALabel` | 代码 |
| `lblSavedGameFolderSizeSuffix` | 保存尺寸标签 | `XNALabel` | 代码 |
| `lblSavedGameRetentionHint` | 保存标签 | `XNALabel` | 代码 |
| `lblSavedGamesHeader` | 保存标题标签 | `XNALabel` | 代码 |
| `scrollPanel` | 滚动容器 | `StorageScrollPanel` | 代码 |
| `tbMaxGameLogAge` | 日志输入框 | `XNATextBox` | 代码 |
| `tbMaxGameLogFolderSize` | 日志尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptLogFiles` | 日志输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptReplays` | 回放输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptSavedGames` | 保存输入框 | `XNATextBox` | 代码 |
| `tbMaxLogFolderSize` | 日志尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxReplayFolderSize` | 回放尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxSavedGameFolderSize` | 保存尺寸输入框 | `XNATextBox` | 代码 |
| `tbReplayKeyframeStorageLimit` | 回放按键存储输入框 | `XNATextBox` | 代码 |

### StorageScrollPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\StorageOptionsPanel.cs`　基类：`XNAScrollPanel`　INI：`StorageScrollPanel.ini`　控件：37 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `bottomMargin` | 底部留白面板 | `XNAPanel` | 代码 |
| `lblFolderSize` | 尺寸标签 | `XNALabel` | 代码 |
| `lblFolderSizeSuffix` | 尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogAge` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogAgeSuffix` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogFolderSize` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogFolderSizeSuffix` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblGameLogRetentionHint` | 日志标签 | `XNALabel` | 代码 |
| `lblGameLogsHeader` | 日志标题标签 | `XNALabel` | 代码 |
| `lblKeptLogFiles` | 日志标签 | `XNALabel` | 代码 |
| `lblKeptLogFilesSuffix` | 日志标签 | `XNALabel` | 代码 |
| `lblKeptReplays` | 回放标签 | `XNALabel` | 代码 |
| `lblKeptReplaysSuffix` | 回放标签 | `XNALabel` | 代码 |
| `lblKeptSavedGames` | 保存标签 | `XNALabel` | 代码 |
| `lblKeptSavedGamesSuffix` | 保存标签 | `XNALabel` | 代码 |
| `lblKeyframesHeader` | 按键标题标签 | `XNALabel` | 代码 |
| `lblKeyframeSize` | 按键尺寸标签 | `XNALabel` | 代码 |
| `lblKeyframeSizeSuffix` | 按键尺寸标签 | `XNALabel` | 代码 |
| `lblLogFolderSize` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblLogFolderSizeSuffix` | 日志尺寸标签 | `XNALabel` | 代码 |
| `lblLogsHeader` | 日志标题标签 | `XNALabel` | 代码 |
| `lblReplaysHeader` | 回放标题标签 | `XNALabel` | 代码 |
| `lblReplayUsage` | 回放标签 | `XNALabel` | 代码 |
| `lblSavedGameFolderSize` | 保存尺寸标签 | `XNALabel` | 代码 |
| `lblSavedGameFolderSizeSuffix` | 保存尺寸标签 | `XNALabel` | 代码 |
| `lblSavedGameRetentionHint` | 保存标签 | `XNALabel` | 代码 |
| `lblSavedGamesHeader` | 保存标题标签 | `XNALabel` | 代码 |
| `scrollPanel` | 滚动容器 | `StorageScrollPanel` | 代码 |
| `tbMaxGameLogAge` | 日志输入框 | `XNATextBox` | 代码 |
| `tbMaxGameLogFolderSize` | 日志尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptLogFiles` | 日志输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptReplays` | 回放输入框 | `XNATextBox` | 代码 |
| `tbMaxKeptSavedGames` | 保存输入框 | `XNATextBox` | 代码 |
| `tbMaxLogFolderSize` | 日志尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxReplayFolderSize` | 回放尺寸输入框 | `XNATextBox` | 代码 |
| `tbMaxSavedGameFolderSize` | 保存尺寸输入框 | `XNATextBox` | 代码 |
| `tbReplayKeyframeStorageLimit` | 回放按键存储输入框 | `XNATextBox` | 代码 |

### UpdaterOptionsPanel

- 源码：`DXMainClient\DXGUI\Generic\OptionPanels\UpdaterOptionsPanel.cs`　基类：`XNAOptionsPanel`　INI：`UpdaterOptionsPanel.ini`　控件：6 个

| 控件名 | 用途 / 说明 | 类型 | 创建方式 |
|---|---|---|---|
| `btnForceUpdate` | 更新按钮 | `XNAClientButton` | 代码 |
| `btnMoveDown` | 下移 | `XNAClientButton` | 代码 |
| `btnMoveUp` | 上移 | `XNAClientButton` | 代码 |
| `chkAutoCheck` | 自动检查更新 | `XNAClientCheckBox` | 代码 |
| `lblDescription` | 说明文字标签 | `XNALabel` | 代码 |
| `lblUpdateServerList` | 更新列表 | `XNAListBox` | 代码 |


