# 已注册控件参考

本文档列出客户端源码中通过 `ClientGUICreator` 注册、可在 INI 中按名称创建的全部控件类型，供编写窗口 INI 时查阅。

- 注册位置：`DXMainClient/DXGUI/GameClass.cs` 的 `BuildServiceProvider`（单例与瞬时两段）。
- 查找与实例化逻辑：`ClientGUI/ClientGUICreator.cs`。
- 属性与 INI 键说明：见 [INISystem.md](INISystem.md)。
- 统计：单例 24 个，瞬时 36 个，合计 60 个。

## 注册机制

```csharp
services
    .AddSingletonXnaControl<MainMenu>()
    .AddSingletonXnaControl<MapPreviewBox>()
    // ...
    .AddTransientXnaControl<XNAClientButton>()
    .AddTransientXnaControl<XNAPanel>()
    // ...
```

规则：

- 注册名就是 C# 类型名（`Type.Name`），例如 `XNAClientButton`。INI 中写错或大小写不一致都会抛出 `Control type X was not registered with ServiceCollection in GameClass`。
- 只能注册 `XNAControl` 或其派生类；注册同名类型会抛出异常。名字不区分命名空间，因此不能注册两个同名类。
- 控件构造函数参数通过依赖注入容器解析，构造函数可以请求 `WindowManager` 等已注册服务。
- **单例（Singleton）**：在依赖注入容器中以单例注册，代码注入该类型时共享同一实例。
- **瞬时（Transient）**：在依赖注入容器中以瞬态注册，每次注入解析都会创建新实例。
- **INI 按名称创建**：无论注册为单例还是瞬时，`ClientGUICreator.GetXnaControl(name)` 都会直接调用构造函数并返回新实例，不会复用 DI 中的单例。因此全局窗口/大厅应通过代码注入获取，不要在 INI 中重复创建。

INI 中按名称创建控件的三种位置：

```ini
; 1. INItializableWindow：$CC 子控件
[MyWindow]
$CC00=btnOk:XNAClientButton
$CC01=pnlContent:XNAPanel

; 2. INItializableWindow：$ExtraControls 段
[$ExtraControls]
$CC00=btnExtra:XNAClientButton

; 3. XNAWindowBase：窗口级额外控件
[MyWindowExtraControls]
btnExtra=XNAClientButton
```

## 瞬时控件（Transient）

在依赖注入容器中以瞬态注册，适合窗口内部的普通控件；INI 每次按名称创建都会新建实例。

