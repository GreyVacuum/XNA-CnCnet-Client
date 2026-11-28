# Window and Panel Control Reference

This manual lists the controls defined by each window and panel in source, for looking up control purpose and rewriting INI layouts.

## How to read

- The control name is the INI section name; look it up or add it in the corresponding window INI.
- "Type" is the control class used by source; the last column shows creation: `Code` means created with `new`, `FindChild` means looked up from INI by name.
- Controls inherited from a base class are not repeated; see the base class section.
- Only controls created by source or referenced through `FindChild` are listed; `$CC` / extra controls declared in the default theme INI are MOD-defined and are not included.
- INI file names are inferred from the class name / window `Name`; themes or mods may override them.

## Rewriting the layout

Keep the section name and change only the relevant keys:

- Position and size: `Location`, `Size`, `X`, `Y`, `Width`, `Height`
- Expression layout: `$X`, `$Y`, `$Width`, `$Height` (order-sensitive)
- Background and borders: `BackgroundTexture`, `SolidColorBackgroundTexture`, `DrawBorders`
- Code-created controls are configured through same-named sections; INI-created controls require their `$CC` or extra-control declaration.

## Contents

**General Windows and Panels**

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

**Multiplayer and Lobbies**

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

**Campaign**

- [CampaignSelector](#campaignselector)
- [CampaignTagSelector](#campaigntagselector)
- [CheaterWindow](#cheaterwindow)

**Options Window Panels**

- [AudioOptionsPanel](#audiooptionspanel)
- [CnCNetOptionsPanel](#cncnetoptionspanel)
- [ComponentsPanel](#componentspanel)
- [DisplayOptionsPanel](#displayoptionspanel)
- [GameOptionsPanel](#gameoptionspanel)
- [StorageOptionsPanel](#storageoptionspanel)
- [StorageScrollPanel](#storagescrollpanel)
- [UpdaterOptionsPanel](#updateroptionspanel)

## General Windows and Panels

### ExtrasWindow

Extras window.

- Source: `DXMainClient\DXGUI\Generic\ExtrasWindow.cs` | Base: `XNAWindow` | INI: `ExtrasWindow.ini` | Controls: 4

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnExCancel` | Back | `XNAClientButton` | Code |
| `btnExCredits` | Credits | `XNAClientButton` | Code |
| `btnExMapEditor` | Map editor | `XNAClientButton` | Code |
| `btnExStatistics` | Statistics | `XNAClientButton` | Code |

### GameInProgressWindow

- Source: `DXMainClient\DXGUI\Generic\GameInProgressWindow.cs` | Base: `XNAPanel` | INI: `GameInProgressWindow.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### GameLoadingWindow

Load game and replays window.

- Source: `DXMainClient\DXGUI\Generic\GameLoadingWindow.cs` | Base: `XNAWindow` | INI: `GameLoadingWindow.ini` | Controls: 5

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnDelete` | Delete | `XNAClientButton` | Code |
| `btnLaunch` | Launch game/mission | `XNAClientButton` | Code |
| `lbSaveGameList` | Save Game List | `XNAMultiColumnListBox` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |

### HotkeyConfigurationWindow

Hotkey configuration window.

- Source: `ClientGUI\HotkeyConfigurationWindow.cs` | Base: `XNAWindow` | INI: `HotkeyConfigurationWindow.ini` | Controls: 18

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnAssign` | Assign hotkey | `XNAClientButton` | Code |
| `btnExit` | Exit | `XNAClientButton` | Code |
| `btnResetAllToDefaults` | Reset all hotkeys | `XNAClientButton` | Code |
| `btnResetKey` | Reset current hotkey | `XNAClientButton` | Code |
| `btnSave` | Save | `XNAClientButton` | Code |
| `ddCategory` | Category | `XNAClientDropDown` | Code |
| `HotkeyInfoPanel` | Hotkey Info Panel | `XNAPanel` | Code |
| `lbHotkeys` | Hotkeys | `XNAMultiColumnListBox` | Code |
| `lblCategory` | Category: | `XNALabel` | Code |
| `lblCommandCaption` | Command name | `XNALabel` | Code |
| `lblCurrentHotkey` | Currently assigned hotkey: | `XNALabel` | Code |
| `lblCurrentHotkeyValue` | Current hotkey value | `XNALabel` | Code |
| `lblCurrentlyAssignedTo` | Currently assigned to: | `XNALabel` | Code |
| `lblDefaultHotkeyValue` | Default Hotkey Value | `XNALabel` | Code |
| `lblDescription` | Command description | `XNALabel` | Code |
| `lblNewHotkey` | New hotkey: | `XNALabel` | Code |
| `lblNewHotkeyValue` | New Hotkey Value | `XNALabel` | Code |
| `lblOriginalHotkey` | Default hotkey: | `XNALabel` | Code |

### LoadingScreen

Startup loading screen.

- Source: `DXMainClient\DXGUI\Generic\LoadingScreen.cs` | Base: `XNAWindow` | INI: `LoadingScreen.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### MainMenu

Main menu window with campaign, load, skirmish, CnCNet and LAN entries.

- Source: `DXMainClient\DXGUI\Generic\MainMenu.cs` | Base: `XNAWindow, ISwitchable` | INI: `MainMenu.ini` | Controls: 15

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCnCNet` | Play online | `XNAClientButton` | Code |
| `btnCredits` | Credits | `XNAClientButton` | Code |
| `btnExit` | Exit | `XNAClientButton` | Code |
| `btnExtras` | Extras | `XNAClientButton` | Code |
| `btnLan` | LAN | `XNAClientButton` | Code |
| `btnLoadGame` | Load game | `XNAClientButton` | Code |
| `btnMapEditor` | Map editor | `XNAClientButton` | Code |
| `btnNewCampaign` | Campaign | `XNAClientButton` | Code |
| `btnOptions` | Options | `XNAClientButton` | Code |
| `btnSkirmish` | Skirmish | `XNAClientButton` | Code |
| `btnStatistics` | Statistics | `XNAClientButton` | Code |
| `lblCnCNetPlayerCount` | CnCNet player count | `XNALabel` | Code |
| `lblCnCNetStatus` | CnCNet status text | `XNALabel` | Code |
| `lblUpdateStatus` | Update status | `XNALinkLabel` | Code |
| `lblVersion` | Client version | `XNALinkLabel` | Code |

### ManualUpdateQueryWindow

Manual update query window.

- Source: `DXMainClient\DXGUI\Generic\ManualUpdateQueryWindow.cs` | Base: `XNAWindow` | INI: `ManualUpdateQueryWindow.ini` | Controls: 3

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `btnDownload` | View Downloads | `XNAClientButton` | Code |
| `lblDescription` | Description | `XNALabel` | Code |

### OptionsWindow

Options window with display, audio, game, CnCNet, storage, updater and components tabs.

- Source: `DXMainClient\DXGUI\Generic\OptionsWindow.cs` | Base: `XNAWindow` | INI: `OptionsWindow.ini` | Controls: 3

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnSave` | Save | `XNAClientButton` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |

### PrivacyNotification

- Source: `DXMainClient\DXGUI\Generic\PrivacyNotification.cs` | Base: `XNAWindow` | INI: `PrivacyNotification.ini` | Controls: 6

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnOK` | Got it | `XNAClientButton` | Code |
| `lblDescription` | Description | `XNALabel` | Code |
| `lblExplanation` | By using this application you agree to the CnCNet Terms & Conditions a... | `XNALabel` | Code |
| `lblMoreInformation` | More information: | `XNALabel` | Code |
| `lblPrivacyPolicy` | https://cncnet.org/privacy-policy | `XNALinkLabel` | Code |
| `lblTermsAndConditions` | https://cncnet.org/terms-and-conditions | `XNALinkLabel` | Code |

### ReplaysPanel

- Source: `DXMainClient\DXGUI\Generic\LoadGamePanels\ReplaysPanel.cs` | Base: `XNAPanel` | INI: `ReplaysPanel.ini` | Controls: 11

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `ddGameSpeed` | Replay playback speed | `XNAClientDropDown` | Code |
| `ddSeekCheckpoints` | Replay seek checkpoints | `XNAClientDropDown` | Code |
| `ddWatchAs` | Watch-as player | `XNAClientDropDown` | Code |
| `lblGameSpeed` | Speed: | `XNALabel` | Code |
| `lblPlayback` | Playback: | `XNALabel` | Code |
| `lblReleases` | Releases | `XNALinkLabel` | Code |
| `lblSeekCheckpoints` | Seek checkpoints: | `XNALabel` | Code |
| `lblWatchAs` | Watch as: | `XNALabel` | Code |
| `lbReplayList` | Replay List | `XNAMultiColumnListBox` | Code |
| `replayContextMenu` | Replay context menu | `XNAContextMenu` | Code |
| `tbDetails` | Details text | `XNATextBlock` | Code |

### StatisticsWindow

Match statistics window.

- Source: `DXMainClient\DXGUI\Generic\StatisticsWindow.cs` | Base: `XNAWindow` | INI: `StatisticsWindow.ini` | Controls: 31

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClearStatistics` | Clear Statistics | `XNAClientButton` | Code |
| `btnReturnToMenu` | Return to main menu | `XNAClientButton` | Code |
| `chkIncludeSpectatedGames` | Include spectated games | `XNAClientCheckBox` | Code |
| `cmbGameClassFilter` | Cmb Game Class Filter | `XNAClientDropDown` | Code |
| `cmbGameModeFilter` | Cmb Game Mode Filter | `XNAClientDropDown` | Code |
| `lbGameList` | Game room list | `XNAMultiColumnListBox` | Code |
| `lbGameStatistics` | Game Statistics | `XNAMultiColumnListBox` | Code |
| `lblAverageAILevelValue` | Hard | `XNALabel` | Code |
| `lblAverageAllyCountValue` | Average Ally Count Value | `XNALabel` | Code |
| `lblAverageEconomyValue` | Average Economy Value | `XNALabel` | Code |
| `lblAverageEnemyCountValue` | Average Enemy Count Value | `XNALabel` | Code |
| `lblAverageGameLengthValue` | Average Game Length Value | `XNALabel` | Code |
| `lblFavouriteSideValue` | Favourite Side Value | `XNALabel` | Code |
| `lblFilter` | FILTER: | `XNALabel` | Code |
| `lblGameMode` | Game mode | `XNALabel` | Code |
| `lblGames` | GAMES: | `XNALabel` | Code |
| `lblGamesFinishedValue` | Games Finished Value | `XNALabel` | Code |
| `lblGamesStartedValue` | Games Started Value | `XNALabel` | Code |
| `lblKillLossRatioValue` | Kill Loss Ratio Value | `XNALabel` | Code |
| `lblKillsPerGameValue` | Kills Per Game Value | `XNALabel` | Code |
| `lblLossesPerGameValue` | Losses Per Game Value | `XNALabel` | Code |
| `lblLossesValue` | Losses Value | `XNALabel` | Code |
| `lblTotalKillsValue` | Total Kills Value | `XNALabel` | Code |
| `lblTotalLossesValue` | Total Losses Value | `XNALabel` | Code |
| `lblTotalScoreValue` | Total Score Value | `XNALabel` | Code |
| `lblTotalTimePlayedValue` | Total Time Played Value | `XNALabel` | Code |
| `lblWinLossRatioValue` | Win Loss Ratio Value | `XNALabel` | Code |
| `lblWinsValue` | Wins Value | `XNALabel` | Code |
| `panelGameStatistics` | Panel Game Statistics | `XNAPanel` | Code |
| `panelTotalStatistics` | Panel Total Statistics | `XNAPanel` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |

### TopBar

- Source: `DXMainClient\DXGUI\Generic\TopBar.cs` | Base: `XNAPanel` | INI: `TopBar.ini` | Controls: 10

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCnCNetLobby` | CnCNet Lobby (F3) | `XNAClientButton` | Code |
| `btnLogout` | Log out | `XNAClientButton` | Code |
| `btnMainButton` | Main menu button | `XNAClientButton` | Code |
| `btnOptions` | Options | `XNAClientButton` | Code |
| `btnPrivateMessages` | Open private messages | `XNAClientButton` | Code |
| `lblCnCNetPlayerCount` | CnCNet player count | `XNALabel` | Code |
| `lblCnCNetStatus` | CnCNet status text | `XNALabel` | Code |
| `lblConnectionStatus` | OFFLINE | `XNALabel` | Code |
| `lblDate` | Date | `XNALabel` | Code |
| `lblTime` | Time | `XNALabel` | Code |

### UpdateQueryWindow

Update query window.

- Source: `DXMainClient\DXGUI\Generic\UpdateQueryWindow.cs` | Base: `XNAWindow` | INI: `UpdateQueryWindow.ini` | Controls: 5

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnNo` | No | `XNAClientButton` | Code |
| `btnYes` | Yes | `XNAClientButton` | Code |
| `lblChangelogLink` | View Changelog | `XNALinkLabel` | Code |
| `lblDescription` | Description | `XNALabel` | Code |
| `lblUpdateSize` | Update Size | `XNALabel` | Code |

### UpdateWindow

Update progress window.

- Source: `DXMainClient\DXGUI\Generic\UpdateWindow.cs` | Base: `XNAWindow` | INI: `UpdateWindow.ini` | Controls: 9

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `lblCurrentFile` | Current file: | `XNALabel` | Code |
| `lblCurrentFileProgressPercentage` | Progress percentage of current file: | `XNALabel` | Code |
| `lblCurrentFileProgressPercentageValue` | 0% | `XNALabel` | Code |
| `lblDescription` | Description | `XNALabel` | Code |
| `lblTotalProgressPercentage` | Total progress percentage: | `XNALabel` | Code |
| `lblTotalProgressPercentageValue` | 0% | `XNALabel` | Code |
| `lblUpdaterStatus` | Connecting | `XNALabel` | Code |
| `prgCurrentFile` | Current File | `XNAProgressBar` | Code |
| `prgTotal` | Total progress bar | `XNAProgressBar` | Code |

### XNAClientScrollPanel

- Source: `ClientGUI\XNAClientScrollPanel.cs` | Base: `XNAScrollPanel, ICompositeControl` | INI: `XNAClientScrollPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### XNAMessageBox

Generic message box.

- Source: `ClientGUI\XNAMessageBox.cs` | Base: `XNAWindow` | INI: `XNAMessageBox.ini` | Controls: 4

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAButton` | Code |
| `btnNo` | No | `XNAButton` | Code |
| `btnOK` | OK | `XNAButton` | Code |
| `btnYes` | Yes | `XNAButton` | Code |

## Multiplayer and Lobbies

### ChoiceNotificationBox

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\ChoiceNotificationBox.cs` | Base: `XNAPanel` | INI: `ChoiceNotificationBox.ini` | Controls: 6

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `affirmativeButton` | Affirmative button | `XNAClientButton` | Code |
| `gameIconPanel` | Game icon panel | `XNAPanel` | Code |
| `lblChoiceText` | What do you want to do? | `XNALabel` | Code |
| `lblHeader` | MAKE A CHOICE | `XNALabel` | Code |
| `lblSender` | fonger | `XNALabel` | Code |
| `negativeButton` | Negative button | `XNAClientButton` | Code |

### CnCNetGameLoadingLobby

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetGameLoadingLobby.cs` | Base: `GameLoadingLobbyBase, IV3NegotiationHost` | INI: `CnCNetGameLoadingLobby.ini` | Controls: 1

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnChangeTunnel` | Change tunnel | `XNAClientButton` | Code |

### CnCNetGameLobby

CnCNet lobby tunnel and room settings entries.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\CnCNetGameLobby.cs` | Base: `MultiplayerGameLobby, IV3NegotiationHost` | INI: `CnCNetGameLobby.ini` | Controls: 4

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `_negotiationStatusPanel` | Negotiation status panel | `TunnelNegotiationStatusPanel` | Code |
| `btnChangeTunnel` | Change tunnel | `XNAClientButton` | FindChild |
| `btnGameLobbySettings` | Game lobby settings | `XNAClientButton` | FindChild |
| `btnNegotiationStatus` | Negotiation status | `XNAClientButton` | FindChild |

### CnCNetLobby

CnCNet room-list lobby.

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetLobby.cs` | Base: `XNAWindow, ISwitchable` | INI: `CnCNetLobby.ini` | Controls: 17

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnGameFilterOptions` | Game Filter Options | `XNAClientToggleButton` | Code |
| `btnGameSortAlpha` | Sort games alphabetically | `XNAClientStateButton<SortDirection>` | Code |
| `btnJoinGame` | Join game | `XNAClientButton` | Code |
| `btnLogout` | Log out | `XNAClientButton` | Code |
| `btnNewGame` | Create game | `XNAClientButton` | Code |
| `ddColor` | Chat color selector | `XNAClientDropDown` | Code |
| `ddCurrentChannel` | Channel selector | `XNAClientDropDown` | Code |
| `lbChatMessages` | Chat history | `ChatListBox` | Code |
| `lbGameList` | Game room list | `GameListBox` | Code |
| `lblColor` | "Your color" label | `XNALabel` | Code |
| `lblCurrentChannel` | Current channel label | `XNALabel` | Code |
| `lblOnline` | Online: | `XNALabel` | Code |
| `lblOnlineCount` | Online Count | `XNALabel` | Code |
| `lbPlayerList` | Player list | `PlayerListBox` | Code |
| `panelGameFilters` | Panel Game Filters | `GameFiltersPanel` | Code |
| `tbChatInput` | Chat input | `XNAChatTextBox` | Code |
| `tbGameSearch` | Game search | `XNASuggestionTextBox` | Code |

### CnCNetLoginWindow

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\CnCNetLoginWindow.cs` | Base: `XNAWindow` | INI: `CnCNetLoginWindow.ini` | Controls: 8

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnConnect` | Connect | `XNAClientButton` | Code |
| `chkAutoConnect` | Connect automatically on client startup | `XNAClientCheckBox` | Code |
| `chkPersistentMode` | Stay connected outside of the CnCNet lobby | `XNAClientCheckBox` | Code |
| `chkRememberMe` | Remember me | `XNAClientCheckBox` | Code |
| `lblConnectToCnCNet` | CONNECT TO CNCNET | `XNALabel` | Code |
| `lblPlayerName` | PLAYER NAME: | `XNALabel` | Code |
| `tbPlayerName` | Player Name | `XNATextBox` | Code |

### CoopBriefingBox

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\CoopBriefingBox.cs` | Base: `XNAPanel` | INI: `CoopBriefingBox.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### GameCreationWindow

Create multiplayer game window with name, password, max players and tunnel.

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\GameCreationWindow.cs` | Base: `XNAWindow` | INI: `GameCreationWindow.ini` | Controls: 18

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnAutoSelectTunnel` | Auto Select | `XNAClientButton` | Code |
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnCreateGame` | Create Game | `XNAClientButton` | Code |
| `btnDisplayAdvancedOptions` | Advanced Options | `XNAClientButton` | Code |
| `btnLoadMPGame` | Load Game | `XNAClientButton` | Code |
| `btnSaveDefaultTunnel` | Save as Default | `XNAClientButton` | Code |
| `ddMaxPlayers` | Max Players | `XNAClientDropDown` | Code |
| `ddSkillLevel` | Skill Level | `XNAClientDropDown` | Code |
| `ddTunnelMode` | Tunnel Mode | `XNADropDown` | Code |
| `lblMaxPlayers` | Maximum number of players: | `XNALabel` | Code |
| `lblPassword` | Password (leave blank for none): | `XNALabel` | Code |
| `lblRoomName` | Game room name: | `XNALabel` | Code |
| `lblSkillLevel` | Select preferred skill level of players: | `XNALabel` | Code |
| `lblTunnelServer` | Tunnel mode: | `XNALabel` | Code |
| `lbTunnelList` | Tunnel List | `TunnelListBox` | Code |
| `pnlTunnelListDisabledOverlay` | Tunnel List Disabled Overlay | `XNAPanel` | Code |
| `tbGameName` | Game Name | `XNATextBox` | Code |
| `tbPassword` | Password | `XNATextBox` | Code |

### GameFiltersPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameFiltersPanel.cs` | Base: `XNAPanel` | INI: `GameFiltersPanel.ini` | Controls: 12

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `BottomButtonPanel` | Bottom button panel | `XNAPanel` | Code |
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnResetDefaults` | Reset Defaults | `XNAClientButton` | Code |
| `btnSave` | Save | `XNAClientButton` | Code |
| `chkBoxFriendsOnly` | Friends only | `XNAClientCheckBox` | Code |
| `chkBoxHideIncompatibleGames` | Hide incompatible games | `XNAClientCheckBox` | Code |
| `chkBoxHideLockedGames` | Hide locked games | `XNAClientCheckBox` | Code |
| `chkBoxHidePasswordedGames` | Hide Passworded Games | `XNAClientCheckBox` | Code |
| `ddMaxPlayerCount` | Max Player Count | `XNAClientDropDown` | Code |
| `FiltersScrollPanel` | Filters Scroll Panel | `GameFiltersScrollPanel` | Code |
| `lblMaxPlayerCount` | Max Player Count | `XNALabel` | Code |
| `lblTitle` | Game Filters | `XNALabel` | Code |

### GameFiltersScrollPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameFiltersPanel.cs` | Base: `XNAScrollPanel` | INI: `GameFiltersScrollPanel.ini` | Controls: 12

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `BottomButtonPanel` | Bottom button panel | `XNAPanel` | Code |
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnResetDefaults` | Reset Defaults | `XNAClientButton` | Code |
| `btnSave` | Save | `XNAClientButton` | Code |
| `chkBoxFriendsOnly` | Friends only | `XNAClientCheckBox` | Code |
| `chkBoxHideIncompatibleGames` | Hide incompatible games | `XNAClientCheckBox` | Code |
| `chkBoxHideLockedGames` | Hide locked games | `XNAClientCheckBox` | Code |
| `chkBoxHidePasswordedGames` | Hide Passworded Games | `XNAClientCheckBox` | Code |
| `ddMaxPlayerCount` | Max Player Count | `XNAClientDropDown` | Code |
| `FiltersScrollPanel` | Filters Scroll Panel | `GameFiltersScrollPanel` | Code |
| `lblMaxPlayerCount` | Max Player Count | `XNALabel` | Code |
| `lblTitle` | Game Filters | `XNALabel` | Code |

### GameInformationIconOnlyPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameInformationIconOnlyPanel.cs` | Base: `XNAPanel` | INI: `GameInformationIconOnlyPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### GameInformationIconPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameInformationIconPanel.cs` | Base: `XNAPanel` | INI: `GameInformationIconPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### GameInformationPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameInformationPanel.cs` | Base: `XNAPanel` | INI: `GameInformationPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### GameLoadingLobbyBase

Shared multiplayer save-game loading lobby layout.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLoadingLobbyBase.cs` | Base: `XNAWindow, ISwitchable` | INI: `GameLoadingLobbyBase.ini` | Controls: 12

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnLeaveGame` | Return to main menu | `XNAClientButton` | Code |
| `btnLoadGame` | Load game | `XNAClientButton` | Code |
| `ddSavedGame` | Saved Game | `XNAClientDropDown` | Code |
| `lbChatMessages` | Chat history | `ChatListBox` | Code |
| `lblDescription` | Wait for all players to join and get ready, then click Load Game to lo... | `XNALabel` | Code |
| `lblGameMode` | Game mode | `XNALabel` | Code |
| `lblGameModeValue` | Game mode | `XNALabel` | Code |
| `lblMapName` | Map name | `XNALabel` | Code |
| `lblMapNameValue` | Map name | `XNALabel` | Code |
| `lblPlayerName` | Player Name | `XNALabel` | Code |
| `lblSavedGameTime` | SAVED GAME: | `XNALabel` | Code |
| `tbChatInput` | Chat input | `XNATextBox` | Code |

### GameLobbyBase

Shared game lobby layout: map list, preview, player options and game options.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\GameLobbyBase.cs` | Base: `INItializableWindow` | INI: `GameLobbyBase.ini` | Controls: 31

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnLaunchGame` | Launch game | `GameLaunchButton` | FindChild |
| `btnLeaveGame` | Return to main menu | `XNAClientButton` | FindChild |
| `btnMapSortAlphabetically` | Sort maps alphabetically | `XNAClientStateButton<SortDirection>` | Code |
| `btnPickRandomMap` | Pick random map | `XNAClientButton` | FindChild |
| `btnPlayerAIQuickOptionsOpen` | Open AI quick options | `XNAClientButton` | FindChild |
| `btnPlayerExtraOptionsOpen` | Open player extra options | `XNAClientButton` | FindChild |
| `btnPlayerNameOptionsOpen` | Open name options | `XNAClientButton` | FindChild |
| `btnSaveLoadGameOptions` | Save/load option preset | `XNAClientButton` | FindChild |
| `ddDifficulty` | Difficulty (optional) | `XNAClientDropDown` | FindChild |
| `ddGameMode` | Game mode filter | `XNAClientDropDown` | FindChild |
| `ddPlayerColor` | Color | `XNAClientColorDropDown` | Code |
| `ddPlayerName` | Player name | `XNAClientDropDown` | Code |
| `ddPlayerSide` | Side | `XNAClientDropDown` | Code |
| `ddPlayerStart` | Starting location | `XNAClientDropDown` | Code |
| `ddPlayerTeam` | Team | `XNAClientDropDown` | Code |
| `lblDifficulty` | Difficulty label | `XNALabel` | FindChild |
| `lblGameMode` | Game mode | `XNALabel` | FindChild |
| `lblGameModeSelect` | Game mode filter heading | `XNALabel` | FindChild |
| `lblMapAuthor` | Map author | `XNALabel` | FindChild |
| `lblMapName` | Map name | `XNALabel` | FindChild |
| `lblMapSize` | Map size | `XNALabel` | FindChild |
| `lbMapList` | Map list | `XNAMultiColumnListBox` | FindChild |
| `loadSaveGameOptionsMenu` | Option preset menu | `XNAContextMenu` | Code |
| `mapContextMenu` | Map list context menu | `XNAContextMenu` | Code |
| `MapPreviewBox` | Map preview | `MapPreviewBox` | FindChild |
| `PlayerAIQuickOptionsPanel` | Player AI Quick Options Panel | `PlayerAIQuickOptionsPanel` | FindChild |
| `PlayerExtraOptionsPanel` | Player Extra Options Panel | `PlayerExtraOptionsPanel` | FindChild |
| `PlayerNameOptionsPanel` | Player Name Options Panel | `PlayerNameOptionsPanel` | Code |
| `PlayerOptionsPanel` | Player options container | `XNAPanel` | FindChild |
| `searchContextMenu` | Search context menu | `XNAContextMenu` | Code |
| `tbMapSearch` | Map search | `XNASuggestionTextBox` | FindChild |

### LANGameCreationWindow

- Source: `DXMainClient\DXGUI\Multiplayer\LANGameCreationWindow.cs` | Base: `XNAWindow` | INI: `LANGameCreationWindow.ini` | Controls: 4

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAButton` | Code |
| `btnLoadGame` | Load game | `XNAButton` | Code |
| `btnNewGame` | Create game | `XNAButton` | Code |
| `lblDescription` | SELECT SESSION TYPE | `XNALabel` | Code |

### LANGameLoadingLobby

- Source: `DXMainClient\DXGUI\Multiplayer\LANGameLoadingLobby.cs` | Base: `GameLoadingLobbyBase` | INI: `LANGameLoadingLobby.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### LANGameLobby

LAN lobby; layout inherited from MultiplayerGameLobby.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\LANGameLobby.cs` | Base: `MultiplayerGameLobby` | INI: `LANGameLobby.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### LANLobby

Shared lobby layout used by CnCNetLobby; controls are created by CnCNetLobby.

- Source: `DXMainClient\DXGUI\Multiplayer\LANLobby.cs` | Base: `XNAWindow` | INI: `LANLobby.ini` | Controls: 9

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnJoinGame` | Join game | `XNAClientButton` | Code |
| `btnMainMenu` | Return to main menu | `XNAClientButton` | Code |
| `btnNewGame` | Create game | `XNAClientButton` | Code |
| `ddColor` | Chat color selector | `XNAClientDropDown` | Code |
| `lbChatMessages` | Chat history | `ChatListBox` | Code |
| `lbGameList` | Game room list | `GameListBox` | Code |
| `lblColor` | "Your color" label | `XNALabel` | Code |
| `lbPlayerList` | Player list | `XNAListBox` | Code |
| `tbChatInput` | Chat input | `XNAChatTextBox` | Code |

### LoadOrSaveGameOptionPresetWindow

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\LoadOrSaveGameOptionPresetWindow.cs` | Base: `XNAWindow` | INI: `LoadOrSaveGameOptionPresetWindow.ini` | Controls: 7

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnDelete` | Delete | `XNAClientButton` | Code |
| `btnLoadSave` | Load Save | `XNAClientButton` | Code |
| `ddPresetSelect` | Preset Select | `XNAClientDropDown` | Code |
| `lblHeader` | Header | `XNALabel` | Code |
| `lblNewPresetName` | New Preset Name | `XNALabel` | Code |
| `lblPresetName` | Preset Name | `XNALabel` | Code |
| `tbNewPresetName` | New Preset Name | `XNATextBox` | Code |

### MapPreviewBox

Lobby map preview.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\MapPreviewBox.cs` | Base: `XNAPanel, ICompositeControl` | INI: `MapPreviewBox.ini` | Controls: 1

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `mainContextMenu` | Main context menu | `XNAContextMenu` | Code |

### MapSharingConfirmationPanel

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\MapSharingConfirmationPanel.cs` | Base: `XNAPanel` | INI: `MapSharingConfirmationPanel.ini` | Controls: 2

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnDownload` | Download | `XNAClientButton` | Code |
| `lblDescription` | Description | `XNALabel` | Code |

### MultiplayerGameLobby

Multiplayer lobby chat, locking and auto-ready controls.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\MultiplayerGameLobby.cs` | Base: `GameLobbyBase, ISwitchable` | INI: `MultiplayerGameLobby.ini` | Controls: 9

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnLockGame` | Lock game | `XNAClientButton` | FindChild |
| `chkAutoReady` | Auto ready | `XNAClientCheckBox` | FindChild |
| `lbChatMessages` | Chat history | `ChatListBox` | FindChild |
| `lbChatMessages_Host` | Chat Messages_Host | `ChatListBox` | Code |
| `lbChatMessages_Player` | Player chat history | `ChatListBox` | Code |
| `playerStatusIndicator` | Player status indicator | `XNAPlayerSlotIndicator` | Code |
| `tbChatInput` | Chat input | `XNAChatTextBox` | FindChild |
| `tbChatInput_Host` | Chat Input_Host | `XNAChatTextBox` | Code |
| `tbChatInput_Player` | Player chat input | `XNAChatTextBox` | Code |

### PasswordRequestWindow

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\PasswordRequestWindow.cs` | Base: `XNAWindow` | INI: `PasswordRequestWindow.ini` | Controls: 4

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnOK` | OK | `XNAClientButton` | Code |
| `lblDescription` | Please enter the password for the game and click OK. | `XNALabel` | Code |
| `tbPassword` | Password | `XNATextBox` | Code |

### PlayerAIQuickOptionsPanel

AI quick options panel.

- Source: `DXMainClient\DXGUI\Multiplayer\PlayerAIQuickOptionsPanel.cs` | Base: `XNAPanel` | INI: `PlayerAIQuickOptionsPanel.ini` | Controls: 9

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `btnResetQuickOptions` | Reset | `XNAClientButton` | Code |
| `chkAutoAssignAIStarts` | Auto Assign Starts | `XNAClientCheckBox` | Code |
| `chkFPPlayer` | First-person player | `XNAClientCheckBox` | Code |
| `lblHeader` | AI Quick Options | `XNALabel` | Code |
| `lblRandomHeader` | Random | `XNALabel` | Code |
| `pnlFormatPainter` | Format painter panel | `XNAPanel` | Code |
| `pnlQuickActions` | Quick actions panel | `XNAPanel` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |

### PlayerExtraOptionsPanel

Player extra options: forced side/color/start and team start mapping.

- Source: `DXMainClient\DXGUI\Multiplayer\PlayerExtraOptionsPanel.cs` | Base: `XNAPanel` | INI: `PlayerExtraOptionsPanel.ini` | Controls: 11

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `btnHelp` | Help | `XNAClientButton` | Code |
| `chkBoxForceNoTeams` | Force No Teams | `XNAClientCheckBox` | Code |
| `chkBoxForceRandomColors` | Force Random Colors | `XNAClientCheckBox` | Code |
| `chkBoxForceRandomSides` | Force Random Sides | `XNAClientCheckBox` | Code |
| `chkBoxForceRandomStarts` | Force Random Starts | `XNAClientCheckBox` | Code |
| `chkBoxUseTeamStartMappings` | Enable Auto Allying: | `XNAClientCheckBox` | Code |
| `ddTeamStartMappingPreset` | Team Start Mapping Preset | `XNAClientDropDown` | Code |
| `lblHeader` | Extra Player Options | `XNALabel` | Code |
| `lblPreset` | Presets: | `XNALabel` | Code |
| `teamStartMappingsPanel` | Team Start Mappings Panel | `TeamStartMappingsPanel` | Code |

### PlayerNameOptionsPanel

Player name options panel.

- Source: `DXMainClient\DXGUI\Multiplayer\PlayerNameOptionsPanel.cs` | Base: `XNAPanel` | INI: `PlayerNameOptionsPanel.ini` | Controls: 12

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `chkAllowCustomNames` | Allow Custom Names | `XNAClientCheckBox` | Code |
| `chkEnableCustomName` | Enable | `XNAClientCheckBox` | Code |
| `lblHeader` | Player Name Options | `XNALabel` | Code |
| `lblLobbyName` | Lobby Name: | `XNALabel` | Code |
| `lblLobbyNameValue` | Lobby Name Value | `XNALabel` | Code |
| `lblNewName` | New Name: | `XNALabel` | Code |
| `lblOtherHeader` | Other Players: | `XNALabel` | Code |
| `playerSlotsScrollPanel` | Player slots scroll panel | `PlayerSlotsScrollPanel` | Code |
| `pnlOtherSlot` | Other player slot panel | `XNAPanel` | Code |
| `tbCustomName` | Custom Name | `XNATextBox` | Code |
| `tbOther` | Other player name input | `XNATextBox` | Code |

### PlayerSlotsScrollPanel

- Source: `DXMainClient\DXGUI\Multiplayer\PlayerNameOptionsPanel.cs` | Base: `XNAScrollPanel` | INI: `PlayerSlotsScrollPanel.ini` | Controls: 11

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `chkAllowCustomNames` | Allow Custom Names | `XNAClientCheckBox` | Code |
| `chkEnableCustomName` | Enable | `XNAClientCheckBox` | Code |
| `lblHeader` | Player Name Options | `XNALabel` | Code |
| `lblLobbyName` | Lobby Name: | `XNALabel` | Code |
| `lblLobbyNameValue` | Lobby Name Value | `XNALabel` | Code |
| `lblNewName` | New Name: | `XNALabel` | Code |
| `lblOtherHeader` | Other Players: | `XNALabel` | Code |
| `pnlOtherSlot` | Other player slot panel | `XNAPanel` | Code |
| `tbCustomName` | Custom Name | `XNATextBox` | Code |
| `tbOther` | Other player name input | `XNATextBox` | Code |

### PrivateMessageNotificationBox

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessageNotificationBox.cs` | Base: `XNAPanel` | INI: `PrivateMessageNotificationBox.ini` | Controls: 6

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `gameIconPanel` | Game icon panel | `XNAPanel` | Code |
| `lblHeader` | PRIVATE MESSAGE | `XNALabel` | Code |
| `lblHint` | Press F4 to respond | `XNALabel` | Code |
| `lblMessage` | This is a test message. | `XNALabel` | Code |
| `lblSender` | Rampastring: | `XNALabel` | Code |
| `linePanel` | Separator line panel | `XNAPanel` | Code |

### PrivateMessagingPanel

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessagingPanel.cs` | Base: `DarkeningPanel` | INI: `PrivateMessagingPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### PrivateMessagingWindow

Private messaging window.

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\PrivateMessagingWindow.cs` | Base: `XNAWindow, ISwitchable` | INI: `PrivateMessagingWindow.ini` | Controls: 7

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `lblMessages` | MESSAGES: | `XNALabel` | Code |
| `lblPlayers` | Players | `XNALabel` | Code |
| `lblPrivateMessaging` | PRIVATE MESSAGING | `XNALabel` | Code |
| `lbMessages` | Message list | `ChatListBox` | Code |
| `lbUserList` | User list | `XNAListBox` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |
| `tbMessageInput` | Private message input | `XNATextBox` | Code |

### SkirmishLobby

Skirmish lobby; layout inherited from GameLobbyBase.

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\SkirmishLobby.cs` | Base: `GameLobbyBase, ISwitchable` | INI: `SkirmishLobby.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### TeamStartMappingPanel

- Source: `DXMainClient\DXGUI\Multiplayer\TeamStartMappingPanel.cs` | Base: `XNAPanel` | INI: `TeamStartMappingPanel.ini` | Controls: 1

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `ddTeams` | Teams | `XNAClientDropDown` | Code |

### TeamStartMappingsPanel

- Source: `DXMainClient\DXGUI\Multiplayer\TeamStartMappingsPanel.cs` | Base: `XNAPanel` | INI: `TeamStartMappingsPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### TunnelNegotiationStatusPanel

- Source: `DXMainClient\DXGUI\Multiplayer\GameLobby\TunnelNegotiationStatusPanel.cs` | Base: `XNAPanel` | INI: `TunnelNegotiationStatusPanel.ini` | Controls: 6

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnClose` | Close | `XNAClientButton` | Code |
| `btnRenegotiateAll` | Renegotiate All | `XNAClientButton` | Code |
| `lblTitle` | Tunnel Negotiation Status | `XNALabel` | Code |
| `lbPairs` | Player pair list | `XNAMultiColumnListBox` | Code |
| `matrixPanel` | Tunnel matrix panel | `XNAPanel` | Code |
| `tabControl` | Tab control | `XNAClientTabControl` | Code |

### TunnelSelectionWindow

Tunnel selection window.

- Source: `DXMainClient\DXGUI\Multiplayer\CnCNet\TunnelSelectionWindow.cs` | Base: `XNAWindow` | INI: `TunnelSelectionWindow.ini` | Controls: 8

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnApply` | Apply | `XNAClientButton` | Code |
| `btnAutoSelectTunnel` | Auto Select | `XNAClientButton` | Code |
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnSaveDefaultTunnel` | Save as Default | `XNAClientButton` | Code |
| `ddMode` | Tunnel mode selection | `XNADropDown` | Code |
| `lblDescription` | Line 1 | `XNALabel` | Code |
| `lbTunnelList` | Tunnel List | `TunnelListBox` | Code |
| `pnlTunnelListDisabledOverlay` | Tunnel List Disabled Overlay | `XNAPanel` | Code |

## Campaign

### CampaignSelector

Campaign mission selector with list, description and difficulty.

- Source: `DXMainClient\DXGUI\Campaign\CampaignSelector.cs` | Base: `XNAWindow` | INI: `CampaignSelector.ini` | Controls: 17

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnLaunch` | Launch game/mission | `XNAClientButton` | Code |
| `btnReturn` | Back | `XNAClientButton` | Code |
| `lbCampaignList` | Campaign mission list | `XNAListBox` | Code |
| `lblDifficultyLevel` | Difficulty heading | `XNALabel` | Code |
| `lblEasy` | Easy | `XNALabel` | Code |
| `lblGameSpeed` | GAME SPEED | `XNALabel` | Code |
| `lblHard` | Hard | `XNALabel` | Code |
| `lblMissionDescriptionHeader` | Mission description header | `XNALabel` | Code |
| `lblNormal` | Normal | `XNALabel` | Code |
| `lblSelectCampaign` | MISSIONS: | `XNALabel` | Code |
| `pnlMissionPreview` | Mission preview panel | `XNAPanel` | Code |
| `spnMissionDescription` | Spn Mission Description | `XNAScrollPanel` | Code |
| `tbMissionDescription` | Mission description | `XNATextBlock` | Code |
| `trbDifficultySelector` | Difficulty trackbar | `XNATrackbar` | Code |
| `trbGameSpeedSelector` | Game Speed Selector | `XNATrackbar` | Code |
| `trMissionDescription` | Tr Mission Description | `XNATextRenderer` | Code |

### CampaignTagSelector

- Source: `DXMainClient\DXGUI\Campaign\CampaignTagSelector.cs` | Base: `INItializableWindow` | INI: `CampaignTagSelector.ini` | Controls: 2

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | FindChild |
| `btnShowAllMission` | Show all missions | `XNAClientButton` | FindChild |

### CheaterWindow

Cheat/file modification detection window.

- Source: `DXMainClient\DXGUI\Campaign\CheaterWindow.cs` | Base: `XNAWindow` | INI: `CheaterScreen.ini` | Controls: 5

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnCancel` | Cancel | `XNAClientButton` | Code |
| `btnYes` | Yes | `XNAClientButton` | Code |
| `imagePanel` | Notice image panel | `XNAPanel` | Code |
| `lblCheater` | CHEATER! | `XNALabel` | Code |
| `lblDescription` | Description | `XNALabel` | Code |

## Options Window Panels

### AudioOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\AudioOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `AudioOptionsPanel.ini` | Controls: 17

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `chkMainMenuMusic` | Main menu music | `XNAClientCheckBox` | Code |
| `chkPlaySoundOnGameHosted` | Play sound when a game is hosted | `XNAClientCheckBox` | Code |
| `chkScoreShuffle` | Shuffle Music | `XNAClientCheckBox` | Code |
| `chkStopGameLobbyMessageAudio` | Stop game lobby message audio | `XNAClientCheckBox` | Code |
| `chkStopMusicOnMenu` | Don't play main menu music in lobbies | `XNAClientCheckBox` | Code |
| `lblClientVolume` | Client Volume: | `XNALabel` | Code |
| `lblClientVolumeValue` | 0 | `XNALabel` | Code |
| `lblScoreVolume` | Music Volume: | `XNALabel` | Code |
| `lblScoreVolumeValue` | 0 | `XNALabel` | Code |
| `lblSoundVolume` | Sound Volume: | `XNALabel` | Code |
| `lblSoundVolumeValue` | 0 | `XNALabel` | Code |
| `lblVoiceVolume` | Voice Volume: | `XNALabel` | Code |
| `lblVoiceVolumeValue` | 0 | `XNALabel` | Code |
| `trbClientVolume` | Client Volume | `XNATrackbar` | Code |
| `trbScoreVolume` | Score Volume | `XNATrackbar` | Code |
| `trbSoundVolume` | Sound Volume | `XNATrackbar` | Code |
| `trbVoiceVolume` | Voice Volume | `XNATrackbar` | Code |

### CnCNetOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\CnCNetOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `CnCNetOptionsPanel.ini` | Controls: 19

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `chkAllowGameInvitesFromFriendsOnly` | Only receive game invitations from friends | `XNAClientCheckBox` | Code |
| `chkConnectOnStartup` | Connect automatically on client startup | `XNAClientCheckBox` | Code |
| `chkDisablePrivateMessagePopup` | Disable popups from private messages | `XNAClientCheckBox` | Code |
| `chkDiscordIntegration` | Show detailed game info in Discord status | `XNAClientCheckBox` | Code |
| `chkEnableP2P` | Allow P2P connections | `XNAClientCheckBox` | Code |
| `chkNotifyOnUserListChange` | Notify On User List Change | `XNAClientCheckBox` | Code |
| `chkPersistentMode` | Stay connected outside of the CnCNet lobby | `XNAClientCheckBox` | Code |
| `chkPingUnofficialTunnels` | Ping unofficial CnCNet tunnels | `XNAClientCheckBox` | Code |
| `chkSkipLoginWindow` | Skip login dialog | `XNAClientCheckBox` | Code |
| `chkSteamIntegration` | Show the game being played in Steam | `XNAClientCheckBox` | Code |
| `chkWriteInstallPathToRegistry` | Write install path to registry | `XNAClientCheckBox` | Code |
| `ddAllowPrivateMessagesFrom` | Allow private messages from | `XNAClientDropDown` | Code |
| `ddTunnelMode` | Tunnel Mode | `XNAClientDropDown` | Code |
| `gameIcon` | Game icon | `XNAPanel` | Code |
| `gameListPanel` | Game list panel | `XNAPanel` | Code |
| `gamePanel` | Game information panel | `XNAPanel` | Code |
| `lblAllPrivateMessagesFrom` | Allow private messages from: | `XNALabel` | Code |
| `lblFollowedGames` | Show game rooms from the following games: | `XNALabel` | Code |
| `lblTunnelMode` | Tunnel mode for games you host: | `XNALabel` | Code |

### ComponentsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\ComponentsPanel.cs` | Base: `XNAOptionsPanel` | INI: `ComponentsPanel.ini` | Controls: 0

No own controls; all controls come from the base class or INI extra controls.

### DisplayOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\DisplayOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `DisplayOptionsPanel.ini` | Controls: 25

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnGameCompatibilityFix` | Disable | `XNAClientButton` | Code |
| `btnMapEditorCompatibilityFix` | Enable | `XNAClientButton` | Code |
| `chkBackBufferInVRAM` | Back Buffer In VRAM | `XNAClientCheckBox` | Code |
| `chkBorderlessClient` | Fullscreen Client | `XNAClientCheckBox` | Code |
| `chkBorderlessWindowedMode` | Borderless Windowed Mode | `XNAClientCheckBox` | Code |
| `chkEnableBackgroundVideo` | Enable Background Video | `XNAClientCheckBox` | Code |
| `chkEnableBackgroundVideoSound` | Video Sound | `XNAClientCheckBox` | Code |
| `chkIntegerScaledClient` | Integer Scaled Client | `XNAClientCheckBox` | Code |
| `chkUseTTFFonts` | Use TrueType Fonts | `XNAClientCheckBox` | Code |
| `chkWindowedMode` | Windowed Mode | `XNAClientCheckBox` | Code |
| `ddClientResolution` | Client Resolution | `XNAClientPreferredItemDropDown` | Code |
| `ddClientTheme` | Client Theme | `XNAClientDropDown` | Code |
| `ddDetailLevel` | Detail level | `XNAClientDropDown` | Code |
| `ddIngameResolution` | Ingame Resolution | `XNAClientDropDown` | Code |
| `ddRenderer` | Renderer | `XNAClientDropDown` | Code |
| `ddTranslation` | Translation | `XNAClientDropDown` | Code |
| `lblClientResolution` | Client Resolution: | `XNALabel` | Code |
| `lblClientTheme` | Client Theme: | `XNALabel` | Code |
| `lblCompatibilityFixes` | Legacy Compatibility Fixes: | `XNALabel` | Code |
| `lblDetailLevel` | Detail Level: | `XNALabel` | Code |
| `lblGameCompatibilityFix` | DTA/TI/TS Compatibility Fix: | `XNALabel` | Code |
| `lblIngameResolution` | In-game Resolution: | `XNALabel` | Code |
| `lblMapEditorCompatibilityFix` | FinalSun Compatibility Fix: | `XNALabel` | Code |
| `lblRenderer` | Renderer: | `XNALabel` | Code |
| `lblTranslation` | Language: | `XNALabel` | Code |

### GameOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\GameOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `GameOptionsPanel.ini` | Controls: 14

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnConfigureHotkeys` | Configure Hotkeys | `XNAClientButton` | Code |
| `chkAltToUndeploy` | Undeploy units by holding Alt key instead of a regular move command | `SettingCheckBox` | Code |
| `chkBlackChatBackground` | Use black background for in-game chat messages | `SettingCheckBox` | Code |
| `chkRecordReplays` | Record Replays | `XNAClientCheckBox` | Code |
| `chkScrollCoasting` | Scroll Coasting | `SettingCheckBox` | Code |
| `chkShowHiddenObjects` | Show Hidden Objects | `SettingCheckBox` | Code |
| `chkTargetLines` | Target Lines | `SettingCheckBox` | Code |
| `chkTooltips` | Tooltips | `SettingCheckBox` | Code |
| `lblNotice` | Notice | `XNALabel` | Code |
| `lblPlayerName` | Player Name*: | `XNALabel` | Code |
| `lblScrollRate` | Scroll Rate: | `XNALabel` | Code |
| `lblScrollRateValue` | 0 | `XNALabel` | Code |
| `tbPlayerName` | Player Name | `XNATextBox` | Code |
| `trbScrollRate` | Scroll Rate | `XNATrackbar` | Code |

### StorageOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\StorageOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `StorageOptionsPanel.ini` | Controls: 37

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `bottomMargin` | Bottom margin panel | `XNAPanel` | Code |
| `lblFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblGameLogAge` | Delete after: | `XNALabel` | Code |
| `lblGameLogAgeSuffix` | days (0 = never) | `XNALabel` | Code |
| `lblGameLogFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblGameLogFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblGameLogRetentionHint` | Game Log Retention Hint | `XNALabel` | Code |
| `lblGameLogsHeader` | Game Logs | `XNALabel` | Code |
| `lblKeptLogFiles` | Keep at most: | `XNALabel` | Code |
| `lblKeptLogFilesSuffix` | old log files (0 = no limit) | `XNALabel` | Code |
| `lblKeptReplays` | Keep at most: | `XNALabel` | Code |
| `lblKeptReplaysSuffix` | replays (0 = no limit) | `XNALabel` | Code |
| `lblKeptSavedGames` | Keep at most: | `XNALabel` | Code |
| `lblKeptSavedGamesSuffix` | saved games (0 = no limit) | `XNALabel` | Code |
| `lblKeyframesHeader` | Playback keyframes | `XNALabel` | Code |
| `lblKeyframeSize` | Maximum size: | `XNALabel` | Code |
| `lblKeyframeSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblLogFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblLogFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblLogsHeader` | Client Logs | `XNALabel` | Code |
| `lblReplaysHeader` | Replays | `XNALabel` | Code |
| `lblReplayUsage` | Replay Usage | `XNALabel` | Code |
| `lblSavedGameFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblSavedGameFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblSavedGameRetentionHint` | When limits are reached, the oldest saves are permanently deleted and ... | `XNALabel` | Code |
| `lblSavedGamesHeader` | Single-Player Saved Games | `XNALabel` | Code |
| `scrollPanel` | Scroll container | `StorageScrollPanel` | Code |
| `tbMaxGameLogAge` | Max Game Log Age | `XNATextBox` | Code |
| `tbMaxGameLogFolderSize` | Max Game Log Folder Size | `XNATextBox` | Code |
| `tbMaxKeptLogFiles` | Max Kept Log Files | `XNATextBox` | Code |
| `tbMaxKeptReplays` | Max Kept Replays | `XNATextBox` | Code |
| `tbMaxKeptSavedGames` | Max Kept Saved Games | `XNATextBox` | Code |
| `tbMaxLogFolderSize` | Max Log Folder Size | `XNATextBox` | Code |
| `tbMaxReplayFolderSize` | Max Replay Folder Size | `XNATextBox` | Code |
| `tbMaxSavedGameFolderSize` | Max Saved Game Folder Size | `XNATextBox` | Code |
| `tbReplayKeyframeStorageLimit` | Replay Keyframe Storage Limit | `XNATextBox` | Code |

### StorageScrollPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\StorageOptionsPanel.cs` | Base: `XNAScrollPanel` | INI: `StorageScrollPanel.ini` | Controls: 37

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `bottomMargin` | Bottom margin panel | `XNAPanel` | Code |
| `lblFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblGameLogAge` | Delete after: | `XNALabel` | Code |
| `lblGameLogAgeSuffix` | days (0 = never) | `XNALabel` | Code |
| `lblGameLogFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblGameLogFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblGameLogRetentionHint` | Game Log Retention Hint | `XNALabel` | Code |
| `lblGameLogsHeader` | Game Logs | `XNALabel` | Code |
| `lblKeptLogFiles` | Keep at most: | `XNALabel` | Code |
| `lblKeptLogFilesSuffix` | old log files (0 = no limit) | `XNALabel` | Code |
| `lblKeptReplays` | Keep at most: | `XNALabel` | Code |
| `lblKeptReplaysSuffix` | replays (0 = no limit) | `XNALabel` | Code |
| `lblKeptSavedGames` | Keep at most: | `XNALabel` | Code |
| `lblKeptSavedGamesSuffix` | saved games (0 = no limit) | `XNALabel` | Code |
| `lblKeyframesHeader` | Playback keyframes | `XNALabel` | Code |
| `lblKeyframeSize` | Maximum size: | `XNALabel` | Code |
| `lblKeyframeSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblLogFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblLogFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblLogsHeader` | Client Logs | `XNALabel` | Code |
| `lblReplaysHeader` | Replays | `XNALabel` | Code |
| `lblReplayUsage` | Replay Usage | `XNALabel` | Code |
| `lblSavedGameFolderSize` | Maximum size: | `XNALabel` | Code |
| `lblSavedGameFolderSizeSuffix` | MB (0 = no limit) | `XNALabel` | Code |
| `lblSavedGameRetentionHint` | When limits are reached, the oldest saves are permanently deleted and ... | `XNALabel` | Code |
| `lblSavedGamesHeader` | Single-Player Saved Games | `XNALabel` | Code |
| `scrollPanel` | Scroll container | `StorageScrollPanel` | Code |
| `tbMaxGameLogAge` | Max Game Log Age | `XNATextBox` | Code |
| `tbMaxGameLogFolderSize` | Max Game Log Folder Size | `XNATextBox` | Code |
| `tbMaxKeptLogFiles` | Max Kept Log Files | `XNATextBox` | Code |
| `tbMaxKeptReplays` | Max Kept Replays | `XNATextBox` | Code |
| `tbMaxKeptSavedGames` | Max Kept Saved Games | `XNATextBox` | Code |
| `tbMaxLogFolderSize` | Max Log Folder Size | `XNATextBox` | Code |
| `tbMaxReplayFolderSize` | Max Replay Folder Size | `XNATextBox` | Code |
| `tbMaxSavedGameFolderSize` | Max Saved Game Folder Size | `XNATextBox` | Code |
| `tbReplayKeyframeStorageLimit` | Replay Keyframe Storage Limit | `XNATextBox` | Code |

### UpdaterOptionsPanel

- Source: `DXMainClient\DXGUI\Generic\OptionPanels\UpdaterOptionsPanel.cs` | Base: `XNAOptionsPanel` | INI: `UpdaterOptionsPanel.ini` | Controls: 6

| Control | Purpose | Type | Creation |
|---|---|---|---|
| `btnForceUpdate` | Force Update | `XNAClientButton` | Code |
| `btnMoveDown` | Move down | `XNAClientButton` | Code |
| `btnMoveUp` | Move up | `XNAClientButton` | Code |
| `chkAutoCheck` | Auto check for updates | `XNAClientCheckBox` | Code |
| `lblDescription` | Description | `XNALabel` | Code |
| `lblUpdateServerList` | Update Server List | `XNAListBox` | Code |


