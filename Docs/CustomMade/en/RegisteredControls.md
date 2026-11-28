# Registered Controls Reference

This document lists every control type registered through `ClientGUICreator` that can be created by name from INI files.

- Registration: `BuildServiceProvider` in `DXMainClient/DXGUI/GameClass.cs` (singleton and transient blocks).
- Lookup and instantiation: `ClientGUI/ClientGUICreator.cs`.
- Property and INI key reference: see [INISystem.md](INISystem.md).
- Counts: 24 singletons and 36 transient controls, 60 in total.

## Registration Mechanism

```csharp
services
    .AddSingletonXnaControl<MainMenu>()
    .AddSingletonXnaControl<MapPreviewBox>()
    // ...
    .AddTransientXnaControl<XNAClientButton>()
    .AddTransientXnaControl<XNAPanel>()
    // ...
```

Rules:

- The registered name is the C# type name (`Type.Name`), for example `XNAClientButton`. A typo or a different casing in the INI throws `Control type X was not registered with ServiceCollection in GameClass`.
- Only `XNAControl` and its subclasses can be registered. Registering two types with the same name throws. Names ignore namespaces.
- Constructor parameters are resolved from the dependency injection container, so a control constructor can request `WindowManager` and other registered services.
- **Singleton**: registered as a singleton in the dependency injection container, so code that injects the type shares one instance.
- **Transient**: registered as transient, so every dependency injection resolution creates a new instance.
- **INI creation by name**: regardless of the registration lifetime, `ClientGUICreator.GetXnaControl(name)` invokes the constructor directly and returns a new instance. It does not reuse the DI singleton. Global windows and lobbies should therefore be obtained through dependency injection, not re-created from INI.

Three places where INI can create a control by name:

```ini
; 1. INItializableWindow: $CC child controls
[MyWindow]
$CC00=btnOk:XNAClientButton
$CC01=pnlContent:XNAPanel

; 2. INItializableWindow: $ExtraControls section
[$ExtraControls]
$CC00=btnExtra:XNAClientButton

; 3. XNAWindowBase: window-level extra controls
[MyWindowExtraControls]
btnExtra=XNAClientButton
```

## Transient Controls

Registered as transient in the dependency injection container. Suitable for ordinary controls inside windows; every INI creation by name produces a new instance.