| 注册名 | 基类 | 源码位置 | 说明 | 文档 |
|---|---|---|---|---|
| `XNAControl` | `DrawableGameComponent` | `Rampastring.XNAUI/XNAControls/XNAControl.cs` | 所有控件的基类，仅提供坐标、尺寸、可见性等基础属性 | [XNAControl](INISystem.md#xnacontrol) |
| `XNAButton` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAButton.cs` | 基础按钮（空闲/悬停/按下纹理 + 文本） | [XNAButton](INISystem.md#xnabutton) |
| `XNAInteractionButton` | `XNAButton` | `ClientGUI/XNAInteractionButton.cs` | 可打开本地文件或结束进程的按钮（`OpenFiles`、`ExitProcess`） | [XNAInteractionButton](INISystem.md#xnainteractionbutton) |
| `XNAClientButton` | `XNAButton` | `ClientGUI/XNAClientButton.cs` | 客户端按钮，支持提示文本、按纹理尺寸匹配等 | [XNAClientButton](INISystem.md#xnaclientbutton) |
| `XNAClientCheckBox` | `XNACheckBox` | `ClientGUI/XNAClientCheckBox.cs` | 带提示文本的复选框 | [XNAClientCheckBox](INISystem.md#xnaclientcheckbox) |
| `XNAClientDropDown` | `XNADropDown` | `ClientGUI/XNAClientDropDown.cs` | 带提示文本的下拉框 | [XNAClientDropDown](INISystem.md#xnaclientdropdown) |
| `XNAClientScrollPanel` | `XNAScrollPanel` | `ClientGUI/XNAClientScrollPanel.cs` | 供 `INItializableWindow` 使用的复合滚动面板（内容面板与滚动条作为子控件注册） | [XNAScrollPanel](INISystem.md#xnascrollpanel) |
| `XNALinkButton` | `XNAClientButton` | `ClientGUI/XNALinkButton.cs` | 点击后由系统 Shell 打开 URL 的按钮 | [XNALinkButton](INISystem.md#xnalinkbutton) |
| `XNAExtraPanel` | `XNAPanel` | `ClientGUI/XNAExtraPanel.cs` | 尺寸为 0 时可自动匹配背景纹理尺寸的面板 | [XNAExtraPanel](INISystem.md#xnaextrappanel) |
| `XNACheckBox` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNACheckBox.cs` | 基础复选框，支持四种状态纹理 | [XNACheckBox](INISystem.md#xnacheckbox) |
| `XNADropDown` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNADropDown.cs` | 基础下拉框 | [XNADropDown](INISystem.md#xnadropdown) |
| `XNAClientTabControl` | `XNATabControl` | `ClientGUI/XNAClientTabControl.cs` | 客户端标签页控件，标签文本支持翻译 | [XNAClientTabControl](INISystem.md#xnaclienttabcontrol) |
| `XNATabControl` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNATabControl.cs` | 基础标签页控件（`SetTabCount`、`TabN.*`、`$ToggleN` 等） | [XNATabControl](INISystem.md#xnatabcontrol) |
| `XNALabel` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNALabel.cs` | 文本标签 | [XNALabel](INISystem.md#xnalabel) |
| `XNALinkLabel` | `XNALabel` | `Rampastring.XNAUI/XNAControls/XNALinkLabel.cs` | 可点击的链接标签 | [XNALinkLabel](INISystem.md#xnalinklabel) |
| `XNAClientLinkLabel` | `XNALinkLabel` | `ClientGUI/XNAClientLinkLabel.cs` | 客户端链接标签，支持悬停音效与提示 | [XNAClientLinkLabel](INISystem.md#xnaclientlinklabel) |
| `XNAListBox` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAListBox.cs` | 列表框 | [XNAListBox](INISystem.md#xnalistbox) |
| `XNAMultiColumnListBox` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAMultiColumnListBox.cs` | 多列列表框 | [XNAMultiColumnListBox](INISystem.md#xnamulticolumnlistbox) |
| `XNAPanel` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAPanel.cs` | 面板容器（背景、边框、内边距） | [XNAPanel](INISystem.md#xnapanel) |
| `XNAScrollPanel` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAScrollPanel.cs` | 可滚动面板 | [XNAScrollPanel](INISystem.md#xnascrollpanel) |
| `XNAProgressBar` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAProgressBar.cs` | 进度条 | [XNAProgressBar](INISystem.md#xnaprogressbar) |
| `XNASuggestionTextBox` | `XNATextBox` | `Rampastring.XNAUI/XNAControls/XNASuggestionTextBox.cs` | 带建议/占位文本的输入框 | [XNASuggestionTextBox](INISystem.md#xnasuggestiontextbox) |
| `XNATextBox` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNATextBox.cs` | 文本输入框 | [XNATextBox](INISystem.md#xnatextbox) |
| `XNATextBlock` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNATextBlock.cs` | 文本块（面板 + 文本） | [XNATextBlock](INISystem.md#xnatextblock) |
| `XNATrackbar` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNATrackbar.cs` | 滑动条 | [XNATrackbar](INISystem.md#xnatrackbar) |
| `XNAChatTextBox` | `XNASuggestionTextBox` | `ClientGUI/XNAChatTextBox.cs` | 聊天输入框（历史记录、命令输入等） | - |
| `ChatListBox` | `XNAListBox` | `DXMainClient/DXGUI/Multiplayer/ChatListBox.cs` | 聊天消息列表 | - |
| `INItializableWindow` | `XNAPanel` | `ClientGUI/INItializableWindow.cs` | 由 INI 定义子控件的窗口/面板容器（`$CC` / `$ExtraControls`） | [从 INI 创建控件](INISystem.md#从-ini-创建控件) |
| `GameLobbyCheckBox` | `GameSessionCheckBox` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyCheckBox.cs` | 大厅游戏选项复选框，可广播选项并写入 spawn.ini | [GameLobbyCheckBox](INISystem.md#gamelobbycheckbox) |
| `GameLobbyDropDown` | `GameSessionDropDown` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyDropDown.cs` | 大厅游戏选项下拉框 | [GameLobbyDropDown](INISystem.md#gamelobbydropdown) |
| `CampaignCheckBox` | `GameSessionCheckBox` | `DXMainClient/DXGUI/Campaign/CampaignCheckBox.cs` | 战役选项复选框 | [CampaignCheckBox](INISystem.md#campaigncheckbox) |
| `CampaignDropDown` | `GameSessionDropDown` | `DXMainClient/DXGUI/Campaign/CampaignDropDown.cs` | 战役选项下拉框 | [CampaignDropDown](INISystem.md#campaigndropdown) |
| `SettingCheckBox` | `SettingCheckBoxBase` | `ClientGUI/Settings/SettingCheckBox.cs` | 绑定到设置项的复选框 | [SettingCheckBox](INISystem.md#settingcheckbox) |
| `SettingDropDown` | `SettingDropDownBase` | `ClientGUI/Settings/SettingDropDown.cs` | 绑定到设置项的下拉框 | [SettingDropDown](INISystem.md#settingdropdown) |
| `FileSettingCheckBox` | `SettingCheckBoxBase` | `ClientGUI/Settings/FileSettingCheckBox.cs` | 文件类设置复选框（文件选择/校验） | [FileSettingCheckBox](INISystem.md#filesettingcheckbox) |
| `FileSettingDropDown` | `SettingDropDownBase` | `ClientGUI/Settings/FileSettingDropDown.cs` | 文件类设置下拉框 | [FileSettingDropDown](INISystem.md#filesettingdropdown) |

## 单例窗口（Singleton）

这些类型同时在依赖注入容器中以单例注册，代码注入时共享同一实例，适合窗口、大厅、主菜单等全局对象。

> [!WARNING]
> 通过 INI 按名称创建单例类型时，`ClientGUICreator` 仍会直接调用构造函数生成新实例，不会返回 DI 中的全局单例。因此不要用 `$CC` 或额外控件重复创建这些窗口/大厅，应在代码中通过依赖注入获取。

| 注册名 | 基类 | 源码位置 | 说明 | 文档 |
|---|---|---|---|---|
| `LoadingScreen` | `XNAWindow` | `DXMainClient/DXGUI/Generic/LoadingScreen.cs` | 启动加载画面窗口 | [LoadingScreen](INISystem.md#loadingscreen) |
| `TopBar` | `XNAPanel` | `DXMainClient/DXGUI/Generic/TopBar.cs` | 主菜单顶栏（版本、公告等） | - |
| `OptionsWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/OptionsWindow.cs` | 选项窗口（显示、音频、游戏、CnCNet、存储、更新、组件标签页） | - |
| `PrivateMessagingWindow` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/CnCNet/PrivateMessagingWindow.cs` | 私信窗口 | - |
| `PrivateMessagingPanel` | `DarkeningPanel` | `DXMainClient/DXGUI/Multiplayer/CnCNet/PrivateMessagingPanel.cs` | 私信窗口的深色遮罩宿主面板 | - |
| `LANLobby` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/LANLobby.cs` | 局域网大厅窗口 | - |
| `CnCNetGameLobby` | `MultiplayerGameLobby` | `DXMainClient/DXGUI/Multiplayer/GameLobby/CnCNetGameLobby.cs` | CnCNet 多人游戏大厅 | [GameLobbyBase 控件](INISystem.md) |
| `CnCNetGameLoadingLobby` | `GameLoadingLobbyBase` | `DXMainClient/DXGUI/Multiplayer/CnCNet/CnCNetGameLoadingLobby.cs` | CnCNet 游戏加载/读档大厅 | - |
| `CnCNetLobby` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/CnCNet/CnCNetLobby.cs` | CnCNet 房间列表大厅 | - |
| `GameInProgressWindow` | `XNAPanel` | `DXMainClient/DXGUI/Generic/GameInProgressWindow.cs` | “游戏进行中”提示窗口 | - |
| `SkirmishLobby` | `GameLobbyBase` | `DXMainClient/DXGUI/Multiplayer/GameLobby/SkirmishLobby.cs` | 遭遇战大厅 | [GameLobbyBase 控件](INISystem.md) |
| `MainMenu` | `XNAWindow` | `DXMainClient/DXGUI/Generic/MainMenu.cs` | 主菜单窗口 | [MainMenu](INISystem.md#mainmenu) |
| `MapPreviewBox` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/GameLobby/MapPreviewBox.cs` | 游戏大厅地图预览框 | [MapPreviewBox](INISystem.md#mappreviewbox) |
| `GameLaunchButton` | `XNAClientButton` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLaunchButton.cs` | 大厅“启动游戏”按钮 | - |
| `PlayerExtraOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerExtraOptionsPanel.cs` | 玩家额外选项面板（强制随机阵营/队伍/起始位置映射等） | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `PlayerAIQuickOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerAIQuickOptionsPanel.cs` | 玩家 AI 快捷选项面板（阵营、难度、颜色） | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `PlayerNameOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerNameOptionsPanel.cs` | 玩家名称选项面板 | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `CampaignTagSelector` | `INItializableWindow` | `DXMainClient/DXGUI/Campaign/CampaignTagSelector.cs` | 战役标签选择窗口 | [CampaignSelector](INISystem.md#campaignselector) |
| `GameLoadingWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/GameLoadingWindow.cs` | 读取游戏/回放窗口 | - |
| `StatisticsWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/StatisticsWindow.cs` | 对局统计窗口 | - |
| `UpdateQueryWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/UpdateQueryWindow.cs` | 更新询问窗口 | - |
| `ManualUpdateQueryWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/ManualUpdateQueryWindow.cs` | 手动更新询问窗口 | - |
| `UpdateWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/UpdateWindow.cs` | 更新执行窗口 | - |
| `ExtrasWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/ExtrasWindow.cs` | 附加内容窗口 | - |

## 文档中有章节但未注册的类型

以下类型在 [INISystem.md](INISystem.md) 中有属性说明，但**没有**在 `GameClass.cs` 中注册，因此不能通过 INI 的 `$CC` 或额外控件按名称创建，只能由代码创建：

| 类型 | 原因 / 使用方式 |
|---|---|
| `XNAIndicator` | 代码内使用的指示器控件 |
| `XNAContextMenu` | 代码内创建的右键菜单 |
| `XNAClientToggleButton` | 代码内使用的切换按钮 |
| `XNAClientColorDropDown` | 代码内创建的颜色下拉框（玩家颜色选择等） |
| `XNAClientPreferredItemDropDown` | 代码内使用的首选项下拉框 |
| `XNAOptionsPanel` | 选项面板基类，由代码创建 |
| `GameSessionCheckBox` / `GameSessionDropDown` | 代码基类；INI 中改用已注册的派生类型 `GameLobbyCheckBox`、`CampaignCheckBox` 等 |
| `CoopBriefingBox` | 由地图预览/大厅代码创建 |
| `CampaignSelector` | 由代码创建并读取自身 INI 的窗口 |
| `XNAPasswordBox` / `XNATimerControl` | 代码内使用 |
| `XNAWindow` / `XNAWindowBase` / `DarkeningPanel` | 窗口与面板基类，本身不作为可创建控件注册 |

## 新增控件注册

要让新控件能通过 INI 创建：

1. 新建类并继承 `XNAControl` 或其派生类，类名即注册名（不得与现有注册名重复）。
2. 在 `DXMainClient/DXGUI/GameClass.cs` 的 `BuildServiceProvider` 中注册：
   - 全局唯一对象（窗口、大厅等）用 `.AddSingletonXnaControl<YourControl>()`；
   - 普通控件用 `.AddTransientXnaControl<YourControl>()`。
3. 在控件中实现 `ParseControlINIAttribute` 以解析自定义 INI 键。
4. INI 中使用 `YourControl` 作为类型名，例如 `$CC00=myControl:YourControl`。

> 注意：注册名区分大小写，且必须与 C# 类型名完全一致（`Type.Name`），INI 中写错会直接报错。
