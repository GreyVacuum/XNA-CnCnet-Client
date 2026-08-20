# INI System — Constructing the Client UI with INI Files

This document is the authoritative reference for building the client's user interface with INI files. It is generated from and verified against the client source code; every key listed below is parsed by the implementation referenced next to it.

## Table of Contents

1. [How INI Files Are Resolved](#how-ini-files-are-resolved)
2. [Constants](#constants)
3. [Data Types](#data-types)
4. [Creating Controls From INI](#creating-controls-from-ini)
5. [Basic Control Properties](#basic-control-properties)
6. [Client Control Properties](#client-control-properties)
7. [Dynamic Control Properties and Expressions](#dynamic-control-properties-and-expressions)
8. [Late Attributes (Control Linking)](#late-attributes-control-linking)
9. [Special Controls](#special-controls)
10. [Windows](#windows)
11. [Global Config Files](#global-config-files)

---

## How INI Files Are Resolved

Every control that is driven by INI reads its own section `[ControlName]` from an INI file. The section name **must** equal the control's `Name` (which is set in code or by the `$CC` mechanism, see [Creating Controls From INI](#creating-controls-from-ini)).

There are two distinct initialization models:

| Model | Class | INI lookup | Extra control support |
|---|---|---|---|
| Window model | [`XNAWindow`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindow.cs) (inherits [`XNAWindowBase`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindowBase.cs)) | `{Name}.ini` in the active theme's resource folder, then the base resource folder, then `GenericWindow.ini`'s `[GenericWindow]` section as fallback | `[Name]ExtraControls` (opt-in via `EnabledExtraControls`) |
| INItializable model | [`INItializableWindow`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/INItializableWindow.cs) (inherits `XNAPanel`) | `{Name}.ini` resolved with the same theme-then-base priority; no generic fallback | `$CC` child controls and `[$ExtraControls]` section |

Key implementation notes:

- **Resource path priority** — the client first looks for the INI in the *theme-specific* resource path (`ProgramConstants.GetResourcePath()`), then in the *base* resource path (`GetBaseResourcePath()`).
- **`IniNameOverride`** (code-only, protected) — if set, the window reads `{IniNameOverride}.ini` instead of `{Name}.ini`, falling back to `{Name}.ini` when the override file does not exist.
- **`ExternalIniFile`** (code-only) — when an `INItializableWindow` is registered as an extra control of another window, it is initialized from the hosting window's INI file so its `[Name]` section and `$CC` children can be declared in the same file.
- **Case sensitivity** — INI keys are matched case-insensitively by the parser; section names and control names are case-sensitive for lookups inside the control tree, so keep them consistent.

---

## INI Inheritance

INI files used by the client support two forms of inheritance.

### Section inheritance — `BaseSection` / `$BaseSection`

Any section may inherit keys from another section **in the same file**. Keys that are not already defined in the inheriting section are copied from the base section; keys defined by the inheriting section are kept.

- **`BaseSection=<section>`** — applied by the INI preprocessor (`IniPreprocessor`), used for window layout files. Works recursively; if a base section is missing the section is left untouched.
- **`$BaseSection=<section>`** — the same mechanism applied at load time by `CCIniFile` (the client's INI class). A missing base section only logs a warning.

```ini
[lbChatMessages_Player]
BaseSection=lbChatMessages
```

### File inheritance — `[INISystem] BasedOn`

A `CCIniFile`-based file may chain-merge other files through the `[INISystem]` section:

```ini
[INISystem]
BasedOn=GameLobbyBase.ini,MoreSettings.ini
```

Each listed file is loaded and its sections are consolidated into the including file; sections already present in the including file take precedence. Relative paths are resolved against the including file's directory, and the `$THEME_DIR$` token resolves to the resource path. A missing base file only produces a warning.

Window layout files use both mechanisms together: for example `MultiplayerGameLobby.ini` declares `[INISystem] BasedOn=GameLobbyBase.ini`, and control sections inside it reuse other sections via `BaseSection`.

### Background preprocessing — `INI/Base`

At startup the client runs a background task that preprocesses every `*.ini` file in `INI/Base` (game folder): `BaseSection` inheritance is applied and the result is written to `INI/<name>.ini`. A `ProcessedIniInfo.ini` (user files folder) stores the SHA1 hashes of the source and the processed file, so only outdated files are re-processed. `desktop.ini` is ignored.

---

## Constants

Constants are **integers** that can be referenced from [dynamic control properties](#dynamic-control-properties-and-expressions). They are resolved at window initialization time by the expression `Parser` (`ClientGUI/Parser.cs`).

### Definition

Constants are defined in the `[ParserConstants]` section of `DTACnCNetClient.ini` (or any theme file that `DTACnCNetClient.ini` includes, e.g. `GlobalThemeSettings.ini`):

```ini
; DTACnCNetClient.ini
[ParserConstants]
MY_EXAMPLE_CONSTANT=15
```

In addition, the `[SameNameConstants]` section of the same file defines **aliases**. An alias maps a constant name to another constant name and supports chaining (`A=B`, `B=C`); cyclic alias definitions are detected and rejected at parse time:

```ini
[SameNameConstants]
ALIAS_OF_WIDTH=RESOLUTION_WIDTH
```

### Predefined System Constants

| Constant | Value |
|---|---|
| `RESOLUTION_WIDTH` | The render resolution width of the game window when initialized. |
| `RESOLUTION_HEIGHT` | The render resolution height of the game window when initialized. |

### Usage

```ini
[MyExampleControl]
$X=MY_EXAMPLE_CONSTANT
$Width=RESOLUTION_WIDTH/2
```

> [!NOTE]
> Constants (and expressions) can **only** be used in dynamic control properties (keys starting with `$`). Basic control properties (e.g. `X=`, `Width=`) are parsed as plain values and do not support constants.

### Lookup order

1. `[ParserConstants]` (canonical definitions take precedence).
2. `[SameNameConstants]` alias chain — an unknown name is resolved through the alias table before failing. The error message tells you to check `[ParserConstants]` in `DTACnCNetClient.ini` or its dependencies.

### Arithmetic expressions

Dynamic property values are parsed as arithmetic expressions supporting `+`, `-`, `*`, `/`, parentheses `( )`, constants, and the functions below. Whitespace is ignored.

| Function | Description |
|---|---|
| `getX(ControlName)` / `getY(ControlName)` | Returns the X/Y coordinate of the named control. |
| `getWidth(ControlName)` / `getHeight(ControlName)` | Returns the width/height of the named control. |
| `getBottom(ControlName)` | Returns `Y + Height` of the named control (`XNAOptionsPanel` uses its scrollable bottom). |
| `getRight(ControlName)` | Returns `X + Width` of the named control (`XNAOptionsPanel` uses its scrollable right). |
| `horizontalCenterOnParent()` | Centers the parsed control horizontally on its parent and returns the resulting X. |

Special parameter names:

| Parameter | Meaning |
|---|---|
| `$Self` | The control whose dynamic property is being parsed. |
| `$ParentControl` | The logical parent of the control being parsed (skips `XNAScrollPanel`/content-panel internals, so it resolves to the real container such as an `XNAOptionsPanel`). |

Control lookup rules for expression parameters: the primary (window) control itself → descendants of the primary control → ancestors and their **direct** children (sibling controls of the primary control). A deeply nested control of a sibling subtree can never shadow a window-level control with the same name.

Example:

```ini
[lblHeader]
$X=horizontalCenterOnParent()
$Y=getY(btnBack)+10
$Width=getWidth(btnBack)
```

---

## Data Types

| Type | Syntax / Notes |
|---|---|
| `text` | `@` is a line break (replaced at parse time in the control's `Text` attribute, `XNAControl.cs`). The `\@` and `\semicolon` escapes are honored **only** by the translation system (`FromIniString`); they are **not** processed in control INI files, where a raw `;` always starts a comment. |
| `color` | `R,G,B` or `R,G,B,A`. All components must be between `0` and `255`. Examples: `255,255,255`, `255,255,255,128`. |
| `boolean` | Parsed by first character: `t`, `y`, `1`, `a`, `e` ⇒ `true`; `n`, `f`, `0` ⇒ `false`. Anything else falls back to the control's default. `true`/`false`, `yes`/`no` and `1`/`0` are all accepted. |
| `integer` | `System.Int32`. |
| `float` | `System.Single`. |
| `N integers` / `N floats` | Exactly `N` values of the respective type separated by commas **without spaces**, e.g. `0,0` or `0.0,0.0`. |
| `comma-separated strings` | Strings separated by commas without spaces, e.g. `one,two,three`. |
| `string:type` | Two parts separated by a colon; used by keys such as `$CC` and `ColumnX` (see their sections). |

---

## Creating Controls From INI

### `$CC` child controls (INItializableWindow)

Any key starting with `$CC` in a control's section creates a child control. The value format is `ControlName:ControlType`:

```ini
[MyWindow]
$CC00=btnOk:XNAClientButton
$CC01=pnlContent:XNAPanel

[btnOk]
Text=OK
X=100

[pnlContent]
BackgroundTexture=content.png
```

Rules:

- The child control's section (named after `ControlName`) is then parsed recursively, so children can have their own `$CC` children.
- `ControlType` must be a registered control type name (see [Control Types](#control-types) below). The type is looked up by `Type.Name`.
- **Child control names may only contain letters, digits and underscores.** Any other character throws an `INIConfigException`.
- Children of an `XNAScrollPanel` are mounted onto its content panel automatically.
- `DrawOrder` of `$CC` children is set to `-Children.Count` (negative), so children created from INI are drawn before code-created children.

### `$Include` (INItializableWindow)

A control section can merge keys from another INI file with the `$Include` directive. The key name is `$Include` followed by any text; the value is the path to the included file. Included keys **override** keys already present in the section:

```ini
[MyWindow]
$Include00=SpawnGameOptions.ini

; MyWindow.ini
```

Path resolution:

- `$THEME_DIR$` inside the value is replaced with the theme-specific resource path.
- Otherwise the path is resolved relative to the directory of the current INI file.

The `$Include` keys are removed from the section after processing. If the included file is missing, or does not contain a section named after the current control, an error is logged and parsing continues.

### Extra controls

Two mechanisms allow adding controls that are not declared in the section of their host:

1. **`[$ExtraControls]` section (INItializableWindow)** — keys starting with `$CC` define extra controls with the same `Name:Type` format. The control is only created if no existing child already has that name.

```ini
[MyWindow]
; ...

[$ExtraControls]
$CC00=pnlExtra:XNAClientPanel
```

2. **`[Name]ExtraControls` section (XNAWindowBase / XNAOptionsPanel)** — used by `XNAWindow`-derived windows and `XNAOptionsPanel`. Keys are arbitrary (`Name:Type` values). Extra controls are recursively supported: a control created as an extra control can itself host extra controls through its own `[Name]ExtraControls` section if `EnabledExtraControls` is set.

```ini
[GameOptionsPanel]
; ...

[GameOptionsPanelExtraControls]
pnlModOptions=XNAPanel

[pnlModOptions]
EnabledExtraControls=Yes

[pnlModOptionsExtraControls]
chkModOption=SettingCheckBox
```

- `EnabledExtraControls` (boolean, default `false` for `XNAWindowBase`; **default `true` for `XNAOptionsPanel`**) enables hosting of extra controls on the control itself.
- When an `INItializableWindow` is created as an extra control, its `ExternalIniFile` is set to the hosting window's INI file so it initializes from the same file.

### Control Types

Controls that can be created from INI are registered by their `Type.Name` (see `DXMainClient/DXGUI/GameClass.cs`). The registered names are:

`XNAControl`, `XNAButton`, `XNAInteractionButton`, `XNAClientButton`, `XNAClientCheckBox`, `XNAClientDropDown`, `XNALinkButton`, `XNAExtraPanel`, `XNACheckBox`, `XNADropDown`, `XNAClientTabControl`, `XNATabControl`, `XNALabel`, `XNALinkLabel`, `XNAClientLinkLabel`, `XNAListBox`, `XNAMultiColumnListBox`, `XNAPanel`, `XNAScrollPanel`, `XNAProgressBar`, `XNASuggestionTextBox`, `XNATextBox`, `XNATextBlock`, `XNATrackbar`, `XNAChatTextBox`, `ChatListBox`, `INItializableWindow`, `GameLobbyCheckBox`, `GameLobbyDropDown`, `CampaignCheckBox`, `CampaignDropDown`, `SettingCheckBox`, `SettingDropDown`, `FileSettingCheckBox`, `FileSettingDropDown`, plus the singleton windows (`LoadingScreen`, `MainMenu`, `TopBar`, `OptionsWindow`, `CnCNetLobby`, `CnCNetGameLobby`, `SkirmishLobby`, `MapPreviewBox`, `CampaignTagSelector`, ...).

---

## Basic Control Properties

> [!WARNING]
> Do not copy-paste the snippets below without editing them — they only illustrate how each property works. For example `X`/`Y` conflict with `Location`, `BackgroundTexture` conflicts with `SolidColorBackgroundTexture`, and so on.

> [!NOTE]
> **Property order matters.** Properties that depend on the size of a control (e.g. `DistanceFromRightBorder`, `FillWidth`) must be listed *after* the properties that set that size (`Width`/`Height`/`Size`). INI keys are applied in file order.

### [XNAControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAControl.cs)

Base class of every control element.

```ini
[SOMECONTROL]                      ; XNAControl
X=                                 ; integer,    the X location of the control relative to its parent.
Y=                                 ; integer,    the Y location of the control relative to its parent.
Location=                          ; 2 integers, the X and Y location of the control.
Width=                             ; integer,    the width of the control.
Height=                            ; integer,    the height of the control.
Size=                              ; 2 integers, the width and height of the control.
Text=                              ; text,       the text to display (buttons, labels, ...). `@` becomes a line break.
Visible=true                       ; boolean,    whether the control is visible by default. Setting `Visible` also sets `Enabled` to the same value.
Enabled=true                       ; boolean,    whether the control can be interacted with by default.
DistanceFromRightBorder=0          ; integer,    distance of the control's right edge from its parent's right edge.
                                   ;             Requires a parent; silently ignored when the control has no parent.
DistanceFromBottomBorder=0         ; integer,    distance of the control's bottom edge from its parent's bottom edge.
                                   ;             Requires a parent; silently ignored when the control has no parent.
FillWidth=0                        ; integer,    sets the width to `Parent.Width - X - value` (or the window's render
                                   ;             resolution width when parentless).
FillHeight=0                       ; integer,    sets the height to `Parent.Height - Y - value` (or the window's render
                                   ;             resolution height when parentless).
DrawOrder=0                        ; integer,    layering order among the parent's children. Parsed with `int.Parse`;
                                   ;             an invalid value throws an exception.
UpdateOrder=0                      ; integer,    update order among the parent's children (higher updates first).
RemapColor=255,255,255             ; color,      theme-defined remap color applied to the control's textures.
ControlDrawMode=UniqueRenderTarget ; string,     only the exact value `UniqueRenderTarget` has an effect: it draws the
                                   ;             control onto its own render target. Any other value (including
                                   ;             `Normal`) is ignored and the control keeps the default behavior of
                                   ;             drawing onto the same target as its parent.
```

### [XNAPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAPanel.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMEPANEL]                  ; XNAPanel
BorderColor=196,196,196      ; color,      border color.
AlphaRate=0.01               ; float,      transparency change rate per 100 ms; a transparent panel becomes opaque at this rate.
BackgroundTexture=           ; string,     texture loaded by file name (with extension). If the texture is not found in any
                             ;             asset search path, a dummy texture is returned.
SolidColorBackgroundTexture= ; color,      replaces the background with a stretched solid-color texture.
DrawBorders=true             ; boolean,    enables/disables border drawing. Borders are enabled by default.
Padding=                     ; 4 integers, CSS-like padding `left,top,right,bottom`: expands the control rectangle and shifts
                             ;             all existing children by the left/top amounts.
DrawMode=Stretched           ; enum (Tiled | Centered | Stretched),
                             ;             background texture draw mode (default Stretched).
```

### [XNAExtraPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAExtraPanel.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMEEXTRAPANEL]   ; XNAExtraPanel
BackgroundTexture= ; string, same as XNAPanel's BackgroundTexture. When the panel has zero width/height, it resizes itself to
                   ;         the texture dimensions.
```

### [XNATextBlock](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBlock.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMETEXTBLOCK]       ; XNATextBlock
TextColor=196,196,196 ; color, text color.
```

### [XNAIndicator](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAIndicator.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMEINDICATOR]            ; XNAIndicator
FontIndex=0                ; integer, index of the font loaded from the font list. Default `0`.
HighlightColor=255,255,255 ; color,   text color when the cursor is above the indicator.
AlphaRate=0.1              ; float,   transparency change rate per 100 ms.
```

### [XNALabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNALabel.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMELABEL]            ; XNALabel
RemapColor=255,255,255 ; color,    alias of TextColor — the `RemapColor` key is intercepted and sets the text color.
TextColor=196,196,196  ; color,    text color.
FontIndex=0            ; integer,  index of the font loaded from the font list.
AnchorPoint=0.0,0.0    ; 2 floats, text start drawing point.
TextShadowDistance=1.0 ; float,    distance between the text and its shadow (default 1.0).
TextAnchor=            ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER | CENTER),
                       ;           text anchor inside the label's draw box. `CENTER` = HORIZONTAL_CENTER | VERTICAL_CENTER.
```

### [XNAButton](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAButton.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMEBUTTON]               ; XNAButton
TextColorIdle=255,255,255  ; color,   text color when the cursor is not above the button.
TextColorHover=255,255,255 ; color,   text color when the cursor is above the button.
HoverSoundEffect=          ; string,  sound file played on hover.
ClickSoundEffect=          ; string,  sound file played on click.
AdaptiveText=true          ; boolean, whether the client adjusts the text start position to fill all free space. Default `true`.
AlphaRate=0.01             ; float,   transparency change rate per 100 ms.
FontIndex=0                ; integer, index of the font loaded from the font list.
IdleTexture=               ; string,  idle texture file name. When the texture loads successfully, the button's
                           ;          ClientRectangle is resized to the texture dimensions.
HoverTexture=              ; string,  hover texture file name.
TextShadowDistance=1.0     ; float,   distance between the text and its shadow (default 1.0).
```

### [XNAProgressBar](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAProgressBar.cs)

_(inherits [XNAPanel](#xnapanel))_

`XNAProgressBar` defines no INI-specific properties; it uses the inherited panel/basic properties only.

### [XNALinkLabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNALinkLabel.cs)

_(inherits [XNALabel](#xnalabel))_

```ini
[SOMELINKLABEL]     ; XNALinkLabel
IdleColor=          ; color,  text color when the cursor is not above the label (default: theme text color).
HoverColor=         ; color,  text color when the cursor is above the label (default: theme alt color).
DrawUnderline=true  ; boolean, draws an underline below the text. Default `true`.
```

### [XNACheckBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNACheckBox.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMECHECKBOX]             ; XNACheckBox
FontIndex=0                ; integer, index of the font loaded from the font list.
IdleColor=196,196,196      ; color,   text color when the cursor is not above the checkbox.
HighlightColor=255,255,255 ; color,   text color when the cursor is above the checkbox.
AlphaRate=0.1              ; float,   transparency change rate per 100 ms.
AllowChecking=true         ; boolean, allows the user to check/uncheck the checkbox.
Checked=true               ; boolean, default checked state.
TextPadding=5              ; integer, horizontal distance between the checkbox and its text. Default `5`.
```

### [XNADropDown](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNADropDown.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMEDROPDOWN]                  ; XNADropDown
OpenUp=false                    ; boolean, defines whether the dropdown opens upwards. Default `false`.
DropDownTexture=                ; string,  texture used when the dropdown is closed.
DropDownOpenTexture=            ; string,  texture used when the dropdown is opened.
ItemHeight=                     ; integer, height of each item in pixels. Defaults to the theme value
                                ;          (UISettings.DropDownDefaultItemHeight) or the item text height + 1.
ClickSoundEffect=               ; string,  sound file played when opening the dropdown.
FontIndex=0                     ; integer, index of the font loaded from the font list.
BorderColor=196,196,196         ; color,   border color of the opened dropdown.
FocusColor=64,64,64             ; color,   background color of the item under the cursor.
BackColor=0,0,0                 ; color,   background color of the opened dropdown.
DisabledItemColor=169,169,169   ; color,   text color of disabled items.
ShowEllipsisOnOverflow=false    ; boolean, appends an ellipsis to item text that overflows the dropdown. Default `false`.
EnableScrollBar=true            ; boolean, shows a scroll bar when the item list is longer than the dropdown. Default `true`.
ScrollBarWidth=12               ; integer, width of the scroll bar in pixels. Default `12`.
ScrollBarThumbColor=            ; color,   scroll bar thumb color (default: theme value).
ScrollBarTrackColor=            ; color,   scroll bar track color (default: theme value).
ScrollBarBorderColor=           ; color,   scroll bar border color (default: theme value).
ScrollBarThumbPadding=2         ; integer, padding between the thumb and the scroll bar borders. Default `2`.
ScrollStep=3                    ; integer, scrolling speed in pixels per scroll step. Default `3`.
MaxVisibleItems=5               ; integer, number of items visible before the scroll bar appears. Default `5`.
OptionX=                        ; string,  adds an item with the given text. `X` is any text, e.g. `Option_FirstOption`.
; Option_FirstOption=1
; Option_SecondOption=two
; Option_ThirdOption=33333
```

### [XNATabControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATabControl.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMETABCONTROL]              ; XNATabControl
RemapColor=255,255,255        ; color,   tab text color (alias of TextColor).
TextColor=255,255,255         ; color,   tab text color.
TextColorDisabled=169,169,169 ; color,   text color of disabled tabs.
DisabledTabIndexN=false       ; boolean, disables the tab at index `N` (`N` must be a number). If the currently selected tab
                              ;          becomes disabled, the selection moves to the next selectable tab.
SetTabCount=3                 ; integer, creates/removes placeholder tabs so the control has exactly `N` tabs.
TabDirection=Horizontal       ; enum (Horizontal | Vertical), tab layout direction. Default `Horizontal`.
ClickSoundEffect=             ; string,  default click sound for tabs (per-tab sounds override it).
IdleTexture=                  ; string,  default idle texture for tabs.
ClickTexture=                 ; string,  default pressed texture for tabs (alias: PressedTexture).
PressedTexture=               ; string,  alias of ClickTexture.
FontIndex=0                   ; integer, default tab font index.
TabText=                      ; text,    default text for tabs that have no per-tab text.
; DisabledTabIndex0=true
```

Per-tab properties use the dot-separated form `Tab{N}.{Property}` (a tab is created on demand if the index does not exist yet):

```ini
[SOMETABCONTROL]
Tab0.Text=First Tab
Tab0.IdleTexture=tab0idle.png
Tab0.ClickTexture=tab0click.png
Tab0.FontIndex=1
Tab0.ClickSoundEffect=tabclick.wav
Tab0.Selectable=true
Tab1.Text=Second Tab
```

Supported per-tab properties: `ClickSoundEffect`, `IdleTexture`, `ClickTexture`/`PressedTexture`, `FontIndex`, `Text`/`TabText`, `Selectable` (boolean, default `true`).

### [XNATextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBox.cs)

_(inherits [XNAControl](#xnacontrol))_

```ini
[SOMETEXTBOX]                ; XNATextBox
MaximumTextLength=2147483647 ; integer, maximum input length in characters (defaults to int.MaxValue).
```

### [XNASuggestionTextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNASuggestionTextBox.cs)

_(inherits [XNATextBox](#xnatextbox))_

```ini
[SOMESUGGESTIONTEXTBOX] ; XNASuggestionTextBox
Suggestion=             ; string, background text shown while the box is empty.
```

### [XNAListBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAListBox.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMELISTBOX]                   ; XNAListBox
EnableScrollbar=true            ; boolean, shows the integrated scroll bar. Default `true`.
DrawSelectionUnderScrollbar=false ; boolean, draws the selected-item highlight under the scroll bar area. Default `false`.
AllowMultiLineItems=true        ; boolean, when `false` only the first line of long items is displayed. Applies to new items only.
AllowRightClickUnselect=true    ; boolean, allows right-clicking to un-select the selected item. Default `true`.
FontIndex=0                     ; integer, index of the font loaded from the font list.
LineHeight=                     ; integer, height of a single item line in pixels (minimum 1).
TextBorderDistance=3            ; integer, horizontal distance between item text and the list box border. Default `3`.
```

### [XNAMultiColumnListBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAMultiColumnListBox.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMEMULTICOLUMNLISTBOX]         ; XNAMultiColumnListBox
FontIndex=0                      ; integer,        index of the font loaded from the font list.
LineHeight=                      ; integer,        height of a single item line in pixels (minimum 1).
DrawSelectionUnderScrollbar=yes  ; boolean,        draws the selected-item highlight of the last column under the scroll bar area.
ColumnWidthN=                    ; integer,        width of the column with index `N` in pixels (N starts from 0).
ColumnX=                         ; HeaderText:Width, defines a column; `X` is any text. The value is `HeaderText` + `:` + width,
                                 ;                 e.g. `Column0=Player:100` (the separator is a colon, not a comma).
ListBoxYAttribute:Attrname=Value ; string,         intended to set attribute `Attrname` on the internal single-column list
                                 ;                 box `Y` (e.g. `ListBox0Attribute:LineHeight=20`). `Attrname` is parsed by
                                 ;                 XNAListBox. [KNOWN ISSUE] the current prefix check compares the wrong
                                 ;                 substring (`"Attribute:"` vs `":Attribute"`) and never matches, so this
                                 ;                 key is currently non-functional.
```

### [XNATrackbar](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATrackbar.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMETRACKBAR] ; XNATrackbar
MinValue=0     ; integer, minimum value.
MaxValue=10    ; integer, maximum value.
Value=0        ; integer, default value.
ClickSound=    ; string,  sound file played when the trackbar is clicked.
```

### [XNAScrollPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAScrollPanel.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMESCROLLPANEL]       ; XNAScrollPanel
AllowKeyboardInput=true ; boolean, allows scrolling with the arrow keys. Default `true`.
AllowScroll=true,true   ; 2 booleans, allows scrolling on X and Y axes (`X,Y`).
AllowScrollX=true       ; boolean, allows horizontal scrolling. Default `true`.
AllowScrollY=true       ; boolean, allows vertical scrolling. Default `true`.
ScrollStep=             ; integer, scroll distance per step in pixels.
OverscrollMargin=0,0    ; 2 integers, how far the content may exceed the panel bounds on X and Y.
OverscrollMarginX=0     ; integer, horizontal overscroll margin.
OverscrollMarginY=0     ; integer, vertical overscroll margin.
DrawBorders=true        ; boolean, enables/disables border drawing.
; Padding is intentionally ignored by this control.
```

### [XNAContextMenu](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAContextMenu.cs)

_(inherits [XNAPanel](#xnapanel))_

```ini
[SOMECONTEXTMENU]        ; XNAContextMenu
ItemHeight=              ; integer, height of each item in pixels. Defaults to the theme value
                         ;          (UISettings.ContextMenuDefaultItemHeight) or the item text height + 1.
FontIndex=0              ; integer, index of the font for item text.
HintFontIndex=0          ; integer, index of the font for item hints.
TextHorizontalPadding=1  ; integer, horizontal text padding inside items.
TextVerticalPadding=1    ; integer, vertical text padding inside items.
```

### XNAClientTabControl

_(inherits XNATabControl)_ — `XNAClientTabControl` (ClientGUI) adds localization of tab text; it defines no additional INI keys.

### XNAPasswordBox / XNATimerControl

These controls define no INI-specific properties (they inherit their base controls' properties).

---

## Client Control Properties

Client controls (`ClientGUI/`) add client-specific properties on top of the XNAUI controls above.

### [XNAClientButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientButton.cs)

_(inherits [XNAButton](#xnabutton))_

```ini
[SOMECLIENTBUTTON] ; XNAClientButton
MatchTextureSize=  ; boolean, sets the button's width and height to match its texture dimensions.
ToolTip=           ; text,    tooltip text shown when hovering the button.
```

### [XNAClientToggleButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientToggleButton.cs)

_(inherits [XNAButton](#xnabutton))_

```ini
[SOMECLIENTTOGGLEBUTTON] ; XNAClientToggleButton
CheckedTexture=          ; string, texture shown when the toggle button is checked.
UncheckedTexture=        ; string, texture shown when the toggle button is unchecked.
ToolTip=                 ; text,   tooltip text.
```

### [XNALinkButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNALinkButton.cs)

_(inherits [XNAClientButton](#xnaclientbutton))_

```ini
[SOMELINKBUTTON] ; XNALinkButton
URL=             ; string, URL opened by the OS shell on click (Windows and other OSes when UnixURL is not set).
UnixURL=         ; string, URL opened on Unix-like OSes (overrides URL).
Arguments=       ; string, arguments passed to the shell, separated by spaces.
```

### [XNAClientLinkLabel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientLinkLabel.cs)

_(inherits [XNALinkLabel](#xnalinklabel))_

```ini
[SOMECLIENTLINKLABEL] ; XNAClientLinkLabel
ToolTip=              ; text,   tooltip text.
URL=                  ; string, URL opened by the OS shell on click.
UnixURL=              ; string, URL opened on Unix-like OSes (overrides URL).
HoverSoundEffect=     ; string, sound file played when the cursor enters the label.
ClickSoundEffect=     ; string, sound file played when the label is clicked.
```

### [XNAClientCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientCheckBox.cs)

_(inherits [XNACheckBox](#xnacheckbox))_

```ini
[SOMECLIENTCHECKBOX] ; XNAClientCheckBox
ToolTip=             ; text, tooltip text.
```

### [XNAClientDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientDropDown.cs)

_(inherits [XNADropDown](#xnadropdown))_

```ini
[SOMECLIENTDROPDOWN] ; XNAClientDropDown
ToolTip=            ; text, tooltip text. The tooltip is suppressed while the dropdown is open.
```

### [XNAClientColorDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientColorDropDown.cs)

_(inherits [XNAClientDropDown](#xnaclientdropdown))_

```ini
[SOMECOLORDROPDOWN] ; XNAClientColorDropDown
ItemsDrawMode=TextAndIcon         ; enum (Text | Icon | TextAndIcon),
                                  ;         combination of texture and text used to render items. Default TextAndIcon.
RandomColorTexture=randomicon.png ; string,  texture for the random color item (item 0).
DisabledItemTexture=              ; string,  texture for disabled items; defaults to a texture generated from DisabledItemColor.
ColorTextureHeight=               ; int,     height of the color icon in pixels.
ColorTextureWidth=                ; int,     width of the color icon in pixels.
```

### [XNAClientPreferredItemDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientPreferredItemDropDown.cs)

_(inherits [XNAClientDropDown](#xnaclientdropdown))_

```ini
[SOMEPREFERREDITEMDROPDOWN] ; XNAClientPreferredItemDropDown
PreferredItemLabel=         ; string, label appended to the preferred item's text.
```

### [XNAInteractionButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAInteractionButton.cs)

_(inherits [XNAClientButton](#xnaclientbutton))_

A button that interacts with files and processes on the local system.

```ini
[SOMEINTERACTIONBUTTON]               ; XNAInteractionButton
OpenFiles=                            ; comma/semicolon-separated strings, files to open with the OS shell when clicked.
ExitProcess=                          ; comma/semicolon-separated strings, process names to terminate when clicked.
TargetSuffixes=                       ; comma/semicolon-separated strings, allowed file extensions for OpenFiles;
                                      ;          a missing leading dot is added automatically.
                                      ;          Defaults to `.exe,.bat,.txt,.ini,.log`.
OpenDisableButtonTime=0               ; integer, seconds the button stays disabled after opening files. Default `0`.
ExitDisableButtonTime=0               ; integer, seconds the button stays disabled after exiting processes. Default `0`.
ProcessExists.AllowedButtonChecks=    ; comma-separated strings, the button is only enabled while at least one of these
                                      ;          processes is running.
ProcessExists.DisableButtonChecks=    ; comma-separated strings, the button is disabled while at least one of these
                                      ;          processes is running.
TextColorDisabled=                    ; color,   text color of the disabled button state.
```

### [XNAOptionsPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAOptionsPanel.cs)

_(inherits [XNAScrollPanel](#xnascrollpanel))_

```ini
[SOMEOPTIONSPANEL]  ; XNAOptionsPanel
EnableScrolling=true ; boolean, when `false` the inner scroll container is removed and the children are hosted directly.
```

`XNAOptionsPanel` parses `[Name]ExtraControls` by default and forwards INI attribute processing to the controls inside its scrollable content, so the `Setting*` controls listed under [XNAOptionsPanel Controls](#xnaoptionspanel-controls) can be declared as `$CC` children or extra controls.

---

## Dynamic Control Properties and Expressions

Dynamic control properties **can** use [constants](#constants) and [arithmetic expressions](#arithmetic-expressions). They can be used on any control whose window uses the INItializable or expression-enabled models (see [How INI Files Are Resolved](#how-ini-files-are-resolved)).

```ini
$X=10            ; integer, X location of the control. Value is parsed as an expression.
$Y=20            ; integer, Y location of the control.
$Width=50        ; integer, width of the control.
$Height=10       ; integer, height of the control.
$TextAnchor=LEFT ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER),
                 ;          text anchor of an XNALabel's draw box.
$AnchorPoint=0,0 ; 2 expressions, text start drawing point of an XNALabel. Each component is parsed as an expression.
$LeftClickAction=Disable ; action executed on left click. The only supported action is `Disable`, which disables
                 ;          and hides the control.
```

Example with constants:

```ini
[lblExample]
$X=MY_X_CONSTANT
$Y=MY_Y_CONSTANT
$Width=MY_WIDTH_CONSTANT
$Height=MY_HEIGHT_CONSTANT
```

> [!IMPORTANT]
> How `$` properties interact with their plain counterparts depends on the window model:
> - **XNAWindowBase model** (e.g. `XNAWindow`, `XNAOptionsPanel`): the expression pass runs **again after** the base attribute pass, so `$X`/`$Y`/`$Width`/`$Height` always win over `X=`/`Y=`/`Width=`/`Height=`.
> - **INItializableWindow model**: keys are applied in the order they appear in the INI file, so the last occurrence of a property wins.

---

## Late Attributes (Control Linking)

Late attributes link controls to each other after the whole control tree has been created, so a control may reference controls that are **defined later** in the INI file. They are processed by `ClientGUI/INIControlLinkHelper.cs` and apply to both `INItializableWindow` and `XNAWindowBase`-derived windows.

### Buttons & checkboxes — `$Toggles`, `$Opens`, `$Exits`

```ini
[btnToggle]        ; XNAButton / XNACheckBox
$Toggles=pnlExtra,btnSecondary   ; comma-separated control names.
$Opens=pnlDetail                 ; comma-separated control names, shown (Visible+Enabled) while the state is active.
$Exits=pnlHint                   ; comma-separated control names, hidden while the state is active.
```

Behavior:

- **Checkbox**: `CheckedChanged` fires whenever the checkbox state changes. `$Toggles` flips `Visible`/`Enabled` of each listed control; `$Opens` shows the listed controls while checked; `$Exits` hides them while checked (and vice versa). The initial state is applied from the checkbox's initial `Checked` value.
- **Button**: `$Toggles` flips `Visible`/`Enabled` on every click; `$Opens` shows and `$Exits` hides on click.
- Lookups are recursive within the control's enclosing window/panel, so targets defined anywhere in the window are found.

### Tab controls — `$ToggleN`, `$OpenN`, `$ExitN`

```ini
[tabOptions]         ; XNATabControl
$Toggle0=pnlTab0     ; controls visible/enabled only while tab 0 is selected.
$Toggle1=pnlTab1
$Open0=pnlCommon     ; controls forced visible while tab 0 is selected.
$Exit0=pnlLegacy     ; controls forced hidden while tab 0 is selected.
; Simple sequential form (index 0, 1, 2, ...):
$Toggles=pnlTab0,pnlTab1,pnlTab2
```

When the selected tab changes, all mapped controls are updated: a control is visible/enabled only if it is listed for the currently selected tab (`$ToggleN`/`$OpenN`), and controls listed in `$ExitN` are additionally hidden for that tab.

### Drop-downs — `$ToggleN`, `$OpenN`, `$ExitN`

The same mechanism applies to `XNADropDown` using the selected item index instead of the tab index:

```ini
[ddMode]             ; XNADropDown
$Toggle0=pnlMode0
$Toggle1=pnlMode1
$Toggles=pnlMode0,pnlMode1
```

### Text boxes — `NextControl`, `PreviousControl`

```ini
[tbUsername]   ; XNATextBox
NextControl=tbPassword

[tbPassword]
PreviousControl=tbUsername
NextControl=btnLogin
```

Pressing `Tab` moves focus to `NextControl`; pressing `Shift+Tab` moves focus to `PreviousControl`.

---

## Special Controls

Some controls are only available under specific circumstances.

### [CoopBriefingBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/CoopBriefingBox.cs)

_(inherits XNAPanel)_ — the co-op mission briefing panel of the game lobby.

```ini
; GameLobbyBase.ini
[MapPreviewBox_CoopBriefingBox]
FontIndex=0 ; integer, index of the font loaded from the font list.
```

### GameLobbyBase Controls

The following controls are only available as children of `GameLobbyBase` and derived controls.

In addition, the lobby window itself (`[GameLobbyBase]` / `[SkirmishLobby]` / `[MultiplayerGameLobby]` / `[CnCNetGameLobby]` sections, which chain to `GameLobbyBase.ini` via `[INISystem] BasedOn`) supports these layout keys, read directly from the window's own INI section with `ConfigIni.GetIntValue` (not via the control attribute parser):

```ini
[LOBBY_WINDOW]                 ; GameLobbyBase
PlayerOptionLocationX=25       ; integer, X coordinate of the player option controls.
PlayerOptionLocationY=24       ; integer, Y coordinate of the player option controls.
PlayerOptionVerticalMargin=    ; integer, vertical gap between the player option rows.
PlayerOptionHorizontalMargin=  ; integer, horizontal gap between the player option controls.
PlayerOptionCaptionLocationY=6 ; integer, Y coordinate of the option captions.
PlayerNameWidth=136            ; integer, width of the player name control.
SideWidth=91                   ; integer, width of the side selector.
ColorWidth=79                  ; integer, width of the color selector.
StartWidth=49                  ; integer, width of the starting location selector.
TeamWidth=46                   ; integer, width of the team selector.
PlayerStatusIndicatorX=3       ; integer, X offset of the player status indicator (`[MultiplayerGameLobby]`).
PlayerStatusIndicatorY=0       ; integer, Y offset of the player status indicator (`[MultiplayerGameLobby]`).
```

#### [GameSessionCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/GameSessionCheckBox.cs)

_(inherits [XNAClientCheckBox](#xnaclientcheckbox))_

Game option checkbox for the game lobby. Supports broadcasting game options to the CnCNet lobby and displaying them in the game list and filters.

```ini
[SOMEGAMESESSIONCHECKBOX]                  ; GameSessionCheckBox
OptionName=                                ; string,  display name of this option (used in the game information panel).
SpawnIniOption=                            ; string,  spawn INI option written when the checkbox state changes. Indexed
                                           ;          variants `SpawnIniOption0`, `SpawnIniOption1`, ... are supported.
SpawnIniProject=Settings                   ; string,  spawn INI section the option is written to. Default `Settings`.
                                           ;          Indexed variants `SpawnIniProject0`, `SpawnIniProject1`, ... are
                                           ;          supported.
EnabledSpawnIniValue=True                  ; string,  spawn INI value when checked. Defaults to `True`. Indexed variants
                                           ;          `EnabledSpawnIniValueN` supported.
DisabledSpawnIniValue=False                ; string,  spawn INI value when unchecked. Defaults to `False`. Indexed
                                           ;          variants `DisabledSpawnIniValueN` supported.
CustomIniPath=                             ; string,  custom INI path for map-specific settings (indexed variants supported).
SpawnWriteCustom=false                     ; boolean, writes the option to the map INI (spawnmap.ini) instead of spawn.ini.
                                           ;          Indexed variants `SpawnWriteCustomN` supported.
CustomWriteSpawn=false                     ; boolean, inverse of SpawnWriteCustom (kept for compatibility). Indexed
                                           ;          variants `CustomWriteSpawnN` supported.
SpawnIniValueCheck=false                   ; boolean, when `true` an empty value is not written to spawn.ini. Indexed
                                           ;          variants `SpawnIniValueCheckN` supported.
Reversed=false                             ; boolean, reverses the checkbox behavior.
Checked=false                              ; boolean, initial checked state.
MapScoringMode=Irrelevant                  ; enum (Irrelevant | DenyWhenChecked | DenyWhenUnchecked),
                                           ;          controls whether the setting affects map scoring.
BroadcastToLobby=false                     ; boolean, include this checkbox in the GAME broadcast to the CnCNet lobby.
ShowInGameList=false                       ; boolean, show icon/text in the game list.
ShowInGameListOnRight=false                ; boolean, show icon on the right side of the game list. Only applies if
                                           ;          `ShowInGameList` is `true`.
ShowInGameInformationPanel=false           ; boolean, show icon/text in the game information panel.
ShowInGameInformationPanelAsIconOnly=false ; boolean, show only the icon in the game information panel. Only applies if
                                           ;          `ShowInGameInformationPanel` is `true`.
ShowIconInGameLobby=false                  ; boolean, show icon in the game lobby control.
ShowInFilters=false                        ; boolean, show this setting in the filters panel.
EnabledIcon=                               ; string,  texture name for the icon when the setting is enabled.
DisabledIcon=                              ; string,  texture name for the icon when the setting is disabled.
SortOrder=0                                ; integer, display order for icons in the game information panel and game list.
                                           ;          Lower values appear first.
ParentCheckBoxName=                        ; string,  name of a parent checkbox (single-parent form). When this value
                                           ;          contains no comma, single-parent gate semantics apply (see below).
ParentCheckBoxName=chkA,chkB              ; comma-separated list of parent checkbox names, stored as indices 0, 1, 2, ...
                                           ;          (indexed-parent semantics, see below). Equivalent to writing
                                           ;          ParentCheckBoxName0=chkA, ParentCheckBoxName1=chkB, ...
ParentCheckBoxNameN=                       ; string,  indexed form: parent checkbox name for index `N` (N = 0, 1, 2, ...).
ParentCheckBoxRequiredValue=true           ; boolean, state required from the parent checkbox. Single-parent form: applies
                                           ;          to the single parent. Indexed form: comma-separated values are mapped
                                           ;          to the indexed parents in order (a single value is copied to all of
                                           ;          them; the last value fills in missing entries); also available as the
                                           ;          indexed key ParentCheckBoxRequiredValueN. Unspecified indices default
                                           ;          to `true`.
ParentCheckBoxTexture=checkedTex,uncheckedTex ; string, texture pair "checked,unchecked" (or a single texture applied to
                                           ;          both states) shown on THIS checkbox while it is disabled by its parent
                                           ;          dependency. Also available as the indexed key ParentCheckBoxTextureN.
ParentChecked=false                        ; boolean, checked state shown on this checkbox while it is disabled by its
                                           ;          parent dependency. Default `false`. Also available as the indexed key
                                           ;          ParentCheckedN.
```

Parent-dependency semantics:

- **Single-parent form** (`ParentCheckBoxName=chkX`, no comma): *gate* semantics. The parent checkbox must be in the
  state required by `ParentCheckBoxRequiredValue` for this checkbox to be changeable; otherwise this checkbox is
  disabled, forced to the `ParentChecked` state, and displayed with the `ParentCheckBoxTexture` textures.
- **Indexed form** (comma-separated `ParentCheckBoxName` list or `ParentCheckBoxName{N}` keys): *lock* semantics. When
  **all** indexed parent checkboxes are in their required states (a parent that cannot be found counts as not
  matching; an index without an explicit `ParentCheckBoxRequiredValue{N}` defaults to `true`), this checkbox is locked
  — it cannot be changed — its checked state is taken from the **lowest** configured index's `ParentChecked{N}`
  (falling back to the plain `ParentChecked`) and it is displayed with that index's `ParentCheckBoxTexture{N}`
  (falling back to the global texture pair). As soon as any parent does not match, the checkbox is freely changeable.

#### [CampaignCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignCheckBox.cs)

_(inherits [GameSessionCheckBox](#gamesessioncheckbox))_

Use this control type for campaign checkboxes in `CampaignSelector.ini`. Inherits all properties from `GameSessionCheckBox`. Note: `CustomIniPath` throws an exception on campaign checkboxes when `CopyMissionsToSpawnmapINI` is disabled in `ClientDefinitions.ini`.

```ini
[SOMECAMPAIGNCHECKBOX]                  ; CampaignCheckBox
ResetToDefaultOnGameExit=false          ; boolean, reset the checkbox to its default value when the game exits.
```

#### [GameLobbyCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyCheckBox.cs)

_(inherits [GameSessionCheckBox](#gamesessioncheckbox))_

Use this control type for game lobby checkboxes in `GameLobbyBase.ini`. Inherits all properties from `GameSessionCheckBox`.

```ini
[SOMEGAMELOBBYCHECKBOX]           ; GameLobbyCheckBox
CheckedMP=false                   ; boolean, checked state applied in multiplayer games specifically.
DisallowedSideIndex=0,1           ; comma-separated integers, side indices that cannot be selected while this
                                  ;          checkbox is checked. Alias: DisallowedSideIndices.
```

#### [GameSessionDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/GameSessionDropDown.cs)

_(inherits [XNAClientDropDown](#xnaclientdropdown))_

Game option dropdown for the game lobby. Supports broadcasting game options to the CnCNet lobby and displaying them in the game list and filters.

```ini
[SOMEGAMESESSIONDROPDOWN]                  ; GameSessionDropDown
Items=                                     ; comma-separated strings, item values (tags) for the dropdown.
ItemLabels=                                ; comma-separated strings, optional display labels for the items.
Icons=                                     ; comma-separated strings, texture names for the item icons. Should match the
                                           ;          number of items.
ItemN=                                     ; string, tag of the item at index `N` (overrides Items entry).
ItemLabelN=                                ; string, display label of the item at index `N`.
SpawnIniOption=                            ; string,  spawn INI option written based on the selected item. Indexed
                                           ;          variants `SpawnIniOption0`, ... are supported.
SpawnIniProject=Settings                   ; string,  spawn INI section the option is written to. Default `Settings`.
                                           ;          Indexed variants `SpawnIniProjectN` supported.
SpawnWriteCustom=false                     ; boolean, writes the option to the map INI (spawnmap.ini) instead of spawn.ini.
                                           ;          Indexed variants `SpawnWriteCustomN` supported.
SpawnIniValueCheck=false                   ; boolean, when `true` an empty value is not written to spawn.ini. Indexed
                                           ;          variants `SpawnIniValueCheckN` supported.
DefaultIndex=0                             ; integer, default selected item index.
DataWriteMode=BOOLEAN                      ; enum (INDEX | BOOLEAN | STRING | MAPCODE),
                                           ;          how the value is written to the spawn INI:
                                           ;          INDEX - the selected index, BOOLEAN - `SelectedIndex > 0`,
                                           ;          STRING - the selected item's tag, MAPCODE - applies the item's tag
                                           ;          as a map code INI name via MapCodeHelper. Default `BOOLEAN`.
OptionName=                                ; string,  display name for this option.
BroadcastToLobby=false                     ; boolean, include this dropdown in the GAME broadcast to the CnCNet lobby.
ShowInGameList=false                       ; boolean, show icon/text in the game list.
ShowInGameListOnRight=false                ; boolean, show icon on the right side of the game list. Only applies if
                                           ;          `ShowInGameList` is `true`.
ShowInGameInformationPanel=false           ; boolean, show icon/text in the game information panel.
ShowInGameInformationPanelAsIconOnly=false ; boolean, show only the icon in the game information panel. Only applies if
                                           ;          `ShowInGameInformationPanel` is `true`.
ShowIconInGameLobby=false                  ; boolean, show icon in the game lobby control.
ShowInFilters=false                        ; boolean, show this setting in the filters panel.
SortOrder=0                                ; integer, display order for icons in the game information panel and game list.
                                           ;          Lower values appear first.
EnableRightInputBox=false                  ; boolean, adds a custom-value input box that opens when right-clicking the
                                           ;          dropdown. When `true`, custom items are appended to the item list.
InputBoxDataMode=INTEGER                   ; enum (INTEGER | STRING), data mode of the custom input box.
InputBoxIntegerScroll=true                 ; boolean, master switch for integer scrolling in the input box (mouse and keyboard).
InputBoxIntegerScroll.Mouse=true           ; boolean, enables scrolling the integer with the mouse wheel.
InputBoxIntegerScroll.KeyBoard=true        ; boolean, enables scrolling the integer with the up/down arrow keys.
InputBoxIntegerScroll.Integer=1            ; integer, scroll step (mouse and keyboard).
InputBoxIntegerScroll.MouseInteger=1       ; integer, mouse wheel scroll step.
InputBoxIntegerScroll.KeyBoardInteger=1    ; integer, keyboard scroll step.
InputBoxIntegerStrict=false                ; boolean, when `true` input outside [MinInputBoxInteger, MaxInputBoxInteger]
                                           ;          is rejected while typing.
InputBoxIntegerRange=                      ; string,  sign range of accepted integers. A value containing `-` allows
                                           ;          negatives, a value containing `+` allows positives; e.g. `-+` allows
                                           ;          both, `-` only negatives, `+` only positives.
InputBoxIntegerRangeShow.Positive=false    ; boolean, shows a `+` prefix for positive custom values.
InputBoxIntegerRangeShow.Negative=true     ; boolean, shows a `-` prefix for negative custom values.
MinInputBoxInteger=                        ; integer, minimum accepted integer (clamped on confirm). Default int.MinValue.
MaxInputBoxInteger=                        ; integer, maximum accepted integer (clamped on confirm). Default int.MaxValue.
InputBoxCustomItems=1                      ; integer, number of custom value slots appended to the item list.
InputBoxCustomItemsLabels=                 ; comma-separated strings, labels of the custom slots (may contain `{0}` for the value).
InputBoxCustomDefaultItems=                ; comma-separated strings, default values of the custom slots.
```

#### [CampaignDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignDropDown.cs)

_(inherits [GameSessionDropDown](#gamesessiondropdown))_

Use this control type for campaign dropdowns in `CampaignSelector.ini`. Inherits all properties from `GameSessionDropDown`. `DataWriteMode=MAPCODE` is not supported for campaign missions.

#### [GameLobbyDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyDropDown.cs)

_(inherits [GameSessionDropDown](#gamesessiondropdown))_

Use this control type for game lobby dropdowns in `GameLobbyBase.ini`. Inherits all properties from `GameSessionDropDown`; `DefaultIndex` is additionally synchronized between host and client users.

### XNAOptionsPanel Controls

The following controls are only available as children of `XNAOptionsPanel` and derived controls. They persist their state to the client's settings INI (`SettingsFile`, default `Settings.ini`).

#### [SettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingCheckBox.cs)

_(inherits [XNAClientCheckBox](#xnaclientcheckbox))_

```ini
[SOMESETTINGCHECKBOX]            ; SettingCheckBox
Checked=false                    ; boolean, initial checked state (takes priority over DefaultValue).
DefaultValue=false               ; boolean, default state of the checkbox. Used when `Checked` is not set.
SettingSection=CustomSettings    ; string,  section of the settings INI the setting is saved to. Default `CustomSettings`.
SettingKey=                      ; string,  key of the settings INI the setting is saved to. Defaults to
                                 ;          `CONTROLNAME_Value` when `WriteSettingValue` is set, otherwise
                                 ;          `CONTROLNAME_Checked`.
WriteSettingValue=false          ; boolean, writes a specific string value instead of the checked state.
EnabledSettingValue=             ; string,  value written when `WriteSettingValue` is set and the checkbox is checked.
DisabledSettingValue=            ; string,  value written when `WriteSettingValue` is set and the checkbox is not checked.
RestartRequired=false            ; boolean, whether applying this setting requires a client restart.
ParentCheckBoxName=              ; string,  name of a checkbox (same parent) that must be in the required state for this
                                 ;          checkbox to be enabled.
ParentCheckBoxRequiredValue=true ; boolean, state required from the parent checkbox.
ResetToDefaultOnGameExit=false   ; boolean, reset the setting to its default value when the game exits.
```

#### [FileSettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingCheckBox.cs)

_(inherits [XNAClientCheckBox](#xnaclientcheckbox))_

File-setting checkbox that copies files when its state changes.

```ini
[SOMEFILESETTINGCHECKBOX]        ; FileSettingCheckBox
Checked=false                    ; boolean, alias of `DefaultValue`; both keys write the same default-state property
                                 ;          (no precedence — the last key in the INI wins).
DefaultValue=false               ; boolean, default state of the checkbox, used when the setting has no saved value.
SettingSection=CustomSettings    ; string,  section of the settings INI the setting is saved to. Default `CustomSettings`.
SettingKey=                      ; string,  key of the settings INI the setting is saved to. Defaults to
                                 ;          `CONTROLNAME_Value` when `WriteSettingValue` is set, otherwise
                                 ;          `CONTROLNAME_Checked`.
RestartRequired=false            ; boolean, whether applying this setting requires a client restart.
ParentCheckBoxName=              ; string,  name of a parent checkbox (same parent) that gates this checkbox.
ParentCheckBoxRequiredValue=true ; boolean, state required from the parent checkbox.
CheckAvailability=false          ; boolean, whether the checkbox can be (un)checked depends on whether the files to copy
                                 ;          are actually present.
ResetUnavailableValue=false      ; boolean, when set together with `CheckAvailability`, a checkbox set to an unavailable
                                 ;          value is reset back to `DefaultValue`.
Reversed=false                   ; boolean, reverses the checkbox behavior (kept for compatibility).
EnabledFileN=                    ; comma-separated strings, files to copy when the checkbox is checked. `N` starts from 0
                                 ;          and increments until no value is found. Format:
                                 ;          source path relative to the game root, destination path relative to the game
                                 ;          root, and an optional file operation option (see
                                 ;          [Appendix: File Operation Options](#appendix-file-operation-options)). The
                                 ;          source and destination may omit a shared prefix that is provided by the
                                 ;          `CopyFilePath` / `PasteFilePath` keys.
DisabledFileN=                   ; comma-separated strings, files to copy when the checkbox is not checked. Same format
                                 ;          as `EnabledFileN`.
CopyFilePath=                    ; string,  base source path (relative to the game root) prepended to the source of every
                                 ;          file entry in this section (`EnabledFileN`, `DisabledFileN`, legacy `FileN`).
                                 ;          Default: none.
PasteFilePath=                   ; string,  base destination path (relative to the game root) prepended to the
                                 ;          destination of every file entry in this section. Independent from
                                 ;          `CopyFilePath`; either key may be set alone. Default: none.
```

#### [SettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingDropDown.cs)

_(inherits [XNAClientDropDown](#xnaclientdropdown))_

```ini
[SOMESETTINGDROPDOWN]  ; SettingDropDown
Items=                 ; comma-separated strings, items to display on the dropdown.
DefaultValue=0         ; integer, default item index.
SettingSection=        ; string,  section of the settings INI the setting is saved to. Default `CustomSettings`.
SettingKey=            ; string,  key of the settings INI the setting is saved to. Defaults to
                       ;          `CONTROLNAME_Value` when `WriteItemValue` is set, otherwise `CONTROLNAME_SelectedIndex`.
WriteItemValue=false   ; boolean, writes the selected item's value (tag) to the setting INI key instead of the index.
RestartRequired=false  ; boolean, whether applying this setting requires a client restart.
```

#### [FileSettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingDropDown.cs)

_(inherits [XNAClientDropDown](#xnaclientdropdown))_

```ini
[SOMEFILESETTINGDROPDOWN]            ; FileSettingDropDown
Items=                               ; comma-separated strings, items to display on the dropdown.
DefaultValue=0                       ; integer, default item index.
SettingSection=CustomSettings        ; string,  section of the settings INI the setting is saved to.
SettingKey=CONTROLNAME_SelectedIndex ; string,  key of the settings INI the setting is saved to.
RestartRequired=false                ; boolean, whether applying this setting requires a client restart.
CheckAvailability=false              ; boolean, whether selecting an item depends on whether the files to copy are present.
ResetUnavailableValue=false          ; boolean, adjusts the setting value automatically when the current value becomes
                                     ;          unavailable.
ItemXFileN=                          ; comma-separated strings, files to copy when dropdown item `X` is selected.
                                     ;          `N` starts from 0 and increments until no value is found. Same format as
                                     ;          `EnabledFileN` (see [Appendix: File Operation Options](#appendix-file-operation-options)).
CopyFilePath=                        ; string,  base source path (relative to the game root) prepended to the source of
                                     ;          every `ItemXFileN` entry (all items share the same base). Default: none.
PasteFilePath=                       ; string,  base destination path (relative to the game root) prepended to the
                                     ;          destination of every `ItemXFileN` entry. Default: none.
```

#### Appendix: File Operation Options

Valid file operation options for the files defined by `FileSettingCheckBox` and `FileSettingDropDown` (enum `FileOperationOption` in `FileSourceDestinationInfo.cs`):

| Option | Behavior |
|---|---|
| `AlwaysOverwrite` | Always overwrites the destination file with the source file. |
| `OverwriteOnMismatch` | Overwrites the destination file only if the two files differ. |
| `DontOverwrite` | Never overwrites the destination file if it is already present. |
| `KeepChanges` | Caches the destination file so that user-made changes survive disabling and re-enabling the option. |
| `AlwaysOverwrite_LinkAsReadOnly` | Attempts to create a hard link (shared content) to the source file, falling back to a copy if linking fails. Recommended for binary files such as `opengl32.dll`, `d3d9.dll`, `dxgi.dll`; not recommended for text files. While the link exists, both the source and the target are marked read-only. |

The optional `CopyFilePath` and `PasteFilePath` keys reduce repetition when several entries share the same directory: they define section-level base paths (relative to the game root) that are prepended to the source / destination of every entry respectively. The entry format then only contains the path remainder. Example:

```ini
[ReShadeSelection]
CopyFilePath=Resources/ReShade Files
Item0File0=dxgi.dll,dxgi.dll,AlwaysOverwrite_LinkAsReadOnly
Item1File0=d3d9.dll,d3d9.dll,AlwaysOverwrite_LinkAsReadOnly
```

This resolves the source of `Item0File0` to `Resources/ReShade Files/dxgi.dll` and its destination to `dxgi.dll` (game root). Both keys are optional, apply to all file entries of the section, and are independent of each other.

---

## Windows

Children of [XNAWindow](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindow.cs) that define their own INI sections.

### [LoadingScreen](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/LoadingScreen.cs)

```ini
; LoadingScreen.ini
[LoadingScreen]
RandomBackgroundTextures=  ; comma-separated list of strings,
                           ; paths of files to use randomly as BackgroundTexture.
RandomBackgroundTexturesPath= ; string, optional sub-path prefix for the random background textures. When empty or
                           ;          literally `Resources`, no prefix is added; otherwise the prefix is prepended
                           ;          (e.g. `Themes/MyTheme`).
```

The default loading screen texture is loaded from `loadingscreen.png`; when `RandomBackgroundTextures` is non-empty,
one of the listed textures is chosen at random and overrides it. The window falls back to `GenericWindow.ini`'s
`[GenericWindow]` section when `[LoadingScreen]` is missing.

### [MainMenu](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/MainMenu.cs)

Background video settings (Windows engines only). All keys are read from the `[MainMenu]` section of `MainMenu.ini`.

```ini
[MainMenu]
BackgroundVideo=                 ; string,  path to the background video file, resolved with the same priority as the
                                 ;          main menu theme ([MainMenu] key > [General] MainMenuThemePath > default
                                 ;          MainMenu/mainmenubg.mp4).
BackgroundVideoLooping=true      ; boolean, whether the video loops (default true).
BackgroundVideoMuted=false       ; boolean, whether the theme's video has no audio track. When true the video can never
                                 ;          take audio priority over the menu music.
BackgroundVideoVolume=100        ; float,   video volume in percent, multiplied by the client volume setting (default 100).
BackgroundVideoFrameInterval=33  ; integer, minimum milliseconds between captured frames; lower is smoother but costs
                                 ;          more CPU (default 33 = ~30fps).
BackgroundMusic=                 ; string,  path to the menu music file, resolved with the same priority as
                                 ;          BackgroundVideo.
BackgroundVideoHotkeys=true      ; boolean, master switch for the video hotkeys below (default true).
BackgroundVideoPauseHotkey=P     ; key,     toggles the video pause/resume state while the main menu is the active
                                 ;          window (default P). Set to None to disable.
BackgroundVideoMuteHotkey=V      ; key,     toggles the video audio mute state (default V). Set to None to disable.
```

> [!NOTE]
> `BackgroundVideoAlpha` and `BackgroundVideoAutoPlay` are parsed from this section but **currently unused** by the
> implementation — the video background is constructed without alpha or auto-play parameters, so setting them has no
> effect. Whether the video plays is controlled by the user setting `EnableBackgroundVideo` (see below).

Audio priority is driven by two **user settings** (persisted in `Settings.ini` via `UserINISettings`, not in
`MainMenu.ini`):

- `PlayMainMenuMusic` — `[Audio]` section, default `true`.
- `EnableBackgroundVideoSound` — `[Video]` section, default `false`. When `false`, the video is treated as muted.
- `EnableBackgroundVideo` — `[Video]` section, default `false`. Master switch for the background video itself.

Notes:

- The pause and mute hotkeys are plain keys (no modifier required, consistent with the main menu button hotkeys) and only work while the main menu is the focused input window and no game is running. Avoid values already used by the main menu buttons (`C`, `L`, `S`, `M`, `N`, `O`, `E`, `T`, `R`, `X`) or by `TopBar` (`F1`-`F4`, `F12`).
- While the game runs, the background video is automatically paused (and its audio faded out) to save resources; it resumes on return to the main menu. A pause made manually with the hotkey is remembered and is not overridden by the automatic resume.
- Muting the video audio via the hotkey lets the menu music take over (video yields priority); unmuting fades the menu music out again. The choice is remembered until the `EnableBackgroundVideoSound` or `PlayMainMenuMusic` setting is changed.

### [CampaignSelector](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)

#### Mission Properties

The following keys are supported in mission sections in `Battle.ini` and in `[ClientMissionConfig]` sections of custom mission map files:

```ini
[MISSION_SECTION]                    ; Mission
CD=0                                 ; integer, CD number.
Side=0                               ; integer, side index for the mission.
Scenario=                            ; string,  relative path to the map file.
Description=Undefined mission        ; string,  mission display name. Supports localization.
SideName=                            ; string,  mission icon asset prefix, not a full path. The client appends
                                     ;          `icon.png` when loading it (for example, `SideName=GDI` loads
                                     ;          `GDIicon.png`).
LongDescription=                     ; string,  mission description text. Supports localization and line breaks via `@`.
FinalMovie=none                      ; string,  movie to play after mission completion.
RequiredAddon=false                  ; boolean, whether the mission requires the expansion. Default: `true` for YR/Ares
                                     ;          clients (the value is written inverted as `Ra2Mode`), `false` otherwise
                                     ;          (written as `Firestorm` for TS).
Enabled=true                         ; boolean, whether the mission is selectable.
BuildOffAlly=false                   ; boolean, whether the player can build off ally structures.
PlayerAlwaysOnNormalDifficulty=false ; boolean, forces the human player to Normal difficulty regardless of the
                                     ;          difficulty slider.
Tags=                                ; comma-separated strings, tags for filtering in the campaign tag selector. Custom
                                     ;          missions always get the `CUSTOM` tag.
PreviewImage=                        ; string,  path relative to `Resources/Mission Previews/` for the mission preview
                                     ;          image.
ScenarioMapINI=                      ; string,  parsed as a boolean; overrides the client configuration
                                     ;          `CopyMissionsToSpawnmapINI` for this mission. Empty (default) falls back
                                     ;          to the configured value.
Supplement=                          ; boolean, overrides `CustomMissionSupplementEnable.Battle` for this mission. Default:
                                     ;          the `CustomMissionSupplementEnable.Battle` configuration value (for custom
                                     ;          missions: `CustomMissionSupplementEnable.Custom`).
MissionSpawnIniOptions=              ; string,  name of a section in `INI/Battle.ini`. When the mission is played, every
                                     ;          key-value pair of that section is written into the `[spawnmap.ini]`
                                     ;          section of `spawn.ini`. Default: empty (no section applied). For custom
                                     ;          missions this key is read from `[ClientMissionConfig]`, but the referenced
                                     ;          section is still resolved from `INI/Battle.ini`, not from the map file.
```

The `[Battles]` section of `Battle.ini` maps list entries to mission sections:

```ini
[Battles]
0=MISSION_SECTION
1=ANOTHER_MISSION
```

#### Custom Mission Map Files

Custom mission `.map` files placed in the `CustomMissionPath` directory (see [ClientDefinitions.ini](#clientdefinitionsini)) are scanned for two INI sections:

- **`[ClientMissionConfig]`** — **required** for the map to be recognized as a custom mission. Supports the client-facing keys from [Mission Properties](#mission-properties), except that for custom missions `Scenario` is derived from the `.map` filename/path and `Tags` is always set to `CUSTOM`.
- **`[GameMissionConfig]`** — **optional**. Key-value pairs are written to `spawn.ini` at launch time. Used for loading screen configuration and other engine-level settings. Empty `LS640BkgdName`/`LS800BkgdName`/`LS800BkgdPal` values are skipped, and `Settings.ReadMissionSection=Yes` is set when loading screen keys are present.

```ini
; In a custom mission .map file

[ClientMissionConfig]
Description=My Custom Mission
Side=0
Enabled=true

[GameMissionConfig]
; Optional. Written to spawn.ini at launch.
; If loading screen keys are present, ReadMissionSection=Yes is set in spawn.ini.
```

If `[GameMissionConfig]` is not present or does not specify loading screen keys, the client automatically looks for `.shp` and `.pal` supplement files as fallback loading screen assets.

Note: supplemental mission files must be configured in `ClientDefinitions.ini` using `CustomMissionPath` together with `CustomMissionSupplementFileNExtension` and `CustomMissionSupplementFileNCopyAs` as sequential `(extension, copy-as filename)` pairs, where `N` refers to a sequential number.

#### [pnlMissionPreview](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)

_(inherits [XNAPanel](#xnapanel))_

You can set a preview image for each mission in the campaign selector, known as the mission preview panel.

To activate this feature, create a `Mission Previews` folder inside the `Resources` folder, then put an image of your desire inside and rename it as `Default.png`.

To adjust the panel size and position, modify `pnlMissionPreview` in `CampaignSelector.ini`. It inherits all properties from `XNAPanel`.

```ini
[pnlMissionPreview]          ; XNAPanel
...
```

To configure which preview image in the `Resources/Mission Previews` folder to use for each mission, add the `PreviewImage` property in the mission's section in `Battle.ini` (or `[ClientMissionConfig]` for custom missions) and set its value to the path of the image file relative to the `Resources/Mission Previews` folder:

```ini
[YourMissionSection]
PreviewImage= ; string, path to the image file relative to `Resources/Mission Previews`.
```

If `PreviewImage` is not set for a mission, `Resources/Mission Previews/Default.png` is used as default.

#### [tbMissionDescription](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)

_(inherits [XNATextBlock](#xnatextblock))_

This control shows the mission description in the campaign selector. Note that, when the mission preview panel is active, the *default* size of the mission description text block is changed automatically.

To adjust the text block size and position, modify `tbMissionDescription` in `CampaignSelector.ini`. It inherits all properties from `XNATextBlock`.

```ini
[tbMissionDescription]       ; XNATextBlock
...
```

#### Campaign Tag Selector

When `CampaignTagSelectorEnabled=true` in `ClientDefinitions.ini`, a tag selector window appears before the campaign selector. It allows players to filter missions by their `Tags` property.

Define tag buttons in `CampaignTagSelector.ini` using the naming pattern `ButtonTag_{TagName}`:

```ini
[CampaignTagSelector]
$CC00=ButtonTag_Story:XNAClientButton
$CC01=ButtonTag_Challenge:XNAClientButton
$CC02=btnShowAllMission:XNAClientButton
$CC03=btnCancel:XNAClientButton

[ButtonTag_Story]
; Button properties...

[ButtonTag_Challenge]
; Button properties...

[btnShowAllMission]
; "Show All Missions" button - shows all missions regardless of tags.
```

The tag name in `ButtonTag_{TagName}` is matched against mission `Tags` values. Custom missions automatically receive the `CUSTOM` tag.

#### Campaign Game Options and Forced Spawn Options

`CampaignCheckBox` and `CampaignDropDown` controls can be added to `CampaignSelector.ini` to provide player-selectable options for campaign missions. See [GameSessionCheckBox](#gamesessioncheckbox) and [GameSessionDropDown](#gamesessiondropdown).

A `[CampaignForcedSpawnIniOptions]` section in `GameOptions.ini` defines keys that are always written to `spawn.ini` for campaign missions, regardless of UI options. This is separate from the multiplayer `[ForcedSpawnIniOptions]` section. See [GameOptions.ini](#gameoptionsini).

---

## Global Config Files

### ClientDefinitions.ini

The `ClientDefinitions.ini` file defines the client's global settings: game type, recommended resolutions, executable used to launch the game, and more. It is read by `ClientCore/ClientConfiguration.cs`.

#### `[Settings]`

```ini
[Settings]
ClientGameType=                      ; string,  client type used for game-specific behavior (e.g. RA/YR/TS/DTA).
LocalGame=                           ; string,  game identifier (default "DTA").
WindowTitle=                         ; string,  game window title. Supports localization.
GameExecutableNames=Game.exe         ; comma-separated strings, game executables to look for.
GameLauncherExecutableName=          ; string,  executable that launches the game client.
UnixGameExecutableName=wine-dta.sh   ; string,  game executable used on Linux/macOS.
LauncherExe=                         ; string,  executable that selects the correct main client executable.
ModMode=false                        ; boolean, enables mod-specific behavior.
RegistryInstallPath=TiberianSun      ; string,  registry key under which the game installation path is stored.
LongGameName=Tiberian Sun            ; string,  long display name of the game.
MaxNameLength=16                     ; integer, maximum length of player names.
RequiredFiles=                       ; comma-separated strings, files that must exist to play.
ForbiddenFiles=                      ; comma-separated strings, files that block the game from running.
MapFileExtension=map                 ; string,  extension of main map files.
SupplementalMapFileExtensions=       ; comma-separated strings, supplemental map files to copy when creating
                                     ;          spawnmap.ini (e.g. bin,mix).
DiscordAppId=                        ; string,  Discord application ID for Rich Presence. Empty disables it.
DisableDiscordIntegration=false      ; boolean, (NetworkDefinitions.ini) disables Discord integration.
SendSleep=2500                       ; integer, network send sleep in milliseconds.
LoadingScreenCount=2                 ; integer, number of loading screens to cycle through.
SidebarHack=false                    ; boolean, enables the sidebar compatibility hack.
UseIsometricCells=true               ; boolean, whether the map preview uses isometric cells.
WaypointCoefficient=128              ; integer, waypoint coordinate coefficient.
MapCellSizeX=48                      ; integer, map cell width for the map preview.
MapCellSizeY=24                      ; integer, map cell height for the map preview.
UseBuiltStatistic=false              ; boolean, writes built structures to the statistics log.
StatisticsLogFileName=DTA.LOG        ; string,  name of the statistics log file.
WindowedModeKey=Video.Windowed      ; string,  INI key toggled to switch the game to windowed mode.
MinimumRenderWidth=1280              ; integer, minimum client render width.
MinimumRenderHeight=768              ; integer, minimum client render height.
MaximumRenderWidth=1280              ; integer, maximum client render width.
MaximumRenderHeight=800              ; integer, maximum client render height.
AllowClientMinimumRenderIndefinitely=false ; boolean, keeps the client at the minimum render size when the
                                     ;          system cannot handle the maximum.
RecommendedResolutions=              ; comma-separated strings, resolutions shown in the display options. Defaults to
                                     ;          `{MinimumRenderWidth}x{MinimumRenderHeight},{MaximumRenderWidth}x{MaximumRenderHeight}`.
MinimumIngameWidth=640               ; integer, minimum in-game resolution width.
MinimumIngameHeight=480              ; integer, minimum in-game resolution height.
MaximumIngameWidth=                  ; integer, maximum in-game resolution width (default int.MaxValue).
MaximumIngameHeight=                 ; integer, maximum in-game resolution height (default int.MaxValue).
CustomIngameResolutions=             ; comma-separated strings, additional in-game resolutions.
CopyResolutionDependentLanguageDLL=true ; boolean, copies the resolution-dependent language DLL.
SettingsFile=Settings.ini            ; string,  name of the settings INI written by the settings controls.
MPMapsPath=INI/MPMaps.ini            ; string,  path to the multiplayer maps INI.
KeyboardINI=Keyboard.ini             ; string,  game keyboard INI file written by the hotkey configuration.
KeyboardHotkeySection=               ; string,  section of the keyboard INI used for hotkeys. Defaults to "WinHotKeys"
                                     ;          for RA-type clients, "Hotkey" otherwise.
ExtraCommandLineParams=              ; string,  extra command line parameters passed to the game executable.
BattleFSFileName=BattleFS.ini        ; string,  name of the battle file system INI.
MapEditorExePath=FinalSun/FinalSun.exe ; string, path of the map editor executable (Windows).
UnixMapEditorExePath=                ; string,  path of the map editor executable (Unix). Defaults to MapEditorExePath.
FSIniPath=FinalSun/FinalSun.ini      ; string,  path of the map editor settings INI.
TrustedDomains=                      ; comma-separated strings, domains that may be opened in the default browser
                                     ;          without a confirmation dialog. Example: cncnet.org,github.com,moddb.com.
LongSupportURL=                      ; string,  support URL.
ShortSupportURL=                     ; string,  short support URL.
ChangelogURL=                        ; string,  change log URL.
CreditsURL=                          ; string,  credits URL.
ManualDownloadURL=                   ; string,  manual download URL.
ShowDevelopmentBuildWarnings=true    ; boolean, shows warnings for development builds.
ShowGameIconInGameList=true          ; boolean, shows the game icon in the game listing.
SaveSkirmishGameOptions=false        ; boolean, saves previously used skirmish game options across sessions.
SaveCampaignGameOptions=false        ; boolean, saves previously used campaign game options across sessions.
CreateSavedGamesDirectory=false      ; boolean, creates the saved games directory.
DisableMultiplayerGameLoading=false  ; boolean, disables the multiplayer game loading screen.
DisplayPlayerCountInTopBar=false     ; boolean, shows the player count in the top bar.
ReturnToMainMenuOnMissionLaunch=true ; boolean, returns to the main menu when launching a mission.
CampaignTagSelectorEnabled=false     ; boolean, enables the campaign tag selector window.
CampaignGameSpeedControlEnable=false ; boolean, enables the game speed slider (1-7) in the campaign selector.
UseNetPlayerSameNameRecognition=true ; boolean, enables same-name recognition for net players.
CustomMissionPath=Maps/CustomMissions ; string, folder containing fan-made maps.
CustomMissionSupplementEnable=true   ; boolean, master switch for copying custom mission supplement files.
CustomMissionSupplementEnable.Battle=true ; boolean, switch for Battle.ini missions. Defaults to CustomMissionSupplementEnable.
CustomMissionSupplementEnable.Custom=true  ; boolean, switch for custom missions. Defaults to CustomMissionSupplementEnable.
CustomMissionSupplementFile0Extension=csf ; string, extension of supplement file 0.
CustomMissionSupplementFile0CopyAs=stringtable99.csf ; string, target filename of supplement file 0 (required if the
                                     ;          Extension key is present).
CustomMissionSupplementFile1Extension=pal ; ...
CustomMissionSupplementFile1CopyAs=custommission.pal ; ...
CustomMissionSupplementFile2Extension=shp ; ...
CustomMissionSupplementFile2CopyAs=custommission.shp ; ...
; Supplement files are copied to the game folder when a custom mission is played.
; The iteration stops if a number is missing; each Extension value must be unique
; and CopyAs is required whenever Extension is present (a missing CopyAs or a
; duplicate extension throws a ClientConfigurationException).
CopyMissionsToSpawnmapINI=true       ; boolean, writes the mission's `[GameMissionConfig]` to spawnmap.ini.
CopyMissionsToSpawnmapINI.Battle=true  ; boolean, per-mission override for Battle.ini missions.
CopyMissionsToSpawnmapINI.Custom=true  ; boolean, per-mission override for custom missions.
AllowedCustomGameModes=Standard,Custom Map ; comma-separated strings, game modes in which custom (unofficial) maps
                                     ;          are allowed to appear. Official maps are not affected.
InactiveHostWarningMessageSeconds=0  ; integer, seconds before an inactive host is warned.
InactiveHostKickSeconds=0            ; integer, additional seconds before an inactive host is kicked (total = kick + warning).
SkillLevelOptions=Any,Beginner,Intermediate,Pro ; comma-separated strings, skill level choices.
DefaultSkillLevelIndex=0             ; integer, default skill level index (clamped to the options list).
CompatibilityCheckExecutables=       ; comma-separated strings, executables checked for DirectDraw compatibility mode
                                     ;          issues. Example: CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe.
DisallowJoiningIncompatibleGames=false ; boolean, prevents joining games on a different game version.
UseClientRandomStartLocations=false  ; boolean, lets the client randomize starting locations.
AllowedAllAspectsWindow.INItializable=true ; boolean, enables expression attributes ($X, $Y, ...) on windows.
UselbMissionDescription=false        ; boolean, makes the mission description scrollable in the campaign selector.
DefaultFrameSendRate=7               ; integer, default FrameSendRate written to spawn.ini.
DefaultProtocolVersion=2             ; integer, default Protocol written to spawn.ini.
DefaultMaxAhead=0                    ; integer, default MaxAhead written to spawn.ini.
CnCNetLiveStatusIdentifier=cncnet5_ts ; string, identifier used for CnCNet live status.
```

#### `[Translations]`

```ini
[Translations]
TranslationIniName=Translation.ini    ; string, translation INI file name (comma-separated list supported).
TranslationsFolder=Resources/Translations ; string, folder containing translation files.
GameFileX=path/to/source.file,path/to/destination.file[,checked] ; translation game files. `X` is any text.
                                     ;          `checked` must literally be `CHECKED`. Invalid syntax throws an
                                     ;          IniParseException.
```

#### `[Themes]`

```ini
[Themes]
0=ThemeName,Themes/ThemePath ; index -> theme name and relative path.
```

#### `[UserDefaults]`

```ini
[UserDefaults]
BorderlessWindowedClient=true ; boolean, default value of the borderless windowed mode option.
IntegerScaledClient=false     ; boolean, default value of the integer scaling option.
WriteInstallationPathToRegistry=true ; boolean, writes the installation path to the registry.
```

### DTACnCNetClient.ini

The client's per-user settings file (`ClientCore/ClientConfiguration.cs`), located in the resource folder. It defines UI colors, tooltip behavior, audio cooldowns and the [constant tables](#constants).

#### `[General]`

```ini
[General]
MainMenuThemePath=               ; string,  sub-path prefix for main menu assets, e.g. "Themes/MyriaDimensionTheme".
MainMenuTheme=mainmenu           ; string,  space-separated list of main menu themes; one is picked at random.
AlphaRate=0.005                  ; float,   default control transparency change rate per 100 ms.
CheckBoxAlphaRate=0.05           ; float,   default checkbox transparency change rate per 100 ms.
IndicatorAlphaRate=0.05          ; float,   default indicator transparency change rate per 100 ms.
UILabelColor=0,0,0               ; color,   default label text color.
HintTextColor=128,128,128        ; color,   hint text color.
DisabledButtonColor=108,108,108  ; color,   disabled button text color.
AltUIColor=255,255,255           ; color,   alternate UI color.
ButtonHoverColor=255,192,192     ; color,   button hover color.
AltUIBackgroundColor=196,196,196 ; color,   alternate UI background color.
WindowBorderColor=128,128,128    ; color,   window border color.
PanelBorderColor=255,255,255     ; color,   panel border color.
ListBoxHeaderColor=255,255,255   ; color,   list box header text color.
ListBoxFocusColor=64,64,168      ; color,   list box focus/highlight color.
HoverOnGameColor=32,32,84        ; color,   hover color of games in the game list.
DefaultChatColor=0,255,0         ; color,   default chat message color.
AdminNameColor=255,0,0           ; color,   admin name color in chat.
PrivateMessageOtherUserColor=196,196,196 ; color, color of received private messages.
PrivateMessageColor=128,128,128  ; color,   color of sent private messages.
DefaultPersonalChatColorIndex=0  ; integer, default personal chat color index.
MapPreviewNameBackgroundColor=0,0,0,144 ; color, map preview name background.
MapPreviewNameBorderColor=128,128,128,128 ; color, map preview name border.
StartingLocationHoverColor=255,255,255,128 ; color, starting location hover remap color.
StartingLocationsUsePlayerRemapColor=false ; boolean, uses the player's remap color for starting locations.
DropDownScrollBarThumbColor=     ; color,   dropdown scroll bar thumb color.
DropDownScrollBarTrackColor=     ; color,   dropdown scroll bar track color.
DropDownScrollBarBorderColor=    ; color,   dropdown scroll bar border color.
ToolTipFontIndex=0               ; integer, font index used by tooltips.
ToolTipOffsetX=0                 ; integer, horizontal offset of tooltips.
ToolTipOffsetY=0                 ; integer, vertical offset of tooltips.
ToolTipMargin=4                  ; integer, tooltip margin in pixels.
ToolTipDelay=0.67                ; float,   seconds before a tooltip appears.
ToolTipAlphaRate=4.0             ; float,   tooltip fade-in rate per second.
```

#### `[Audio]`

```ini
[Audio]
SoundGameLobbyJoinCooldown=0.25     ; float, minimum seconds between join sounds in the game lobby.
SoundGameLobbyLeaveCooldown=0.25    ; float, minimum seconds between leave sounds.
SoundMessageCooldown=0.25           ; float, minimum seconds between lobby chat message sounds.
SoundPrivateMessageCooldown=0.25    ; float, minimum seconds between private message sounds.
SoundGameLobbyGetReadyCooldown=5.0  ; float, minimum seconds between "get ready" sounds.
SoundGameLobbyReturnCooldown=1.0    ; float, minimum seconds between "return" sounds.
```

#### `[ParserConstants]` / `[SameNameConstants]`

See [Constants](#constants).

### Settings.ini (UserINISettings)

The per-user settings file (`ClientCore/Settings/UserINISettings.cs`), written to `SettingsFile` (default `Settings.ini`, key `SettingsFile` in `ClientDefinitions.ini` `[Settings]`) **in the game folder** (`ProgramConstants.GamePath`). It persists user options and is the storage used by the `Setting*` controls (see [XNAOptionsPanel Controls](#xnaoptionspanel-controls)). When a `UserDefaults.ini` exists in the resource folder, it is loaded first as the base and the user file is merged on top of it (user values win). Notable keys:

```ini
[Audio]
PlayMainMenuMusic=true             ; boolean, play the main menu music. Default `true`.
ScoreVolume=0.7                    ; float,   score volume. Default `0.7`. RA reads `[Options] ScoreVolume`.
SoundVolume=0.7                    ; float,   sound volume. Default `0.7`. RA reads `[Options] Volume`.
VoiceVolume=0.7                    ; float,   voice volume. Default `0.7`.
IsScoreShuffle=true                ; boolean, shuffle the score. Default `true`.
ClientVolume=1.0                   ; float,   client volume multiplier. Default `1.0`.
StopMusicOnMenu=true               ; boolean, stop music when returning to the main menu. Default `true`.
StopGameLobbyMessageAudio=true     ; boolean, stop game lobby message audio. Default `true`.
ChatMessageSound=true              ; boolean, play a sound on chat messages. Default `true`.

[Video]
EnableBackgroundVideo=false        ; boolean, master switch for the main menu background video. Default `false`.
EnableBackgroundVideoSound=false   ; boolean, enables the background video's audio track. Default `false`.
WindowedMode=                      ; boolean, windowed mode (key name from `WindowedModeKey`, default `Video.Windowed`).
NoWindowFrame=                     ; boolean, borderless windowed mode.
BorderlessWindowedClient=true      ; boolean, default value of the borderless windowed client option.
IntegerScaledClient=false          ; boolean, default value of the integer scaling option.
ScreenWidth=1024                   ; integer, in-game screen width. Default `1024`. RA uses `[Options] Width`.
ScreenHeight=768                   ; integer, in-game screen height. Default `768`. RA uses `[Options] Height`.
ClientFPS=60                       ; integer, client frame rate. Default `60`.
DisplayToggleableExtraTextures=true; boolean, show toggleable extra textures. Default `true`.
ForceLowestDetailLevel=false       ; boolean, force the lowest detail level. Default `false`.
UseGraphicsPatch=true              ; boolean, (TS only) use the graphics patch. Default `true`.
VideoBackBuffer=false              ; boolean, (non-TS) render into the video back buffer. Default `false`.

[Options]
GameSpeed=1                        ; integer, in-game speed setting.
DetailLevel=2                      ; integer, detail level. Default `2`.
Translation=                       ; string,  translation locale. Default: the built-in locale.
TranslationGameFilesVersion=       ; string,  version marker of the translated game files. Default empty.
ScrollRate=3                       ; integer, map scroll rate. Default `3`.
DragDistance=4                     ; integer, drag distance. Default `4`.
CustomDragDistance=0               ; integer, fixed drag distance override in pixels (0 = auto-scaled). Default `0`.
DoubleTapInterval=30               ; integer, double-tap interval in ms. Default `30`.
Win8Compat=No                      ; string,  Windows 8 compatibility mode. Default `No`.
CheckforUpdates=true               ; boolean, check for updates on startup. Default `true`.
PrivacyPolicyAccepted=false        ; boolean, whether the privacy policy was accepted. Default `false`.
IsFirstRun=true                    ; boolean, whether this is the first run. Default `true`.
CustomComponentsDenied=false       ; boolean, whether installing custom components was denied. Default `false`.
Difficulty=1                       ; integer, campaign difficulty. Default `1`.
ScrollDelay=4                      ; integer, scroll delay. Default `4`.
MinimizeWindowsOnGameStart=true    ; boolean, minimize other windows when the game starts. Default `true`.
AutoRemoveUnderscoresFromName=true ; boolean, remove underscores from player names. Default `true`.
GenerateTranslationStub=false      ; boolean, generate a translation stub file. Default `false`.
GenerateOnlyNewValuesInTranslationStub=false ; boolean, only write new values to the translation stub. Default `false`.
WriteInstallationPathToRegistry=   ; boolean, write the installation path to the registry.

[MultiPlayer]
Theme=                             ; string,  client theme name. Default: the first available theme.
Handle=                            ; string,  player handle / name. Default empty.
CustomPlayerName=                  ; string,  the player's custom in-game name, used when custom names are enabled in
                                   ;          the game lobby player name options ([PlayerNameOptions]).
ChatColor=-1                       ; integer, chat color. Default `-1`.
LANChatColor=-1                    ; integer, LAN chat color. Default `-1`.
PingCustomTunnels=true             ; boolean, ping unofficial (custom) tunnels. Default `true`.
PlaySoundOnGameHosted=true         ; boolean, play a sound when hosting a game. Default `true`.
SkipConnectDialog=false            ; boolean, skip the connect dialog. Default `false`.
PersistentMode=false               ; boolean, persistent mode. Default `false`.
AutomaticCnCNetLogin=false         ; boolean, automatic CnCNet login. Default `false`.
DiscordIntegration=true            ; boolean, enable Discord integration. Default `true`.
SteamIntegration=true              ; boolean, enable Steam integration. Default `true`.
AllowGameInvitesFromFriendsOnly=false ; boolean, only allow game invites from friends. Default `false`.
NotifyOnUserListChange=true        ; boolean, notify when the user list changes. Default `true`.
DisablePrivateMessagePopups=false  ; boolean, disable private message popups. Default `false`.
DisableMainMenuHotkeys=true        ; boolean, disable main menu hotkeys. Default `true`.
AllowPrivateMessagesFromState=0    ; integer, whom private messages are allowed from. Default `0` (everyone).
EnableMapSharing=true              ; boolean, enable map sharing. Default `true`.
AlwaysDisplayTunnelList=false      ; boolean, always display the tunnel list. Default `false`.
PreferredCnCNetTunnel=             ; string,  preferred CnCNet tunnel address (written by the "Save as Default"
                                   ;          tunnel button).
MapSortState=0                     ; integer, map list sort state. Default `0`.
SearchAllGameModes=false           ; boolean, search across all game modes. Default `false`.

[Compatibility]
Renderer=                          ; string,  renderer override. Default empty.

[Phobos]
CampaignDefaultGameSpeed=4         ; integer, default campaign game speed. Default `4`.

[GameFilters]
SortState=0                        ; integer, game list sort state. Default `0`.
ShowFriendGamesOnly=false          ; boolean, show only friend games. Default `false`.
HideLockedGames=false              ; boolean, hide locked games. Default `false`.
HidePasswordedGames=false          ; boolean, hide passworded games. Default `false`.
HideIncompatibleGames=false        ; boolean, hide incompatible games. Default `false`.
MaxPlayerCount=8                   ; integer, maximum player count filter (2-8). Default `8`.

[GameOptionFilters]
ControlName=1                      ; integer, per-option filter (key = game lobby control name). For checkboxes
                                   ;          0 = Off, 1 = On; for dropdowns the selected option index. An absent
                                   ;          key means "All" (no filter).

[Channels]
ChannelName=Yes                    ; boolean, whether the channel / game is followed (key = channel name).

[FavoriteMaps]
0=MAPSHA1:GAMEMODE                 ; string,  favorite map entry (`mapSHA1:gameMode`). Legacy name-based entries are
                                   ;          migrated to SHA1-based entries when used.
```

`SettingCheckBox`/`SettingDropDown` controls save under `[CustomSettings]` (see `SettingSection`) with keys
`CONTROLNAME_Checked` / `CONTROLNAME_Value` / `CONTROLNAME_SelectedIndex` depending on the write mode.

Game type differences: for RA, the screen size keys (`Width`/`Height`) and the volume keys (`ScoreVolume`/`Volume`) live under `[Options]` instead of `[Video]`/`[Audio]`, and saving writes an extra `[Options] MultiplayerScoreVolume` mirror; for TS, the back-buffer key is `UseGraphicsPatch` instead of `VideoBackBuffer`. The `[GameFilters]`, `[GameOptionFilters]`, `[Channels]` and `[FavoriteMaps]` sections are used by the game list and lobby filtering UI.

### NetworkDefinitions.ini

If a `NetworkDefinitions.local.ini` exists in the resource folder, it is used **instead of** `NetworkDefinitions.ini` (user override; a log line confirms which one was loaded).

```ini
[Settings]
CnCNetTunnelListURL=      ; string, URL of the CnCNet tunnel list.
CnCNetPlayerCountURL=     ; string, URL of the CnCNet player count API.
CnCNetMapDBDownloadURL=   ; string, URL of the map database download API.
CnCNetMapDBUploadURL=     ; string, URL of the map database upload API.
DisableDiscordIntegration=false ; boolean, disables Discord integration.

[IRCServers]
0=irc.server.example ; IRC server addresses; every non-empty value is added.
```

### MPMaps.ini

Game modes are defined in the `[GameModes]` section of `MPMaps.ini`. Each game mode can have its own configuration section with the same name.

```ini
[GameModes]
0=Standard
1=No Bases
2=Infantry Only

[Standard]
UIName=Standard
; Game mode properties...
```

The multiplayer map list and aliases are defined in two additional sections:

```ini
[MultiMaps]
0=Maps/MyMap          ; map entries: N = path of the map file relative to the game root, without the extension.
1=Maps/AnotherMap

[GameModeAliases]
MyAlias=Standard,Infantry Only ; alias -> comma-separated list of real game mode names. A game mode alias acts as
                                ;          a single mode that expands to the listed modes.
```

- **`[MultiMaps]`** — the entry point of the official multiplayer map list. Each value is a map path relative to the game root, without the `.map` extension (the extension from `ClientDefinitions.ini` `[Settings]` `MapFileExtension` is appended). Without this section the official map list fails to load. Each map entry may also define any of the [Map Properties](#map-properties) in a section of the same name.
- **`[GameModeAliases]`** — maps an alias name to one or more real game mode names (comma-separated). Selecting the alias in the UI behaves like the listed modes.

#### Game Mode Properties

```ini
[GAME_MODE_NAME]                       ; GameMode
UIName=                                ; string,  display name. Defaults to the section name.
MinPlayersOverride=                    ; integer, override for the minimum player count.
MaxPlayersOverride=                    ; integer, override for the maximum player count.
DisallowedPlayerSides=                 ; comma-separated integers, side indices that no player may select.
DisallowedHumanPlayerSides=            ; comma-separated integers, side indices that human players may not select.
DisallowedComputerPlayerSides=         ; comma-separated integers, side indices that AI players may not select.
DisallowedPlayerColors=                ; comma-separated integers, color indices that no player may select.
DisallowedPlayerSides.StartN=          ; comma-separated integers, per-starting-location side restrictions (unioned with
                                       ;          the global list).
DisallowedPlayerColors.StartN=         ; comma-separated integers, per-starting-location color restrictions.
ForcedOptions=                         ; string,  name of an INI section whose keys become forced checkbox/dropdown values.
ForcedSpawnIniOptions=                 ; string,  name of an INI section whose key-value pairs are written to
                                       ;          spawn.ini [Settings]. Defaults to `{Name}ForcedSpawnIniOptions`.
MapCodeIniName=                        ; string,  name of the map code INI file in `INI/Map Code/`. Defaults to
                                       ;          `{Name}.ini`. Alias: MapCodeININame.
RandomizedMapCodeIniNames=             ; comma-separated strings, additional randomized map code INI names. Alias:
                                       ;          RandomizedMapCodeININames.
RandomizedMapCodesCount=1              ; integer, how many of the randomized map codes to pick.
```

#### GameModeMapBase Properties

The following properties are shared between maps (in `MPMaps.ini` map sections) and game modes. When both a map and its game mode define the same property, the map value takes priority unless noted otherwise.

```ini
; Map section keys:
[MAP_NAME]
ClientMinPlayer=                      ; integer,  minimum player count (client-side). Alternative key names: `MinPlayers`, `MinPlayer`.
ClientMaxPlayer=                      ; integer,  maximum player count (client-side). Alternative key names: `MaxPlayers`, `MaxPlayer`.
EnforceMinPlayers=                    ; boolean,  whether the minimum player count is enforced.
EnforceMaxPlayers=                    ; boolean,  whether the maximum player count is enforced.
AllowedStartingLocations=             ; comma-separated integers, allowed starting locations. Starts from 0. If not set,
                                      ;          all starting locations are allowed (max 8).
IsCoopMission=                        ; boolean,  marks the map as a co-op mission.
ClientMultiplayerOnly=                ; boolean,  whether the map cannot be played in Skirmish (custom maps).
MultiplayerOnly=                      ; boolean,  whether maps in this mode cannot be played in Skirmish (game modes).
HumanPlayersOnly=                     ; boolean,  whether AI players are forbidden.
ForceRandomStartLocations=            ; boolean,  force random starting positions.
ForceNoTeams=                         ; boolean,  force no team assignments.
CoopDifficultyLevel=                  ; integer,  co-op difficulty override.

; Game mode section keys:
[GAME_MODE]
MinPlayersOverride=                   ; integer,  minimum player count. Higher priority than Map.ClientMinPlayer.
MaxPlayersOverride=                   ; integer,  maximum player count. Higher priority than Map.ClientMaxPlayer.
EnforceMinPlayers=                    ; boolean,  whether the minimum player count is enforced.
EnforceMaxPlayers=                    ; boolean,  whether the maximum player count is enforced.
AllowedStartingLocations=             ; comma-separated integers, allowed starting locations.
IsCoopMission=                        ; boolean,  marks the mode as co-op.
MultiplayerOnly=                      ; boolean,  whether maps in this mode cannot be played in Skirmish.
HumanPlayersOnly=                     ; boolean,  whether AI players are forbidden.
ForceRandomStartLocations=            ; boolean,  force random starting positions.
ForceNoTeams=                         ; boolean,  force no team assignments.
CoopDifficultyLevel=                  ; integer,  co-op difficulty override.
```

Priority resolution for player counts:
- `MaxPlayers`: `GameMode.MaxPlayersOverride` > `Map.ClientMaxPlayer` > `Map.MaxPlayer`
- `MinPlayers`: `GameMode.MinPlayersOverride` > `Map.ClientMinPlayer` > `Map.MinPlayer`

#### Map Properties

Additional keys supported by map sections:

```ini
[MAP_NAME]
BaseSection=                           ; string,  name of another map section to inherit keys from (merged first).
Description=Unnamed map                ; string,  map description.
Author=Unknown author                 ; string,  map author.
GameModes=Default                      ; string,  game modes this map appears in.
PreviewImage=                          ; string,  map preview image path.
Briefing=                              ; string,  co-op briefing text.
SpawnIniBriefing=                      ; string,  briefing written to spawn.ini.
CooperativeLoadScreenSettings=false    ; boolean, enables the cooperative loading screen settings.
CooperativeLoadScreen=                 ; string,  cooperative loading screen file.
CooperativeLoadScreenPallet=           ; string,  cooperative loading screen palette file.
Credits=-1                             ; integer, starting credits (-1 = default).
UnitCount=-1                           ; integer, starting unit count (-1 = default).
NeutralColor=-1                        ; integer, neutral color index (-1 = default).
SpecialColor=-1                        ; integer, special color index (-1 = default).
Bases=                                 ; boolean, whether players start with bases.
ExtraTextureN=name,x,y[,level[,toggleable]] ; map preview extra texture placement.
LocalSize=X,Y,WIDTH,HEIGHT            ; 4 integers, map size for the preview (isometric maps); default "0,0,0,0".
                                      ;          Non-isometric maps use the separate `X`/`Y`/`Width`/`Height` keys
                                      ;          instead of `LocalSize`/`Size`.
WaypointN=CELL[,LEVEL]                ; string,  waypoint coordinate (0-based, read from `Waypoint0` up). Non-isometric
                                      ;          games use a single cell value; isometric games use `cell[,level]`.
TeamStartMappingN=A,B,C,D             ; comma-separated team codes, team start mapping preset. Position + 1 is the
                                      ;          starting location; `x` = no player (blocked), `-` = no team,
                                      ;          `A`-`D` = team letters. `N` starts from 0.
TeamStartMappingNName=                 ; string,  team start mapping name.
ForcedOptions=                         ; comma-separated strings, names of INI sections whose keys become forced
                                       ;          checkbox/dropdown values. Default: none.
ForcedSpawnIniOptions=                 ; comma-separated strings, names of INI sections whose key-value pairs are
                                       ;          written to spawn.ini. Default: none.
MissionSpawnMapIniOptions=SourceSection:TargetSection[,More:More] ; comma-separated section mappings. For each
                                       ;          `SourceSection:TargetSection` pair, all keys of `[SourceSection]` (a
                                       ;          section of this map's `MPMaps.ini` entry) are copied into
                                       ;          `[TargetSection]` of the generated map INI (spawnmap.ini) at game
                                       ;          launch, after map codes are applied. Default: none.
ExtraIniName=MyExtraCode.ini           ; string,  filename in `INI/Map Code/` to consolidate into the map INI at game
                                       ;          launch. Alias: ExtraININame.
```

#### Co-op Map Properties

For co-op maps (`IsCoopMission=Yes`), the map section additionally supports enemy/ally house definitions and the same disallowed side/color lists available to game modes:

```ini
[MAP_NAME]
EnemyHouseN=side,color,startingLocation ; 3 integers, enemy house definition for slot `N` (N = 0, 1, ...). Values are
                                         ;          the house side index, color index and starting-location waypoint.
AllyHouseN=side,color,startingLocation   ; 3 integers, ally house definition for slot `N`.
DisallowedPlayerSides=                   ; comma-separated integers, side indices that no player may select (also valid
                                         ;          on map sections, not only game modes).
DisallowedPlayerColors=                  ; comma-separated integers, color indices that no player may select.
DisallowedPlayerSides.StartN=            ; comma-separated integers, per-starting-location side restrictions.
DisallowedPlayerColors.StartN=           ; comma-separated integers, per-starting-location color restrictions.
```

`EnemyHouseN`/`AllyHouseN` are only parsed when `IsCoopMission=Yes`.

#### Custom Multiplayer Map Files

Custom multiplayer `.map` files (placed in the custom map folder or referenced by `[MultiMaps]`) can carry client-side settings inside their own INI sections, which are read when the map is loaded:

```ini
; Inside the .map file
[Basic]
Name=My Map                           ; string,  map display name (Description fallback).
GameModes=Standard                    ; string,  game modes the map appears in (falls back to [Map] GameModes).
GameMode=Standard                     ; string,  legacy single game mode key.
Author=Author Name                    ; string,  map author.
Briefing=                             ; string,  co-op briefing text.
SpawnIniBriefing=                     ; string,  briefing written to spawn.ini.
CooperativeLoadScreenSettings=false    ; boolean, enable cooperative loading screen settings.
CooperativeLoadScreen=                ; string,  cooperative loading screen file.
CooperativeLoadScreenPallet=          ; string,  cooperative loading screen palette file.
Credits=-1                            ; integer, starting credits (-1 = default).
UnitCount=-1                          ; integer, starting unit count (-1 = default).
NeutralColor=-1                       ; integer, neutral color index (-1 = default).
SpecialColor=-1                       ; integer, special color index (-1 = default).
Bases=                                ; boolean, whether players start with bases.
ExtraIniName=                         ; string,  extra map code INI to consolidate (alias: ExtraININame).

[Map]
LocalSize=0,0,0,0                     ; 4 integers, isometric map size. Non-isometric maps use [Map] X/Y/Width/Height.
X=0                                   ; integer, (non-isometric) map X origin.
Y=0                                   ; integer, (non-isometric) map Y origin.
Width=0                               ; integer, (non-isometric) map width.
Height=0                              ; integer, (non-isometric) map height.

[Waypoints]
0=118035                              ; waypoint coordinates, keyed `0`-`7`. Non-isometric: single cell value;
                                      ;          isometric: `cell[,level]`.

[ForcedOptions]
; Keys become forced checkbox/dropdown values (fixed section name).

[ForcedSpawnIniOptions]
; Keys are written to spawn.ini (fixed section name).
```

The same client-side keys (`ClientMinPlayer`, `ClientMaxPlayer`, `AllowedStartingLocations`, ...) can also be defined in these internal sections instead of `MPMaps.ini`.

#### Map Code INI

Files in `INI/Map Code/` (see `MapCodeIniName`) may define extra behavior beyond plain section consolidation:

```ini
[GameModeIncludes]
Standard=ExtraStandardCode.ini         ; key = game mode name, value = additional map code INI file applied only
                                      ;          when that game mode is active. The section itself is erased after use.

[ReplaceMapAircraft]
OLD_ID=NEW_ID                         ; rename map objects: key = old object ID, value = new object ID (empty value
                                      ;          removes the object). One section per category: ReplaceMapAircraft,
                                      ;          ReplaceMapInfantry, ReplaceMapUnits, ReplaceMapStructures,
                                      ;          ReplaceMapTerrain.
```

### GameOptions.ini

The `GameOptions.ini` file defines sides, random selectors, multiplayer colors, and forced spawn options.

```ini
[General]
Sides=GDI,Nod,Allies,Soviet            ; comma-separated strings, playable sides. Code default is
                                       ;          "GDI,Nod,Allies,Soviet"; the shipped file uses "GDI,Nod".
InternalSideIndices=                   ; comma-separated integers, internal side indices.
SpectatorInternalSideIndex=            ; integer, internal side index used for spectators.
StartingLocationAngularVelocity=0.015  ; float,   starting location rotation speed. Code default `0.015`
                                       ;          (the shipped file uses 0.0075).
ReservedStartingLocationAngularVelocity=-0.0075 ; float, reserved starting location rotation speed. Code default
                                       ;          `-0.0075` (the shipped file uses 0.05).
RandomColor=255,255,255                ; color,   random color entry. Code default "255,255,255"
                                       ;          (the shipped file uses 168,168,168).

[MPColors]
Gold=255,223,94,0                      ; name=R,G,B,gameColorIndex - multiplayer colors. The color name is the key
                                       ;          name (not an index); gameColorIndex is the in-game color ID.

[RandomSelectors]
Name=0,1,2                             ; selector name -> comma-separated side indices. Indices outside the valid
                                       ;          side range are ignored; a selector needs more than one valid side
                                       ;          index to be registered.

[ForcedSpawnIniOptions]
FogOfWar=no                            ; keys always written to spawn.ini [Settings] for multiplayer games.

[CampaignForcedSpawnIniOptions]
AutoSaveInterval=0                     ; keys always written to spawn.ini [Settings] for campaign missions.
```

#### Player AI Quick Options

`[PlayerAIQuickOptions]` defines the default state of the AI quick options panel in the game lobby (`PlayerAIQuickOptionsPanel`). All checkbox keys accept `Yes`/`No`. The dropdown keys store the dropdown item index *minus one* (`SelectedIndex - 1`), because index 0 is always the "Don't Set" placeholder item.

```ini
[PlayerAIQuickOptions]
cmbAIQuickDifficultyLevel=2 ; integer, AI difficulty: -1 = Don't Set, 0 = Easy, 1 = Medium, 2 = Hard.
                            ;          Code default `2` (Hard) if the key is absent.
cmbAIQuickSide=0            ; integer, AI side: -1 = Don't Set, 0 = Random, then random-selector / side indices
                            ;          (item order: Don't Set, Random, selectors, sides). Code default `0` (Random).
cmbAIQuickColor=0           ; integer, AI color: -1 = Don't Set, 0 = Random, then multiplayer color indices
                            ;          (item order: Don't Set, Random, colors). Code default `0` (Random).
cmbAIQuickTeam=0            ; integer, AI team: -1 = Don't Set, 0 = No team, then team indices
                            ;          (item order: Don't Set, -, teams). Code default `0` (No team).
chkRandomAIDifficulty=No    ; boolean, randomize the AI difficulty. Default No.
chkRandomAISide=No          ; boolean, randomize the AI side. Default No.
chkRandomAIColor=No         ; boolean, randomize the AI color. Default No.
chkRandomAITeam=No          ; boolean, randomize the AI team. Default No.
chkAutoAssignAIStarts=No    ; boolean, automatically assign starting locations to AI players. Default No.
chkAIPlayerN=No             ; boolean, format-painter default selection for AI player `N` (`N` = 0-7). Default No.

; Which item groups are included in the pool when "random" is chosen for AI sides / colors:
Side.RandomAISelection=Yes           ; boolean, include the playable sides. Default Yes.
SideRandom.RandomAISelection=Yes     ; boolean, include the "Random" side item. Default Yes.
SideSelectors.RandomAISelection=Yes  ; boolean, include the random selectors. Default Yes.
Color.RandomAISelection=Yes          ; boolean, include the multiplayer colors. Default Yes.
ColorRandom.RandomAISelection=Yes    ; boolean, include the "Random" color item. Default Yes.
```

#### Player Name Options

`[PlayerNameOptions]` defines the default state of the player name options panel in the game lobby (`PlayerNameOptionsPanel`). Values accept `Yes`/`No`.

```ini
[PlayerNameOptions]
chkAllowCustomNames=No ; boolean, host master switch: allow players to use custom in-game names. Default No.
chkEnableCustomName=No ; boolean, enable the local player's custom name (effective only when the host allows
                       ;          custom names). Default No.
```

The custom name text itself is not stored here: it is persisted in `Settings.ini` under `[MultiPlayer]` → `CustomPlayerName` (see [Settings.ini](#settingsini-userinisettings)).

#### Player Extra Options

`[PlayerExtraOptions]` defines the default state of the extra player options panel in the game lobby (`PlayerExtraOptionsPanel`). Values accept `Yes`/`No`; all default to `No`.

```ini
[PlayerExtraOptions]
chkBoxForceRandomSides=No     ; boolean, force all players to random sides.
chkBoxForceNoTeams=No         ; boolean, force no teams.
chkBoxForceRandomColors=No    ; boolean, force all players to random colors.
chkBoxForceRandomStarts=No    ; boolean, force random starting locations.
chkBoxUseTeamStartMappings=No ; boolean, enable auto-allying via team start mappings.
```

These are the lobby defaults only; the host can change them during the session, and the live state is synchronized to other players through network messages rather than INI.

#### ForcedSpawnIniOptions

Forced spawn options define keys that are always written to `spawn.ini` regardless of UI settings. They can be defined at multiple levels:

1. **Global** — `[ForcedSpawnIniOptions]` in `GameOptions.ini`: applied to all multiplayer games.
2. **Campaign** — `[CampaignForcedSpawnIniOptions]` in `GameOptions.ini`: applied to campaign missions only.
3. **Per game mode** — each game mode section in `MPMaps.ini` can specify `ForcedSpawnIniOptions=SectionName` pointing to a section whose keys are written to `spawn.ini`.
4. **Per map** — maps can specify `ForcedSpawnIniOptions=SectionName` (comma-separated for multiple sections) in `MPMaps.ini`.

Spawn.ini writing order for multiplayer:
1. Game lobby checkboxes/dropdowns
2. Global `[ForcedSpawnIniOptions]` from `GameOptions.ini`
3. Game mode specific forced options
4. Map specific forced options

In `MPMaps.ini`:

```ini
[MyGameMode]
ForcedSpawnIniOptions=MyModeForcedOptions

[MyModeForcedOptions]
; Keys here are written to spawn.ini when this game mode is active.
SomeOption=value

[MyMap]
ForcedSpawnIniOptions=MyMapForcedOptions

[MyMapForcedOptions]
; Keys here are written to spawn.ini when this specific map is played.
AnotherOption=value
```

### GameOptionsPresets.ini

`GameOptionsPresets.ini` stores game option presets (the save/load preset feature of the game lobby). It lives in the user files folder and is read/written by the client.

```ini
[Presets]
0=My Preset                      ; N = preset name.

[My Preset]
CheckBoxValues=chkCrates:1,chkShortGame:1    ; comma-separated `controlName:value` pairs (0 = unchecked, 1 = checked).
DropDownValues=ddTechLevel:7,ddStartingCredits:5 ; comma-separated `controlName:index` pairs (selected option index).
DropDownCustomValues=ddTechLevel:100|200,ddStartingCredits: ; comma-separated `controlName:value` pairs (custom values).
```

### GameCollectionConfig.ini

`GameCollectionConfig.ini` (in the base resource folder) adds custom games to the CnCNet game selection window.

```ini
[CustomGames]
0=MyGame                          ; N = game section name.

[MyGame]
InternalName=MYGAME               ; string,  unique internal game ID (lowercased, max length enforced).
IconFilename=MYGAMEicon.png       ; string,  icon texture file. Default `{InternalName}icon.png`.
UIName=My Game                    ; string,  display name. Default: the internal ID in upper case.
ChatChannel=#mygame               ; string,  IRC chat channel name.
GameBroadcastChannel=#mygame-broadcast ; string,  IRC game broadcast channel name.
ClientExecutableName=             ; string,  executable launched for the custom game. Default empty.
RegistryInstallPath=HKCU\Software\MYGAME ; string,  registry key holding the installation path. Default
                                  ;          `HKCU\Software\{INTERNALNAME}`.
```

### SkirmishSettings.ini

`Client/SkirmishSettings.ini` (in the game folder) persists the skirmish lobby state between sessions. It is written on save and read on entry; when the file is absent default settings are used.

```ini
[Player]
Info=                             ; string,  human player definition (serialized player info).

[AIPlayers]
0=                                ; string,  AI player definition, keyed `0`-`7`.

[Settings]
Map=                              ; string,  map SHA1 of the selected map.
GameModeMapFilter=                ; string,  selected game-mode/map filter name. Legacy alias: `GameMode`.

[GameOptions]
ControlName=0                     ; per-lobby-option values (key = control name). Dropdowns store the selected
                                  ;          index, checkboxes store `True`/`False`. Only written when
                                  ;          `SaveSkirmishGameOptions` is enabled in `ClientDefinitions.ini`.
```

### CampaignSettings.ini

`Client/CampaignSettings.ini` (in the game folder) persists the campaign lobby options between sessions. It is written and read only when `SaveCampaignGameOptions` is enabled in `ClientDefinitions.ini`.

```ini
[GameOptions]
ControlName=0                     ; per-option values (key = control name). Dropdowns store the selected index,
                                  ;          checkboxes store `True`/`False`.
```

### spawnSG.ini

`Saved Games/spawnSG.ini` (in the game folder) stores the metadata of the last saved game for the "Load Game" feature of the game lobby; it is runtime-generated and copied to `spawn.ini` when a save is loaded.

```ini
[Settings]
GameID=0                          ; integer, unique game ID.
MapSHA1=                          ; string,  SHA1 of the map.
BroadcastedGameOptionValues=      ; string,  serialized broadcasted game option values.
UIMapName=                        ; string,  map display name.
MapID=                            ; string,  map ID used for localization.
UIGameMode=                       ; string,  game mode display name.
PlayerCount=0                     ; integer, number of players.
Color=0                           ; integer, local player's game color index.

[OtherN]
Name=                             ; string,  name of the other player in slot `N` (N = 1, 2, ...).
Color=0                           ; integer, that player's game color index.
```

### KeyboardCommands.ini

The `KeyboardCommands.ini` file defines in-game hotkey commands that the client writes to the game's keyboard INI file, the filename of which is defined as `KeyboardINI` in `[Settings]` of `ClientDefinitions.ini`. Each section represents a game command with its default key binding.

The file is located in the `Resources` directory and is read by the Hotkey Configuration window.

```ini
[CommandName]
UIName=Display name       ; string,  display name for the command in the hotkey configuration UI.
Category=CategoryName     ; string,  grouping category used in the hotkey configuration UI dropdown.
Description=Description   ; string,  description text shown in the hotkey configuration UI.
DefaultKey=0              ; integer, the default TS-encoded key value (low byte = key code, high byte = modifier flags).
                          ;          Use 0 for commands with no default hotkey.
DisableModifierKeys=false ; boolean, prevents modifier keys (Ctrl, Shift, Alt) from being combined with this command.
                          ;          When true, only single keys can be assigned. Defaults to false.
```

Command properties:

- **`UIName`** — display name for the command. Supports localization via `INI:Hotkeys:{CommandName}:UIName`.
- **`Category`** — category used to group commands in the hotkey configuration dropdown. Supports localization via `INI:HotkeyCategories:{Category}`.
- **`Description`** — description text shown when the command is selected. Supports localization via `INI:Hotkeys:{CommandName}:Description`.
- **`DefaultKey`** — the default key binding in TS-encoded integer format `(modifier << 8) + key`. Modifier flags: 0 = None, 1 = Shift, 2 = Ctrl, 4 = Alt. Set to `0` if the command has no default hotkey.
- **`DisableModifierKeys`** — when `true`, the hotkey configuration window will not allow modifier key combinations for this command. Only a single key (without Ctrl, Shift, or Alt) can be assigned. This is useful for certain commands that do not support modifier-combined hotkeys.

Example:

```ini
[PlanningMode]
UIName=Waypoint Mode
Category=Interface
Description=Enable waypoint mode.
DefaultKey=90
DisableModifierKeys=true
```

The configured hotkeys are written back to the game keyboard INI (`KeyboardINI`, in the section defined by `KeyboardHotkeySection`). If `SettingsFile` equals `KeyboardINI`, the settings INI is used instead so that unsaved changes can be cancelled.

---

## Complete Example

The following is a fully working example that combines the mechanisms documented above. It declares a small INItializable window with a panel, a label, a checkbox that toggles another panel, a dropdown that switches between two option panels, and a button that opens a URL.

```ini
; ExampleWindow.ini

[ExampleWindow]                        ; INItializableWindow
HasCloseButton=true
$X=horizontalCenterOnParent()
$Y=100
$Width=400
$Height=300
$CC00=pnlRoot:XNAPanel
$CC01=lblTitle:XNALabel
$CC02=chkAdvanced:XNAClientCheckBox
$CC03=pnlAdvanced:XNAPanel
$CC04=ddMode:XNAClientDropDown
$CC05=pnlModeA:XNAPanel
$CC06=pnlModeB:XNAPanel
$CC07=btnMore:XNALinkButton

[pnlRoot]
SolidColorBackgroundTexture=32,32,32
Padding=10,10,10,10
$X=0
$Y=0
$Width=400
$Height=300

[lblTitle]
Text=Example Window
TextColor=255,255,255
FontIndex=1
$X=0
$Y=0

[chkAdvanced]
Text=Show advanced panel
Checked=false
$X=10
$Y=40
; Toggle pnlAdvanced's visibility when the checkbox state changes
$Toggles=pnlAdvanced

[ddMode]
Option_ModeA=Mode A
Option_ModeB=Mode B
$X=10
$Y=70
$Width=180
; Show pnlModeA while item 0 is selected, pnlModeB while item 1 is selected
$Toggle0=pnlModeA
$Toggle1=pnlModeB

[pnlAdvanced]
BackgroundTexture=panel_adv.png
$X=200
$Y=40
$Width=180
$Height=60
; Hidden until chkAdvanced is checked

[pnlModeA]
BackgroundTexture=panel_a.png
$X=10
$Y=110
$Width=180
$Height=120

[pnlModeB]
BackgroundTexture=panel_b.png
$X=10
$Y=110
$Width=180
$Height=120
; Hidden until ddMode selects item 1 (controlled by $Toggle1)

[btnMore]
Text=More information
URL=https://cncnet.org
$X=10
$Y=250
$Width=120
```