| Registered name | Base class | Source | Purpose | Docs |
|---|---|---|---|---|
| `XNAControl` | `DrawableGameComponent` | `Rampastring.XNAUI/XNAControls/XNAControl.cs` | Base class of all controls; position, size, visibility and other basics | [XNAControl](INISystem.md#xnacontrol) |
| `XNAButton` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAButton.cs` | Basic button (idle/hover/pressed textures and text) | [XNAButton](INISystem.md#xnabutton) |
| `XNAInteractionButton` | `XNAButton` | `ClientGUI/XNAInteractionButton.cs` | Button that can open local files or terminate processes (`OpenFiles`, `ExitProcess`) | [XNAInteractionButton](INISystem.md#xnainteractionbutton) |
| `XNAClientButton` | `XNAButton` | `ClientGUI/XNAClientButton.cs` | Client button with tooltips and texture-size matching | [XNAClientButton](INISystem.md#xnaclientbutton) |
| `XNAClientCheckBox` | `XNACheckBox` | `ClientGUI/XNAClientCheckBox.cs` | Checkbox with a tooltip | [XNAClientCheckBox](INISystem.md#xnaclientcheckbox) |
| `XNAClientDropDown` | `XNADropDown` | `ClientGUI/XNAClientDropDown.cs` | Dropdown with a tooltip | [XNAClientDropDown](INISystem.md#xnaclientdropdown) |
| `XNAClientScrollPanel` | `XNAScrollPanel` | `ClientGUI/XNAClientScrollPanel.cs` | Composite scroll panel for `INItializableWindow` (content panel and scrollbars registered as sub-controls) | [XNAScrollPanel](INISystem.md#xnascrollpanel) |
| `XNALinkButton` | `XNAClientButton` | `ClientGUI/XNALinkButton.cs` | Button that opens a URL through the OS shell | [XNALinkButton](INISystem.md#xnalinkbutton) |
| `XNAExtraPanel` | `XNAPanel` | `ClientGUI/XNAExtraPanel.cs` | Panel that adopts the background texture size when its size is 0 | [XNAExtraPanel](INISystem.md#xnaextrappanel) |
| `XNACheckBox` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNACheckBox.cs` | Basic checkbox with four state textures | [XNACheckBox](INISystem.md#xnacheckbox) |
| `XNADropDown` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNADropDown.cs` | Basic dropdown | [XNADropDown](INISystem.md#xnadropdown) |
| `XNAClientTabControl` | `XNATabControl` | `ClientGUI/XNAClientTabControl.cs` | Client tab control with translatable tab text | [XNAClientTabControl](INISystem.md#xnaclienttabcontrol) |
| `XNATabControl` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNATabControl.cs` | Basic tab control (`SetTabCount`, `TabN.*`, `$ToggleN` and more) | [XNATabControl](INISystem.md#xnatabcontrol) |
| `XNALabel` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNALabel.cs` | Text label | [XNALabel](INISystem.md#xnalabel) |
| `XNALinkLabel` | `XNALabel` | `Rampastring.XNAUI/XNAControls/XNALinkLabel.cs` | Clickable link label | [XNALinkLabel](INISystem.md#xnalinklabel) |
| `XNAClientLinkLabel` | `XNALinkLabel` | `ClientGUI/XNAClientLinkLabel.cs` | Client link label with hover sound and tooltip | [XNAClientLinkLabel](INISystem.md#xnaclientlinklabel) |
| `XNAListBox` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAListBox.cs` | List box | [XNAListBox](INISystem.md#xnalistbox) |
| `XNAMultiColumnListBox` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAMultiColumnListBox.cs` | Multi-column list box | [XNAMultiColumnListBox](INISystem.md#xnamulticolumnlistbox) |
| `XNAPanel` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAPanel.cs` | Panel container (background, borders, padding) | [XNAPanel](INISystem.md#xnapanel) |
| `XNAScrollPanel` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNAScrollPanel.cs` | Scrollable panel | [XNAScrollPanel](INISystem.md#xnascrollpanel) |
| `XNAProgressBar` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNAProgressBar.cs` | Progress bar | [XNAProgressBar](INISystem.md#xnaprogressbar) |
| `XNASuggestionTextBox` | `XNATextBox` | `Rampastring.XNAUI/XNAControls/XNASuggestionTextBox.cs` | Text box with suggestion/placeholder text | [XNASuggestionTextBox](INISystem.md#xnasuggestiontextbox) |
| `XNATextBox` | `XNAControl` | `Rampastring.XNAUI/XNAControls/XNATextBox.cs` | Text input box | [XNATextBox](INISystem.md#xnatextbox) |
| `XNATextBlock` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNATextBlock.cs` | Text block (panel with text) | [XNATextBlock](INISystem.md#xnatextblock) |
| `XNATrackbar` | `XNAPanel` | `Rampastring.XNAUI/XNAControls/XNATrackbar.cs` | Trackbar | [XNATrackbar](INISystem.md#xnatrackbar) |
| `XNAChatTextBox` | `XNASuggestionTextBox` | `ClientGUI/XNAChatTextBox.cs` | Chat input box (history, commands) | - |
| `ChatListBox` | `XNAListBox` | `DXMainClient/DXGUI/Multiplayer/ChatListBox.cs` | Chat message list | - |
| `INItializableWindow` | `XNAPanel` | `ClientGUI/INItializableWindow.cs` | Window or panel whose children are defined by INI (`$CC` / `$ExtraControls`) | [Creating Controls From INI](INISystem.md#creating-controls-from-ini) |
| `GameLobbyCheckBox` | `GameSessionCheckBox` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyCheckBox.cs` | Game lobby option checkbox; broadcasts options and writes spawn.ini | [GameLobbyCheckBox](INISystem.md#gamelobbycheckbox) |
| `GameLobbyDropDown` | `GameSessionDropDown` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyDropDown.cs` | Game lobby option dropdown | [GameLobbyDropDown](INISystem.md#gamelobbydropdown) |
| `CampaignCheckBox` | `GameSessionCheckBox` | `DXMainClient/DXGUI/Campaign/CampaignCheckBox.cs` | Campaign option checkbox | [CampaignCheckBox](INISystem.md#campaigncheckbox) |
| `CampaignDropDown` | `GameSessionDropDown` | `DXMainClient/DXGUI/Campaign/CampaignDropDown.cs` | Campaign option dropdown | [CampaignDropDown](INISystem.md#campaigndropdown) |
| `SettingCheckBox` | `SettingCheckBoxBase` | `ClientGUI/Settings/SettingCheckBox.cs` | Checkbox bound to a client setting | [SettingCheckBox](INISystem.md#settingcheckbox) |
| `SettingDropDown` | `SettingDropDownBase` | `ClientGUI/Settings/SettingDropDown.cs` | Dropdown bound to a client setting | [SettingDropDown](INISystem.md#settingdropdown) |
| `FileSettingCheckBox` | `SettingCheckBoxBase` | `ClientGUI/Settings/FileSettingCheckBox.cs` | File-based setting checkbox (file selection/validation) | [FileSettingCheckBox](INISystem.md#filesettingcheckbox) |
| `FileSettingDropDown` | `SettingDropDownBase` | `ClientGUI/Settings/FileSettingDropDown.cs` | File-based setting dropdown | [FileSettingDropDown](INISystem.md#filesettingdropdown) |

## Singleton Controls

These types are also registered as singletons in the dependency injection container, so code that injects them shares one instance. Suitable for windows, lobbies and the main menu.

> [!WARNING]
> When a singleton type is created from INI by name, `ClientGUICreator` still invokes its constructor and returns a new instance instead of the global DI singleton. Do not re-create these windows or lobbies from INI with `$CC` or extra controls; obtain them through dependency injection in code.

| Registered name | Base class | Source | Purpose | Docs |
|---|---|---|---|---|
| `LoadingScreen` | `XNAWindow` | `DXMainClient/DXGUI/Generic/LoadingScreen.cs` | Startup loading screen window | [LoadingScreen](INISystem.md#loadingscreen) |
| `TopBar` | `XNAPanel` | `DXMainClient/DXGUI/Generic/TopBar.cs` | Main menu top bar (version, announcements) | - |
| `OptionsWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/OptionsWindow.cs` | Options window (display, audio, game, CnCNet, storage, updater, components tabs) | - |
| `PrivateMessagingWindow` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/CnCNet/PrivateMessagingWindow.cs` | Private messaging window | - |
| `PrivateMessagingPanel` | `DarkeningPanel` | `DXMainClient/DXGUI/Multiplayer/CnCNet/PrivateMessagingPanel.cs` | Darkening host panel for private messaging | - |
| `LANLobby` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/LANLobby.cs` | LAN lobby window | - |
| `CnCNetGameLobby` | `MultiplayerGameLobby` | `DXMainClient/DXGUI/Multiplayer/GameLobby/CnCNetGameLobby.cs` | CnCNet multiplayer game lobby | [GameLobbyBase Controls](INISystem.md) |
| `CnCNetGameLoadingLobby` | `GameLoadingLobbyBase` | `DXMainClient/DXGUI/Multiplayer/CnCNet/CnCNetGameLoadingLobby.cs` | CnCNet game loading/save lobby | - |
| `CnCNetLobby` | `XNAWindow` | `DXMainClient/DXGUI/Multiplayer/CnCNet/CnCNetLobby.cs` | CnCNet room list lobby | - |
| `GameInProgressWindow` | `XNAPanel` | `DXMainClient/DXGUI/Generic/GameInProgressWindow.cs` | "Game in progress" window | - |
| `SkirmishLobby` | `GameLobbyBase` | `DXMainClient/DXGUI/Multiplayer/GameLobby/SkirmishLobby.cs` | Skirmish lobby | [GameLobbyBase Controls](INISystem.md) |
| `MainMenu` | `XNAWindow` | `DXMainClient/DXGUI/Generic/MainMenu.cs` | Main menu window | [MainMenu](INISystem.md#mainmenu) |
| `MapPreviewBox` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/GameLobby/MapPreviewBox.cs` | Game lobby map preview | [MapPreviewBox](INISystem.md#mappreviewbox) |
| `GameLaunchButton` | `XNAClientButton` | `DXMainClient/DXGUI/Multiplayer/GameLobby/GameLaunchButton.cs` | Lobby "Launch Game" button | - |
| `PlayerExtraOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerExtraOptionsPanel.cs` | Player extra options (forced random side/team/start mapping and more) | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `PlayerAIQuickOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerAIQuickOptionsPanel.cs` | Player AI quick options (side, difficulty, color) | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `PlayerNameOptionsPanel` | `XNAPanel` | `DXMainClient/DXGUI/Multiplayer/PlayerNameOptionsPanel.cs` | Player name options panel | [GameOptions.ini](INISystem.md#gameoptionsini) |
| `CampaignTagSelector` | `INItializableWindow` | `DXMainClient/DXGUI/Campaign/CampaignTagSelector.cs` | Campaign tag selector window | [CampaignSelector](INISystem.md#campaignselector) |
| `GameLoadingWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/GameLoadingWindow.cs` | Load game / replays window | - |
| `StatisticsWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/StatisticsWindow.cs` | Match statistics window | - |
| `UpdateQueryWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/UpdateQueryWindow.cs` | Update query window | - |
| `ManualUpdateQueryWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/ManualUpdateQueryWindow.cs` | Manual update query window | - |
| `UpdateWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/UpdateWindow.cs` | Update progress window | - |
| `ExtrasWindow` | `XNAWindow` | `DXMainClient/DXGUI/Generic/ExtrasWindow.cs` | Extras window | - |

## Documented but Not Registered

The following types have property sections in [INISystem.md](INISystem.md) but are **not** registered in `GameClass.cs`, so they cannot be created by name from INI (`$CC` or extra controls). They are created from code only:

| Type | Reason / usage |
|---|---|
| `XNAIndicator` | Indicator control used from code |
| `XNAContextMenu` | Context menu created from code |
| `XNAClientToggleButton` | Toggle button used from code |
| `XNAClientColorDropDown` | Color dropdown created from code (player colors and similar) |
| `XNAClientPreferredItemDropDown` | Preferred-item dropdown used from code |
| `XNAOptionsPanel` | Options panel base class created from code |
| `GameSessionCheckBox` / `GameSessionDropDown` | Code base classes; use the registered derived types such as `GameLobbyCheckBox` and `CampaignCheckBox` in INI |
| `CoopBriefingBox` | Created by map preview/lobby code |
| `CampaignSelector` | Window created from code that reads its own INI |
| `XNAPasswordBox` / `XNATimerControl` | Used from code |
| `XNAWindow` / `XNAWindowBase` / `DarkeningPanel` | Window and panel base classes, not registered as creatable controls |

## Registering a New Control

To make a new control creatable from INI:

1. Create a class deriving from `XNAControl` or one of its subclasses. The class name becomes the registered name and must not collide with existing ones.
2. Register it in `BuildServiceProvider` in `DXMainClient/DXGUI/GameClass.cs`:
   - `.AddSingletonXnaControl<YourControl>()` for globally unique objects (windows, lobbies);
   - `.AddTransientXnaControl<YourControl>()` for ordinary controls.
3. Override `ParseControlINIAttribute` in the control to parse custom INI keys.
4. Use `YourControl` as the type name in INI, for example `$CC00=myControl:YourControl`.

> The registered name is case-sensitive and must match the C# type name (`Type.Name`) exactly; a typo in the INI throws an exception.
