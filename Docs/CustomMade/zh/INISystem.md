# INI 系统 — 使用 INI 文件构建客户端 UI



本文档是使用 INI 文件构建客户端用户界面的权威参考。内容基于客户端源码逐项核对生成；下文列出的每个键均由紧邻的实现代码解析。

## 目录

1. INI 文件的解析方式
2. 常量
3. 数据类型
4. 从 INI 创建控件
5. 基础控件属性
6. 客户端控件属性
7. 动态控件属性与表达式
8. 晚期属性（控件联动）
9. 特殊控件
10. 窗口
11. 全局配置文件

---

## INI 文件的解析方式



每个由 INI 驱动的控件都会从 INI 文件中读取自己的段 `[ControlName]`。段名**必须**与控件的 `Name` 一致（`Name` 由代码或 `$CC` 机制设置，参见从 INI 创建控件）。

客户端存在两种不同的初始化模型：

| 模型 | 类 | INI 查找方式 | 额外控件支持 |
|---|---|---|---|
| 窗口模型 | [`XNAWindow`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindow.cs)（继承自 [`XNAWindowBase`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindowBase.cs)） | 活动主题资源目录下的 `{Name}.ini`，其次是基础资源目录，最后回退到 `GenericWindow.ini` 的 `[GenericWindow]` 段 | `[Name]ExtraControls`（通过 `EnabledExtraControls` 启用） |
| INItializable 模型 | [`INItializableWindow`](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/INItializableWindow.cs)（继承自 `XNAPanel`） | `{Name}.ini`，按相同的"主题优先、基础次之"顺序解析；无通用回退 | `$CC` 子控件与 `[$ExtraControls]` 段 |

关键实现说明：

- **资源路径优先级** — 客户端先在 *主题专用* 资源路径（`ProgramConstants.GetResourcePath()`）中查找 INI，然后在 *基础* 资源路径（`GetBaseResourcePath()`）中查找。
- **`IniNameOverride`**（仅代码可设置，protected）— 设置后窗口改为读取 `{IniNameOverride}.ini`，当该文件不存在时回退到 `{Name}.ini`。
- **`ExternalIniFile`**（仅代码可设置）— 当 `INItializableWindow` 被注册为另一窗口的额外控件时，它直接从宿主窗口的 INI 文件初始化，从而可以在同一个文件中声明它的 `[Name]` 段与 `$CC` 子控件。
- **大小写敏感** — INI 键由解析器不区分大小写地匹配；但在控件树中查找时，段名与控件名是大小写敏感的，请保持一致。

---

## 常量



常量是**整数**，可在动态控件属性中引用。它们在窗口初始化时由表达式 `Parser`（`ClientGUI/Parser.cs`）解析。

### 定义

常量定义在 `DTACnCNetClient.ini` 的 `[ParserConstants]` 段中（或 `DTACnCNetClient.ini` 包含的任何主题文件，例如 `GlobalThemeSettings.ini`）：

```ini
; DTACnCNetClient.ini
[ParserConstants]
MY_EXAMPLE_CONSTANT=15
```

此外，同一文件的 `[SameNameConstants]` 段定义**别名**。别名将一个常量名映射到另一个常量名，支持链式定义（`A=B`、`B=C`）；循环别名定义会在解析时被检测并拒绝：

```ini
[SameNameConstants]
ALIAS_OF_WIDTH=RESOLUTION_WIDTH
```

### 预定义系统常量

| 常量 | 值 |
|---|---|
| `RESOLUTION_WIDTH` | 游戏窗口初始化时的渲染分辨率宽度。 |
| `RESOLUTION_HEIGHT` | 游戏窗口初始化时的渲染分辨率高度。 |

### 使用方法

```ini
[MyExampleControl]
$X=MY_EXAMPLE_CONSTANT
$Width=RESOLUTION_WIDTH/2
```

> [!NOTE]
> 常量（以及表达式）**只能**用于动态控件属性（以 `$` 开头的键）。基础控件属性（如 `X=`、`Width=`）按普通值解析，不支持常量。

### 查找顺序

1. `[ParserConstants]`（规范定义优先）。
2. `[SameNameConstants]` 别名链 — 未知名先通过别名表解析，失败才报错。错误信息会提示你检查 `DTACnCNetClient.ini`（或其依赖文件）中的 `[ParserConstants]` 段。

### 算术表达式



动态属性的值按算术表达式解析，支持 `+`、`-`、`*`、`/`、括号 `( )`、常量以及下面的函数。空白字符会被忽略。

| 函数 | 说明 |
|---|---|
| `getX(ControlName)` / `getY(ControlName)` | 返回指定控件的 X/Y 坐标。 |
| `getWidth(ControlName)` / `getHeight(ControlName)` | 返回指定控件的宽/高。 |
| `getBottom(ControlName)` | 返回指定控件的 `Y + Height`（`XNAOptionsPanel` 使用其可滚动区域的底部）。 |
| `getRight(ControlName)` | 返回指定控件的 `X + Width`（`XNAOptionsPanel` 使用其可滚动区域的右侧）。 |
| `horizontalCenterOnParent()` | 将正在解析的控件在其父控件上水平居中，并返回结果 X。 |

特殊参数名：

| 参数 | 含义 |
|---|---|
| `$Self` | 正在解析动态属性的控件本身。 |
| `$ParentControl` | 正在解析控件的逻辑父控件（跳过 `XNAScrollPanel` 及其内容面板内部结构，因此解析到真正的容器，如 `XNAOptionsPanel`）。 |

表达式参数中控件的查找规则：主（窗口）控件本身 → 主控件的后代 → 祖先及其**直接**子控件（主控件的兄弟控件）。兄弟子树中的深层嵌套控件永远无法遮蔽同名的窗口级控件。

示例：

```ini
[lblHeader]
$X=horizontalCenterOnParent()
$Y=getY(btnBack)+10
$Width=getWidth(btnBack)
```

---

## 数据类型



| 类型 | 语法 / 说明 |
|---|---|
| `text` | `@` 表示换行（在控件的 `Text` 属性解析时替换，`XNAControl.cs`）。`\@` 与 `\semicolon` 转义**只**在翻译系统（`FromIniString`）中生效；**不**适用于控件 INI 文件，其中裸 `;` 始终表示注释。 |
| `color` | `R,G,B` 或 `R,G,B,A`。所有分量必须在 `0` 到 `255` 之间。示例：`255,255,255`、`255,255,255,128`。 |
| `boolean` | 按首字符解析：`t`、`y`、`1`、`a`、`e` ⇒ `true`；`n`、`f`、`0` ⇒ `false`。其他字符回退到控件的默认值。`true`/`false`、`yes`/`no`、`1`/`0` 均被接受。 |
| `integer` | `System.Int32`。 |
| `float` | `System.Single`。 |
| `N integers` / `N floats` | 对应类型的 `N` 个值，用逗号**不带空格**分隔，例如 `0,0` 或 `0.0,0.0`。 |
| `comma-separated strings` | 用逗号不带空格分隔的字符串，例如 `one,two,three`。 |
| `string:type` | 用冒号分隔的两部分；用于 `$CC` 与 `ColumnX` 等键（参见各自章节）。 |

---

## 从 INI 创建控件



### `$CC` 子控件（INItializableWindow）



控件段中任何以 `$CC` 开头的键都会创建一个子控件。值格式为 `ControlName:ControlType`：

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

规则：

- 子控件自己的段（以 `ControlName` 命名）随后会被递归解析，因此子控件也可以拥有自己的 `$CC` 子控件。
- `ControlType` 必须是已注册的控件类型名（参见下方的控件类型）。类型通过 `Type.Name` 查找。
- **子控件名只能包含字母、数字和下划线。** 任何其他字符都会抛出 `INIConfigException`。
- `XNAScrollPanel` 的子控件会自动挂载到它的内容面板上。
- `$CC` 子控件的 `DrawOrder` 被设置为 `-Children.Count`（负数），因此由 INI 创建的控件绘制在代码创建的控件之前。

### `$Include` 指令（INItializableWindow）



控件段可以通过 `$Include` 指令合并另一个 INI 文件中的键。键名为 `$Include` 后跟任意文本；值为被包含文件的路径。被包含的键**覆盖**段中已存在的同名键：

```ini
[MyWindow]
$Include00=SpawnGameOptions.ini

; MyWindow.ini
```

路径解析：

- 值中的 `$THEME_DIR$` 会被替换为主题专用资源路径。
- 否则路径相对于当前 INI 文件所在目录解析。

处理完成后，`$Include` 键会从段中移除。如果被包含的文件不存在，或其中没有与当前控件同名的段，则记录一条错误并继续解析。

### 额外控件



有两种机制可以添加未在宿主段中声明的控件：

1. **`[$ExtraControls]` 段（INItializableWindow）** — 以 `$CC` 开头的键按相同的 `Name:Type` 格式定义额外控件。仅当没有同名子控件存在时才创建。

```ini
[MyWindow]
; ...

[$ExtraControls]
$CC00=pnlExtra:XNAClientPanel
```

2. **`[Name]ExtraControls` 段（XNAWindowBase / XNAOptionsPanel）** — 由 `XNAWindow` 派生窗口与 `XNAOptionsPanel` 使用。键是任意的（值为 `Name:Type`）。额外控件支持递归：作为额外控件创建的控件，只要设置了 `EnabledExtraControls`，就可以通过它自己的 `[Name]ExtraControls` 段再托管额外控件。

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

- `EnabledExtraControls`（boolean，`XNAWindowBase` 默认为 `false`；**`XNAOptionsPanel` 默认为 `true`**）控制控件自身是否托管额外控件。
- 当 `INItializableWindow` 作为额外控件创建时，它的 `ExternalIniFile` 会被设为宿主窗口的 INI 文件，以便从同一文件初始化。

### 控件类型



可通过 INI 创建的控件按其 `Type.Name` 注册（参见 `DXMainClient/DXGUI/GameClass.cs`）。已注册的名称如下：

`XNAControl`、`XNAButton`、`XNAInteractionButton`、`XNAClientButton`、`XNAClientCheckBox`、`XNAClientDropDown`、`XNALinkButton`、`XNAExtraPanel`、`XNACheckBox`、`XNADropDown`、`XNAClientTabControl`、`XNATabControl`、`XNALabel`、`XNALinkLabel`、`XNAClientLinkLabel`、`XNAListBox`、`XNAMultiColumnListBox`、`XNAPanel`、`XNAScrollPanel`、`XNAProgressBar`、`XNASuggestionTextBox`、`XNATextBox`、`XNATextBlock`、`XNATrackbar`、`XNAChatTextBox`、`ChatListBox`、`INItializableWindow`、`GameLobbyCheckBox`、`GameLobbyDropDown`、`CampaignCheckBox`、`CampaignDropDown`、`SettingCheckBox`、`SettingDropDown`、`FileSettingCheckBox`、`FileSettingDropDown`，以及单例窗口（`LoadingScreen`、`MainMenu`、`TopBar`、`OptionsWindow`、`CnCNetLobby`、`CnCNetGameLobby`、`SkirmishLobby`、`MapPreviewBox`、`CampaignTagSelector` 等）。

---

## 基础控件属性



> [!WARNING]
> 不要不修改就复制粘贴下面的代码片段 — 它们仅用于说明每个属性的用法。例如 `X`/`Y` 与 `Location` 冲突，`BackgroundTexture` 与 `SolidColorBackgroundTexture` 冲突，等等。

> [!NOTE]
> **属性顺序很重要。** 依赖控件尺寸的属性（如 `DistanceFromRightBorder`、`FillWidth`）必须写在设置该尺寸的属性（`Width`/`Height`/`Size`）**之后**。INI 键按文件中的顺序依次应用。

### [XNAControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAControl.cs)



所有控件元素的基类。

```ini
[SOMECONTROL]                      ; XNAControl
X=                                 ; integer,    控件相对于其父控件的 X 坐标。
Y=                                 ; integer,    控件相对于其父控件的 Y 坐标。
Location=                          ; 2 integers, 控件的 X 与 Y 坐标。
Width=                             ; integer,    控件的宽度。
Height=                            ; integer,    控件的高度。
Size=                              ; 2 integers, 控件的宽度与高度。
Text=                              ; text,       要显示的文本（按钮、标签等）。`@` 变成换行。
Visible=true                       ; boolean,    控件默认是否可见。设置 `Visible` 会把 `Enabled` 设为相同值。
Enabled=true                       ; boolean,    控件默认是否可交互。
DistanceFromRightBorder=0          ; integer,    控件右边缘与父控件右边缘的距离。
                                   ;             必须有父控件；无父控件时静默忽略。
DistanceFromBottomBorder=0         ; integer,    控件底边缘与父控件底边缘的距离。
                                   ;             必须有父控件；无父控件时静默忽略。
FillWidth=0                        ; integer,    将宽度设为 `Parent.Width - X - value`（无父控件时用窗口的
                                   ;             渲染分辨率宽度）。
FillHeight=0                       ; integer,    将高度设为 `Parent.Height - Y - value`（无父控件时用窗口的
                                   ;             渲染分辨率高度）。
DrawOrder=0                        ; integer,    父控件子控件之间的层级顺序。用 `int.Parse` 解析；无效值抛异常。
UpdateOrder=0                      ; integer,    父控件子控件之间的更新顺序（更大的先更新）。
RemapColor=255,255,255             ; color,      应用到控件纹理的主题 remap 颜色。
ControlDrawMode=UniqueRenderTarget ; string,     只有精确值 `UniqueRenderTarget` 有效：将控件绘制到自己的
                                   ;             渲染目标上。任何其他值（包括 `Normal`）都会被忽略，控件保持
                                   ;             默认行为——绘制到与父控件相同的目标上。
```

### [XNAPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAPanel.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMEPANEL]                  ; XNAPanel
BorderColor=196,196,196      ; color,      边框颜色。
AlphaRate=0.01               ; float,      每 100 毫秒的透明度变化率；透明面板会以此速率变为不透明。
BackgroundTexture=           ; string,     按文件名（含扩展名）加载的纹理。如果在任何资源搜索路径中都找不到，
                             ;             则返回一个占位纹理。
SolidColorBackgroundTexture= ; color,      用拉伸的纯色纹理替换背景。
DrawBorders=true             ; boolean,    启用/禁用边框绘制。边框默认启用。
Padding=                     ; 4 integers, CSS 风格内边距 `left,top,right,bottom`：扩大控件矩形，并按 left/top
                             ;             偏移所有现有子控件。
DrawMode=Stretched           ; enum (Tiled | Centered | Stretched),
                             ;             背景纹理绘制模式（默认 Stretched）。
```

### [XNAExtraPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAExtraPanel.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMEEXTRAPANEL]   ; XNAExtraPanel
BackgroundTexture= ; string, 与 XNAPanel 的 BackgroundTexture 相同。当面板宽度/高度为 0 时，
                   ;         自动调整为纹理尺寸。
```

### [XNATextBlock](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBlock.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMETEXTBLOCK]       ; XNATextBlock
TextColor=196,196,196 ; color, 文本颜色。
```

### [XNAIndicator](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAIndicator.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMEINDICATOR]            ; XNAIndicator
FontIndex=0                ; integer, 字体列表中加载的字体索引。默认 `0`。
HighlightColor=255,255,255 ; color,   光标悬停在指示器上方时的文本颜色。
AlphaRate=0.1              ; float,   每 100 毫秒的透明度变化率。
```

### [XNALabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNALabel.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMELABEL]            ; XNALabel
RemapColor=255,255,255 ; color,   TextColor 的别名 — `RemapColor` 键会被拦截并设置文本颜色。
TextColor=196,196,196  ; color,   文本颜色。
FontIndex=0            ; integer, 字体列表中加载的字体索引。
AnchorPoint=0.0,0.0    ; 2 floats, 文本起始绘制点。
TextShadowDistance=1.0 ; float,   文本与其阴影之间的距离（默认 1.0）。
TextAnchor=            ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER | CENTER),
                       ;          标签绘制框内的文本锚点。`CENTER` = HORIZONTAL_CENTER | VERTICAL_CENTER。
```

### [XNAButton](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAButton.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMEBUTTON]               ; XNAButton
TextColorIdle=255,255,255  ; color,  光标不在按钮上方时的文本颜色。
TextColorHover=255,255,255 ; color,  光标在按钮上方时的文本颜色。
HoverSoundEffect=          ; string, 悬停时播放的音效文件。
ClickSoundEffect=          ; string, 点击时播放的音效文件。
AdaptiveText=true          ; boolean, 客户端是否调整文本起始位置以填满所有空闲空间。默认 `true`。
AlphaRate=0.01             ; float,  每 100 毫秒的透明度变化率。
FontIndex=0                ; integer, 字体列表中加载的字体索引。
IdleTexture=               ; string,  空闲状态纹理文件名。纹理加载成功后，按钮的 ClientRectangle 会被调整为
                           ;          纹理尺寸。
HoverTexture=              ; string,  悬停状态纹理文件名。
TextShadowDistance=1.0     ; float,   文本与其阴影之间的距离（默认 1.0）。
```

### [XNAProgressBar](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAProgressBar.cs)



_（继承自 [XNAPanel](#xnapanel)）_

`XNAProgressBar` 没有定义任何 INI 专属属性；它只使用继承的面板/基础属性。

### [XNALinkLabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNALinkLabel.cs)



_（继承自 [XNALabel](#xnalabel)）_

```ini
[SOMELINKLABEL]     ; XNALinkLabel
IdleColor=          ; color,  光标不在标签上方时的文本颜色（默认：主题文本颜色）。
HoverColor=         ; color,  光标在标签上方时的文本颜色（默认：主题备用颜色）。
DrawUnderline=true  ; boolean, 在文本下方绘制下划线。默认 `true`。
```

### [XNACheckBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNACheckBox.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMECHECKBOX]             ; XNACheckBox
FontIndex=0                ; integer, 字体列表中加载的字体索引。
IdleColor=196,196,196      ; color,   光标不在复选框上方时的文本颜色。
HighlightColor=255,255,255 ; color,   光标在复选框上方时的文本颜色。
AlphaRate=0.1              ; float,   每 100 毫秒的透明度变化率。
AllowChecking=true         ; boolean, 允许用户勾选/取消勾选复选框。
Checked=true               ; boolean, 默认勾选状态。
TextPadding=5              ; integer, 复选框与其文本之间的水平距离。默认 `5`。
```

### [XNADropDown](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNADropDown.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMEDROPDOWN]                  ; XNADropDown
OpenUp=false                    ; boolean, 定义下拉框是否向上展开。默认 `false`。
DropDownTexture=                ; string,  下拉框关闭时使用的纹理。
DropDownOpenTexture=            ; string,  下拉框打开时使用的纹理。
ItemHeight=                     ; integer, 每个选项的像素高度。默认为主题值
                                ;          （UISettings.DropDownDefaultItemHeight）或选项文本高度 + 1。
ClickSoundEffect=               ; string,  打开下拉框时播放的音效文件。
FontIndex=0                     ; integer, 字体列表中加载的字体索引。
BorderColor=196,196,196         ; color,   打开状态下拉框的边框颜色。
FocusColor=64,64,64             ; color,   光标所在选项的背景颜色。
BackColor=0,0,0                 ; color,   打开状态下拉框的背景颜色。
DisabledItemColor=169,169,169   ; color,   禁用选项的文本颜色。
ShowEllipsisOnOverflow=false    ; boolean, 为超出下拉框的选项文本追加省略号。默认 `false`。
EnableScrollBar=true            ; boolean, 当选项列表长于下拉框时显示滚动条。默认 `true`。
ScrollBarWidth=12               ; integer, 滚动条的像素宽度。默认 `12`。
ScrollBarThumbColor=            ; color,   滚动条滑块颜色（默认：主题值）。
ScrollBarTrackColor=            ; color,   滚动条轨道颜色（默认：主题值）。
ScrollBarBorderColor=           ; color,   滚动条边框颜色（默认：主题值）。
ScrollBarThumbPadding=2         ; integer, 滑块与滚动条边框之间的内边距。默认 `2`。
ScrollStep=3                    ; integer, 每次滚动步进的像素数。默认 `3`。
MaxVisibleItems=5               ; integer, 出现滚动条前可见的选项数量。默认 `5`。
OptionX=                        ; string,  添加一个给定文本的选项。`X` 是任意文本，例如 `Option_FirstOption`。
; Option_FirstOption=1
; Option_SecondOption=two
; Option_ThirdOption=33333
```

### [XNATabControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATabControl.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMETABCONTROL]              ; XNATabControl
RemapColor=255,255,255        ; color,   标签文本颜色（TextColor 的别名）。
TextColor=255,255,255         ; color,   标签文本颜色。
TextColorDisabled=169,169,169 ; color,   禁用标签的文本颜色。
DisabledTabIndexN=false       ; boolean, 禁用索引为 `N` 的标签（`N` 必须是数字）。如果当前选中的标签
                              ;          被禁用，选择会移动到下一个可选的标签。
SetTabCount=3                 ; integer, 创建/移除占位标签，使控件恰好有 `N` 个标签。
TabDirection=Horizontal       ; enum (Horizontal | Vertical), 标签布局方向。默认 `Horizontal`。
ClickSoundEffect=             ; string,  标签的默认点击音效（单个标签的音效会覆盖它）。
IdleTexture=                  ; string,  标签的默认空闲纹理。
ClickTexture=                 ; string,  标签的默认按下纹理（别名：PressedTexture）。
PressedTexture=               ; string,  ClickTexture 的别名。
FontIndex=0                   ; integer, 默认标签字体索引。
TabText=                      ; text,    没有单独文本的标签使用的默认文本。
; DisabledTabIndex0=true
```

每个标签的属性使用点分隔形式 `Tab{N}.{Property}`（如果索引不存在，标签会被按需创建）：

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

支持的逐标签属性：`ClickSoundEffect`、`IdleTexture`、`ClickTexture`/`PressedTexture`、`FontIndex`、`Text`/`TabText`、`Selectable`（boolean，默认 `true`）。

### [XNATextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBox.cs)



_（继承自 [XNAControl](#xnacontrol)）_

```ini
[SOMETEXTBOX]                ; XNATextBox
MaximumTextLength=2147483647 ; integer, 最大输入长度（字符数）（默认为 int.MaxValue）。
```

### [XNASuggestionTextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNASuggestionTextBox.cs)



_（继承自 [XNATextBox](#xnatextbox)）_

```ini
[SOMESUGGESTIONTEXTBOX] ; XNASuggestionTextBox
Suggestion=             ; string, 文本框为空时显示的背景提示文本。
```

### [XNAListBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAListBox.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMELISTBOX]                   ; XNAListBox
EnableScrollbar=true            ; boolean, 显示内置滚动条。默认 `true`。
DrawSelectionUnderScrollbar=false ; boolean, 在滚动条区域下方绘制选中项的高亮。默认 `false`。
AllowMultiLineItems=true        ; boolean, 为 `false` 时长选项只显示第一行。仅影响新添加的选项。
AllowRightClickUnselect=true    ; boolean, 允许右键取消选中当前选中项。默认 `true`。
FontIndex=0                     ; integer, 字体列表中加载的字体索引。
LineHeight=                     ; integer, 单个选项行的像素高度（最小 1）。
TextBorderDistance=3            ; integer, 选项文本与列表框边框之间的水平距离。默认 `3`。
```

### [XNAMultiColumnListBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAMultiColumnListBox.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMEMULTICOLUMNLISTBOX]         ; XNAMultiColumnListBox
FontIndex=0                      ; integer,        字体列表中加载的字体索引。
LineHeight=                      ; integer,        单个选项行的像素高度（最小 1）。
DrawSelectionUnderScrollbar=yes  ; boolean,        在滚动条区域下方绘制最后一列选中项的高亮。
ColumnWidthN=                    ; integer,        索引为 `N` 的列的像素宽度（N 从 0 开始）。
ColumnX=                         ; HeaderText:Width, 定义一列；`X` 是任意文本。值为 `HeaderText` + `:` + 宽度，
                                 ;                 例如 `Column0=Player:100`（分隔符是冒号，不是逗号）。
ListBoxYAttribute:Attrname=Value ; string,         旨在为内部单列列表框 `Y` 设置属性 `Attrname`
                                 ;                 （例如 `ListBox0Attribute:LineHeight=20`）。`Attrname`
                                 ;                 由 XNAListBox 解析。[已知问题] 当前的前缀检查比较了错误的
                                 ;                 子串（`"Attribute:"` 对 `":Attribute"`）且永不匹配，因此
                                 ;                 该键当前不可用。
```

### [XNATrackbar](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATrackbar.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMETRACKBAR] ; XNATrackbar
MinValue=0     ; integer, 最小值。
MaxValue=10    ; integer, 最大值。
Value=0        ; integer, 默认值。
ClickSound=    ; string,  点击轨道条时播放的音效文件。
```

### [XNAScrollPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAScrollPanel.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMESCROLLPANEL]       ; XNAScrollPanel
AllowKeyboardInput=true ; boolean, 允许使用方向键滚动。默认 `true`。
AllowScroll=true,true   ; 2 booleans, 允许在 X 和 Y 轴上滚动（`X,Y`）。
AllowScrollX=true       ; boolean, 允许水平滚动。默认 `true`。
AllowScrollY=true       ; boolean, 允许垂直滚动。默认 `true`。
ScrollStep=             ; integer, 每次滚动的像素距离。
OverscrollMargin=0,0    ; 2 integers, 内容在 X 和 Y 方向上可以超出面板边界的距离。
OverscrollMarginX=0     ; integer, 水平方向的可越界边距。
OverscrollMarginY=0     ; integer, 垂直方向的可越界边距。
DrawBorders=true        ; boolean, 启用/禁用边框绘制。
; 该控件会故意忽略 Padding。
```

### [XNAContextMenu](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAContextMenu.cs)



_（继承自 [XNAPanel](#xnapanel)）_

```ini
[SOMECONTEXTMENU]        ; XNAContextMenu
ItemHeight=              ; integer, 每个选项的像素高度。默认为主题值
                         ;          （UISettings.ContextMenuDefaultItemHeight）或选项文本高度 + 1。
FontIndex=0              ; integer, 选项文本的字体索引。
HintFontIndex=0          ; integer, 选项提示文本的字体索引。
TextHorizontalPadding=1  ; integer, 选项内部的水平文本内边距。
TextVerticalPadding=1    ; integer, 选项内部的垂直文本内边距。
```

### XNAClientTabControl

_（继承自 [XNATabControl](#xnatabcontrol)）_ — `XNAClientTabControl`（ClientGUI）增加了标签文本的本地化；它没有额外的 INI 键。

### XNAPasswordBox / XNATimerControl

这些控件没有定义任何 INI 专属属性（它们继承各自基控件的属性）。

---

## 客户端控件属性



客户端控件（`ClientGUI/`）在上述 XNAUI 控件的基础上增加客户端专属属性。

### [XNAClientButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientButton.cs)



_（继承自 [XNAButton](#xnabutton)）_

```ini
[SOMECLIENTBUTTON] ; XNAClientButton
MatchTextureSize=  ; boolean, 将按钮的宽度和高度设置为与纹理尺寸一致。
ToolTip=           ; text,    悬停按钮时显示的提示文本。
```

### [XNAClientToggleButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientToggleButton.cs)



_（继承自 [XNAButton](#xnabutton)）_

```ini
[SOMECLIENTTOGGLEBUTTON] ; XNAClientToggleButton
CheckedTexture=          ; string, 切换按钮被勾选时显示的纹理。
UncheckedTexture=        ; string, 切换按钮未被勾选时显示的纹理。
ToolTip=                 ; text,   提示文本。
```

### [XNALinkButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNALinkButton.cs)



_（继承自 [XNAClientButton](#xnaclientbutton)）_

```ini
[SOMELINKBUTTON] ; XNALinkButton
URL=             ; string, 点击时由操作系统 Shell 打开的 URL（Windows，以及未设置 UnixURL 的其他系统）。
UnixURL=         ; string, Unix 类系统上打开的 URL（覆盖 URL）。
Arguments=       ; string, 传给 Shell 的参数，以空格分隔。
```

### [XNAClientLinkLabel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientLinkLabel.cs)



_（继承自 [XNALinkLabel](#xnalinklabel)）_

```ini
[SOMECLIENTLINKLABEL] ; XNAClientLinkLabel
ToolTip=              ; text,   提示文本。
URL=                  ; string, 点击时由操作系统 Shell 打开的 URL。
UnixURL=              ; string, Unix 类系统上打开的 URL（覆盖 URL）。
HoverSoundEffect=     ; string, 光标进入标签时播放的音效文件。
ClickSoundEffect=     ; string, 点击标签时播放的音效文件。
```

### [XNAClientCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientCheckBox.cs)



_（继承自 [XNACheckBox](#xnacheckbox)）_

```ini
[SOMECLIENTCHECKBOX] ; XNAClientCheckBox
ToolTip=             ; text, 提示文本。
```

### [XNAClientDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientDropDown.cs)



_（继承自 [XNADropDown](#xnadropdown)）_

```ini
[SOMECLIENTDROPDOWN] ; XNAClientDropDown
ToolTip=            ; text, 提示文本。下拉框打开时提示被抑制。
```

### [XNAClientColorDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientColorDropDown.cs)



_（继承自 [XNAClientDropDown](#xnaclientdropdown)）_

```ini
[SOMECOLORDROPDOWN] ; XNAClientColorDropDown
ItemsDrawMode=TextAndIcon         ; enum (Text | Icon | TextAndIcon),
                                  ;         用于渲染选项的纹理与文本组合方式。默认 TextAndIcon。
RandomColorTexture=randomicon.png ; string,  随机颜色选项（选项 0）的纹理。
DisabledItemTexture=              ; string,  禁用选项的纹理；默认由 DisabledItemColor 生成纹理。
ColorTextureHeight=               ; int,     颜色图标的像素高度。
ColorTextureWidth=                ; int,     颜色图标的像素宽度。
```

### [XNAClientPreferredItemDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientPreferredItemDropDown.cs)



_（继承自 [XNAClientDropDown](#xnaclientdropdown)）_

```ini
[SOMEPREFERREDITEMDROPDOWN] ; XNAClientPreferredItemDropDown
PreferredItemLabel=         ; string, 追加到首选选项文本后的标签。
```

### [XNAInteractionButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAInteractionButton.cs)



_（继承自 [XNAClientButton](#xnaclientbutton)）_

与本地文件系统上的文件和进程交互的按钮。

```ini
[SOMEINTERACTIONBUTTON]               ; XNAInteractionButton
OpenFiles=                            ; 逗号/分号分隔的字符串，点击时用操作系统 Shell 打开的文件列表。
ExitProcess=                          ; 逗号/分号分隔的字符串，点击时终止的进程名列表。
TargetSuffixes=                       ; 逗号/分号分隔的字符串，OpenFiles 允许的文件扩展名；
                                      ;         缺少的前导点会自动补上。
                                      ;         默认为 `.exe,.bat,.txt,.ini,.log`。
OpenDisableButtonTime=0               ; integer, 打开文件后按钮保持禁用的秒数。默认 `0`。
ExitDisableButtonTime=0               ; integer, 退出进程后按钮保持禁用的秒数。默认 `0`。
ProcessExists.AllowedButtonChecks=    ; 逗号分隔的字符串，仅当这些进程至少有一个在运行时按钮才可用。
ProcessExists.DisableButtonChecks=    ; 逗号分隔的字符串，只要这些进程至少有一个在运行按钮就被禁用。
TextColorDisabled=                    ; color,   按钮禁用状态的文本颜色。
```

### [XNAOptionsPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAOptionsPanel.cs)



_（继承自 [XNAScrollPanel](#xnascrollpanel)）_

```ini
[SOMEOPTIONSPANEL]  ; XNAOptionsPanel
EnableScrolling=true ; boolean, 为 `false` 时移除内部滚动容器，子控件直接托管在面板上。
```

`XNAOptionsPanel` 默认解析 `[Name]ExtraControls`，并将 INI 属性处理转发给其可滚动内容中的控件，因此 XNAOptionsPanel 控件下列出的 `Setting*` 控件可以声明为 `$CC` 子控件或额外控件。

---

## 动态控件属性与表达式



动态控件属性**可以**使用常量与算术表达式。任何使用 INItializable 或表达式启用模型的窗口中的控件都可以使用它们（参见INI 文件的解析方式）。

```ini
$X=10            ; integer, 控件的 X 坐标。值按表达式解析。
$Y=20            ; integer, 控件的 Y 坐标。
$Width=50        ; integer, 控件的宽度。
$Height=10       ; integer, 控件的高度。
$TextAnchor=LEFT ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER),
                 ;         XNALabel 绘制框的文本锚点。
$AnchorPoint=0,0 ; 2 expressions, XNALabel 的文本起始绘制点。每个分量都按表达式解析。
$LeftClickAction=Disable ; 左键点击时执行的动作。唯一支持的动作是 `Disable`，它会禁用并隐藏控件。
```

带常量的示例：

```ini
[lblExample]
$X=MY_X_CONSTANT
$Y=MY_Y_CONSTANT
$Width=MY_WIDTH_CONSTANT
$Height=MY_HEIGHT_CONSTANT
```

> [!IMPORTANT]
> `$` 属性与普通对应属性的交互方式取决于窗口模型：
> - **XNAWindowBase 模型**（例如 `XNAWindow`、`XNAOptionsPanel`）：表达式处理在基础属性处理**之后再次运行**，因此 `$X`/`$Y`/`$Width`/`$Height` 总是胜过 `X=`/`Y=`/`Width=`/`Height=`。
> - **INItializableWindow 模型**：键按它们在 INI 文件中出现的顺序应用，因此同一属性的最后一次出现生效。

---

## 晚期属性（控件联动）



晚期属性在整棵控件树创建完成后将控件彼此关联，因此控件可以引用 INI 文件中**定义在其之后**的控件。它们由 `ClientGUI/INIControlLinkHelper.cs` 处理，适用于 `INItializableWindow` 与所有 `XNAWindowBase` 派生窗口。

### 按钮与复选框 — `$Toggles` / `$Opens` / `$Exits`



```ini
[btnToggle]        ; XNAButton / XNACheckBox
$Toggles=pnlExtra,btnSecondary   ; 逗号分隔的控件名。
$Opens=pnlDetail                 ; 逗号分隔的控件名，状态激活期间显示（Visible+Enabled）。
$Exits=pnlHint                   ; 逗号分隔的控件名，状态激活期间隐藏。
```

行为：

- **复选框**：复选框状态变化时触发 `CheckedChanged`。`$Toggles` 翻转每个列出控件的 `Visible`/`Enabled`；勾选时 `$Opens` 显示列出的控件、`$Exits` 隐藏列出的控件（反之亦然）。初始状态由复选框的初始 `Checked` 值应用。
- **按钮**：每次点击时 `$Toggles` 翻转 `Visible`/`Enabled`；点击时 `$Opens` 显示、`$Exits` 隐藏。
- 查找在控件所在的窗口/面板内递归进行，因此窗口内任何位置定义的目标都能被找到。

### 标签页控件 — `$ToggleN` / `$OpenN` / `$ExitN`



```ini
[tabOptions]         ; XNATabControl
$Toggle0=pnlTab0     ; 仅当选中的是标签 0 时可见/可用。
$Toggle1=pnlTab1
$Open0=pnlCommon     ; 选中的是标签 0 时强制可见。
$Exit0=pnlLegacy     ; 选中的是标签 0 时强制隐藏。
; 简单顺序形式（索引 0、1、2、...）：
$Toggles=pnlTab0,pnlTab1,pnlTab2
```

当选中标签变化时，所有映射的控件都会被更新：只有列在当前选中标签（`$ToggleN`/`$OpenN`）下的控件可见/可用，列在 `$ExitN` 中的控件对该标签额外隐藏。

### 下拉框 — `$ToggleN` / `$OpenN` / `$ExitN`



同样的机制适用于 `XNADropDown`，只是使用选中选项的索引而不是标签索引：

```ini
[ddMode]             ; XNADropDown
$Toggle0=pnlMode0
$Toggle1=pnlMode1
$Toggles=pnlMode0,pnlMode1
```

### 文本框 — `NextControl` / `PreviousControl`



```ini
[tbUsername]   ; XNATextBox
NextControl=tbPassword

[tbPassword]
PreviousControl=tbUsername
NextControl=btnLogin
```

按 `Tab` 将焦点移到 `NextControl`；按 `Shift+Tab` 将焦点移到 `PreviousControl`。

---

## 特殊控件



有些控件只在特定条件下可用。

### [CoopBriefingBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/CoopBriefingBox.cs)



_（继承自 [XNAPanel](#xnapanel)）_ — 游戏大厅的联合作战任务简报面板。

```ini
; GameLobbyBase.ini
[MapPreviewBox_CoopBriefingBox]
FontIndex=0 ; integer, 字体列表中加载的字体索引。
```

### GameLobbyBase 控件



以下控件只能作为 `GameLobbyBase` 及其派生控件的子控件使用。

此外，大厅窗口本身（`[GameLobbyBase]` / `[SkirmishLobby]` / `[CnCNetGameLobby]` 段，它们会链到
`GameLobbyBase.ini`）支持由 `GameLobbyBase.ParseControlINIAttribute` 解析的以下布局键：

```ini
[LOBBY_WINDOW]                 ; GameLobbyBase
PlayerOptionLocationX=         ; integer, 玩家选项控件的 X 坐标。
PlayerOptionLocationY=         ; integer, 玩家选项控件的 Y 坐标。
PlayerOptionVerticalMargin=    ; integer, 玩家选项行之间的垂直间距。
PlayerOptionHorizontalMargin=  ; integer, 玩家选项控件之间的水平间距。
CaptionLocationY=              ; integer, 选项标题的 Y 坐标。
PlayerNameWidth=               ; integer, 玩家名称控件的宽度。
SideWidth=                     ; integer, 阵营选择器的宽度。
ColorWidth=                    ; integer, 颜色选择器的宽度。
StartWidth=                    ; integer, 起始位置选择器的宽度。
TeamWidth=                     ; integer, 队伍选择器的宽度。
```

#### [GameSessionCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/GameSessionCheckBox.cs)



_（继承自 [XNAClientCheckBox](#xnaclientcheckbox)）_

用于游戏大厅的游戏选项复选框。支持向 CnCNet 大厅广播游戏选项，并在游戏列表与过滤器中显示。

```ini
[SOMEGAMESESSIONCHECKBOX]                  ; GameSessionCheckBox
OptionName=                                ; string,  该选项的显示名称（用于游戏信息面板）。
SpawnIniOption=                            ; string,  复选框状态变化时写入的 spawn INI 选项。支持带索引的
                                           ;          变体 `SpawnIniOption0`、`SpawnIniOption1`、...
SpawnIniProject=Settings                   ; string,  选项写入的 spawn INI 段。默认 `Settings`。
EnabledSpawnIniValue=True                  ; string,  勾选时写入的 spawn INI 值。默认为 `True`。
DisabledSpawnIniValue=False                ; string,  未勾选时写入的 spawn INI 值。默认为 `False`。
CustomIniPath=                             ; string,  地图专属设置的自定义 INI 路径（支持带索引的变体）。
SpawnWriteCustom=false                     ; boolean, 将选项写入地图 INI（spawnmap.ini）而不是 spawn.ini。
CustomWriteSpawn=false                     ; boolean, SpawnWriteCustom 的反义（为兼容保留）。
SpawnIniValueCheck=false                   ; boolean, 为 `true` 时空值不会写入 spawn.ini。
Reversed=false                             ; boolean, 反转复选框行为。
Checked=false                              ; boolean, 初始勾选状态。
MapScoringMode=Irrelevant                  ; enum (Irrelevant | DenyWhenChecked | DenyWhenUnchecked),
                                           ;          控制该设置是否影响地图评分。
BroadcastToLobby=false                     ; boolean, 将该复选框包含在发送到 CnCNet 大厅的 GAME 广播中。
ShowInGameList=false                       ; boolean, 在游戏列表中显示图标/文本。
ShowInGameListOnRight=false                ; boolean, 在游戏列表的右侧显示图标。仅当 `ShowInGameList`
                                           ;          为 `true` 时生效。
ShowInGameInformationPanel=false           ; boolean, 在游戏信息面板中显示图标/文本。
ShowInGameInformationPanelAsIconOnly=false ; boolean, 在游戏信息面板中只显示图标。仅当
                                           ;          `ShowInGameInformationPanel` 为 `true` 时生效。
ShowIconInGameLobby=false                  ; boolean, 在游戏大厅控件中显示图标。
ShowInFilters=false                        ; boolean, 在过滤器面板中显示该设置。
EnabledIcon=                               ; string,  设置启用时图标的纹理名。
DisabledIcon=                              ; string,  设置禁用时图标的纹理名。
SortOrder=0                                ; integer, 游戏信息面板与游戏列表中图标的显示顺序。
                                           ;          值越小越靠前。
ParentCheckBoxName=                        ; string,  父复选框名（单一父形式）。该值不含逗号时使用单一父的
                                           ;          "门控"语义（见下文）。
ParentCheckBoxName=chkA,chkB              ; 逗号分隔的父复选框名列表，按索引 0、1、2、... 存储
                                           ;          （索引形式语义，见下文）。等价于分别写
                                           ;          ParentCheckBoxName0=chkA、ParentCheckBoxName1=chkB、...
ParentCheckBoxNameN=                       ; string,  索引形式：索引 `N`（N = 0、1、2、...）的父复选框名。
ParentCheckBoxRequiredValue=true           ; boolean, 父复选框所需的状态。单一父形式：作用于该单一父复选框。
                                           ;          索引形式：逗号分隔的值按顺序映射到各个索引父复选框（单个值
                                           ;          复制给全部；最后一个值补齐缺失项）；也可使用带索引的键
                                           ;          ParentCheckBoxRequiredValueN。未指定的索引默认为 `true`。
ParentCheckBoxTexture=checkedTex,uncheckedTex ; string, "选中纹理,未选中纹理" 纹理对（或单一纹理同时用于两种
                                           ;          状态），在本复选框被父依赖禁用时显示。也可使用带索引的键
                                           ;          ParentCheckBoxTextureN。
ParentChecked=false                        ; boolean, 本复选框被父依赖禁用时显示的勾选状态。默认 `false`。
                                           ;          也可使用带索引的键 ParentCheckedN。
```

父依赖语义：

- **单一父形式**（`ParentCheckBoxName=chkX`，不含逗号）：*门控*语义。父复选框必须处于 `ParentCheckBoxRequiredValue`
  所需的状态，本复选框才可更改；否则本复选框被禁用、强制为 `ParentChecked` 状态，并用 `ParentCheckBoxTexture`
  的纹理显示。
- **索引形式**（逗号分隔的 `ParentCheckBoxName` 列表或 `ParentCheckBoxName{N}` 键）：*锁定*语义。当**所有**
  索引父复选框都处于各自所需状态时（找不到的父复选框视为不匹配；没有显式 `ParentCheckBoxRequiredValue{N}`
  的索引默认要求 `true`），本复选框被锁定 — 不可更改 — 其勾选状态取**最小**已配置索引的 `ParentChecked{N}`
  （回退到普通 `ParentChecked`），并用该索引的 `ParentCheckBoxTexture{N}`（回退到全局纹理对）显示。只要任一
  父复选框不匹配，本复选框就可以自由更改。

#### [CampaignCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignCheckBox.cs)



_（继承自 [GameSessionCheckBox](#gamesessioncheckbox)）_

在 `CampaignSelector.ini` 中为战役复选框使用此控件类型。继承 `GameSessionCheckBox` 的全部属性。注意：当 `ClientDefinitions.ini` 禁用 `CopyMissionsToSpawnmapINI` 时，战役复选框上的 `CustomIniPath` 会抛出异常。

```ini
[SOMECAMPAIGNCHECKBOX]                  ; CampaignCheckBox
ResetToDefaultOnGameExit=false          ; boolean, 游戏退出时将该复选框重置为其默认值。
```

#### [GameLobbyCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyCheckBox.cs)



_（继承自 [GameSessionCheckBox](#gamesessioncheckbox)）_

在 `GameLobbyBase.ini` 中为游戏大厅复选框使用此控件类型。继承 `GameSessionCheckBox` 的全部属性。

```ini
[SOMEGAMELOBBYCHECKBOX]           ; GameLobbyCheckBox
CheckedMP=false                   ; boolean, 专门应用于多人游戏的勾选状态。
DisallowedSideIndex=0,1           ; 逗号分隔的整数，此复选框勾选时不能选择的阵营索引。
                                  ;          别名：DisallowedSideIndices。
```

#### [GameSessionDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/GameSessionDropDown.cs)



_（继承自 [XNAClientDropDown](#xnaclientdropdown)）_

用于游戏大厅的游戏选项下拉框。支持向 CnCNet 大厅广播游戏选项，并在游戏列表与过滤器中显示。

```ini
[SOMEGAMESESSIONDROPDOWN]                  ; GameSessionDropDown
Items=                                     ; 逗号分隔的字符串，下拉框的选项值（tag）。
ItemLabels=                                ; 逗号分隔的字符串，选项的可选显示标签。
Icons=                                     ; 逗号分隔的字符串，选项图标的纹理名。数量应与选项数一致。
ItemN=                                     ; string, 索引为 `N` 的选项的 tag（覆盖 Items 条目）。
ItemLabelN=                                ; string, 索引为 `N` 的选项的显示标签。
SpawnIniOption=                            ; string,  根据选中项写入的 spawn INI 选项。支持带索引的
                                           ;          变体 `SpawnIniOption0`、...
SpawnIniProject=Settings                   ; string,  选项写入的 spawn INI 段。默认 `Settings`。
SpawnWriteCustom=false                     ; boolean, 将选项写入地图 INI（spawnmap.ini）而不是 spawn.ini。
SpawnIniValueCheck=false                   ; boolean, 为 `true` 时空值不会写入 spawn.ini。
DefaultIndex=0                             ; integer, 默认选中的选项索引。
DataWriteMode=BOOLEAN                      ; enum (INDEX | BOOLEAN | STRING | MAPCODE),
                                           ;          值写入 spawn INI 的方式：
                                           ;          INDEX - 选中的索引，BOOLEAN - `SelectedIndex > 0`，
                                           ;          STRING - 选中项的 tag，MAPCODE - 将选中项的 tag 作为
                                           ;          地图代码 INI 名交给 MapCodeHelper 应用。默认 `BOOLEAN`。
OptionName=                                ; string,  该选项的显示名称。
BroadcastToLobby=false                     ; boolean, 将该下拉框包含在发送到 CnCNet 大厅的 GAME 广播中。
ShowInGameList=false                       ; boolean, 在游戏列表中显示图标/文本。
ShowInGameListOnRight=false                ; boolean, 在游戏列表的右侧显示图标。仅当 `ShowInGameList`
                                           ;          为 `true` 时生效。
ShowInGameInformationPanel=false           ; boolean, 在游戏信息面板中显示图标/文本。
ShowInGameInformationPanelAsIconOnly=false ; boolean, 在游戏信息面板中只显示图标。仅当
                                           ;          `ShowInGameInformationPanel` 为 `true` 时生效。
ShowIconInGameLobby=false                  ; boolean, 在游戏大厅控件中显示图标。
ShowInFilters=false                        ; boolean, 在过滤器面板中显示该设置。
SortOrder=0                                ; integer, 游戏信息面板与游戏列表中图标的显示顺序。
                                           ;          值越小越靠前。
EnableRightInputBox=false                  ; boolean, 添加一个右键点击下拉框时打开的自定义值输入框。
                                           ;          为 `true` 时自定义选项会被追加到选项列表。
InputBoxDataMode=INTEGER                   ; enum (INTEGER | STRING), 自定义输入框的数据模式。
InputBoxIntegerScroll=true                 ; boolean, 输入框中整数滚动的总开关（鼠标与键盘）。
InputBoxIntegerScroll.Mouse=true           ; boolean, 启用鼠标滚轮滚动整数。
InputBoxIntegerScroll.KeyBoard=true        ; boolean, 启用上/下方向键滚动整数。
InputBoxIntegerScroll.Integer=1            ; integer, 滚动步进（鼠标与键盘）。
InputBoxIntegerScroll.MouseInteger=1       ; integer, 鼠标滚轮滚动步进。
InputBoxIntegerScroll.KeyBoardInteger=1    ; integer, 键盘滚动步进。
InputBoxIntegerStrict=false                ; boolean, 为 `true` 时输入过程中超出 [MinInputBoxInteger,
                                           ;          MaxInputBoxInteger] 范围的输入会被拒绝。
InputBoxIntegerRange=                      ; string,  接受整数的符号范围。值包含 `-` 允许负数，包含 `+`
                                           ;          允许正数；例如 `-+` 两者都允许，`-` 只允许负数，
                                           ;          `+` 只允许正数。
InputBoxIntegerRangeShow.Positive=false    ; boolean, 为正的自定义值显示 `+` 前缀。
InputBoxIntegerRangeShow.Negative=true     ; boolean, 为负的自定义值显示 `-` 前缀。
MinInputBoxInteger=                        ; integer, 接受的最小整数（确认时夹紧）。默认 int.MinValue。
MaxInputBoxInteger=                        ; integer, 接受的最大整数（确认时夹紧）。默认 int.MaxValue。
InputBoxCustomItems=1                      ; integer, 追加到选项列表的自定义值槽位数量。
InputBoxCustomItemsLabels=                 ; 逗号分隔的字符串，自定义槽位的标签（可用 `{0}` 表示值）。
InputBoxCustomDefaultItems=                ; 逗号分隔的字符串，自定义槽位的默认值。
```

#### [CampaignDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignDropDown.cs)



_（继承自 [GameSessionDropDown](#gamesessiondropdown)）_

在 `CampaignSelector.ini` 中为战役下拉框使用此控件类型。继承 `GameSessionDropDown` 的全部属性。战役任务不支持 `DataWriteMode=MAPCODE`。

#### [GameLobbyDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Multiplayer/GameLobby/GameLobbyDropDown.cs)



_（继承自 [GameSessionDropDown](#gamesessiondropdown)）_

在 `GameLobbyBase.ini` 中为游戏大厅下拉框使用此控件类型。继承 `GameSessionDropDown` 的全部属性；`DefaultIndex` 额外在主机与客户端用户之间同步。

### XNAOptionsPanel 控件



以下控件只能作为 `XNAOptionsPanel` 及其派生控件的子控件使用。它们将自身状态持久化到客户端的设置 INI（`SettingsFile`，默认 `Settings.ini`）。

#### [SettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingCheckBox.cs)



_（继承自 [XNAClientCheckBox](#xnaclientcheckbox)）_

```ini
[SOMESETTINGCHECKBOX]            ; SettingCheckBox
Checked=false                    ; boolean, 初始勾选状态（优先于 DefaultValue）。
DefaultValue=false               ; boolean, 复选框的默认状态。未设置 `Checked` 时使用。
SettingSection=CustomSettings    ; string,  设置保存到的设置 INI 段。默认 `CustomSettings`。
SettingKey=                      ; string,  设置保存到的设置 INI 键。默认在设置 `WriteSettingValue`
                                 ;          时为 `CONTROLNAME_Value`，否则为 `CONTROLNAME_Checked`。
WriteSettingValue=false          ; boolean, 写入特定字符串值而不是勾选状态。
EnabledSettingValue=             ; string,  设置 `WriteSettingValue` 且勾选时写入的值。
DisabledSettingValue=            ; string,  设置 `WriteSettingValue` 且未勾选时写入的值。
RestartRequired=false            ; boolean, 应用该设置是否需要重启客户端。
ParentCheckBoxName=              ; string,  必须处于所需状态的父复选框名（同一父控件下），本复选框才可用。
ParentCheckBoxRequiredValue=true ; boolean, 父复选框所需的状态。
ResetToDefaultOnGameExit=false   ; boolean, 游戏退出时将该设置重置为默认值。
```

#### [FileSettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingCheckBox.cs)



_（继承自 [XNAClientCheckBox](#xnaclientcheckbox)）_

状态变化时复制文件的文件设置复选框。

```ini
[SOMEFILESETTINGCHECKBOX]        ; FileSettingCheckBox
Checked=false                    ; boolean, 初始勾选状态。
DefaultValue=false               ; boolean, 复选框的默认状态。未设置 `Checked` 时使用。
SettingSection=CustomSettings    ; string,  设置保存到的设置 INI 段。默认 `CustomSettings`。
SettingKey=                      ; string,  设置保存到的设置 INI 键。默认在设置 `WriteSettingValue`
                                 ;          时为 `CONTROLNAME_Value`，否则为 `CONTROLNAME_Checked`。
RestartRequired=false            ; boolean, 应用该设置是否需要重启客户端。
ParentCheckBoxName=              ; string,  门控本复选框的父复选框名（同一父控件下）。
ParentCheckBoxRequiredValue=true ; boolean, 父复选框所需的状态。
CheckAvailability=false          ; boolean, 复选框能否（取消）勾选取决于要复制的文件是否实际存在。
ResetUnavailableValue=false      ; boolean, 与 `CheckAvailability` 一起设置时，被设为不可用值的复选框
                                 ;          会重置回 `DefaultValue`。
Reversed=false                   ; boolean, 反转复选框行为（为兼容保留）。
EnabledFileN=                    ; 逗号分隔的字符串，勾选时要复制的文件。`N` 从 0 开始递增，直到没有值。
                                 ;          格式：相对于游戏根目录的源路径、相对于游戏根目录的目标路径，
                                 ;          以及可选的文件操作选项。
DisabledFileN=                   ; 逗号分隔的字符串，未勾选时要复制的文件。格式与 `EnabledFileN` 相同。
```

#### [SettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingDropDown.cs)



_（继承自 [XNAClientDropDown](#xnaclientdropdown)）_

```ini
[SOMESETTINGDROPDOWN]  ; SettingDropDown
Items=                 ; 逗号分隔的字符串，下拉框显示的选项。
DefaultValue=0         ; integer, 默认选项索引。
SettingSection=        ; string,  设置保存到的设置 INI 段。默认 `CustomSettings`。
SettingKey=            ; string,  设置保存到的设置 INI 键。默认在设置 `WriteItemValue` 时为
                       ;          `CONTROLNAME_Value`，否则为 `CONTROLNAME_SelectedIndex`。
WriteItemValue=false   ; boolean, 将选中项的值（tag）写入设置 INI 键而不是索引。
RestartRequired=true   ; boolean, 应用该设置是否需要重启客户端。
```

#### [FileSettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingDropDown.cs)



_（继承自 [XNAClientDropDown](#xnaclientdropdown)）_

```ini
[SOMEFILESETTINGDROPDOWN]            ; FileSettingDropDown
Items=                               ; 逗号分隔的字符串，下拉框显示的选项。
DefaultValue=0                       ; integer, 默认选项索引。
SettingSection=CustomSettings        ; string,  设置保存到的设置 INI 段。
SettingKey=CONTROLNAME_SelectedIndex ; string,  设置保存到的设置 INI 键。
RestartRequired=false                ; boolean, 应用该设置是否需要重启客户端。
CheckAvailability=false              ; boolean, 能否选择某个选项取决于要复制的文件是否存在。
ResetUnavailableValue=false          ; boolean, 当前值变为不可用时自动调整设置值。
ItemXFileN=                          ; 逗号分隔的字符串，选中下拉框选项 `X` 时要复制的文件。
                                     ;          `N` 从 0 开始递增，直到没有值。格式与 `EnabledFileN`
                                     ;          相同（参见文件操作选项）。
```

#### 附录：文件操作选项



`FileSettingCheckBox` 与 `FileSettingDropDown` 定义的文件可用的文件操作选项（枚举 `FileOperationOption`，位于 `FileSourceDestinationInfo.cs`）：

| 选项 | 行为 |
|---|---|
| `AlwaysOverwrite` | 始终用源文件覆盖目标文件。 |
| `OverwriteOnMismatch` | 仅当两个文件不同时覆盖目标文件。 |
| `DontOverwrite` | 目标文件已存在时绝不覆盖。 |
| `KeepChanges` | 缓存目标文件，使玩家做的修改在关闭并重新启用该选项后仍然保留。 |
| `AlwaysOverwrite_LinkAsReadOnly` | 尝试创建指向源文件的硬链接（共享内容），链接失败时回退为复制。推荐用于 `opengl32.dll`、`d3d9.dll`、`dxgi.dll` 等二进制文件；不建议用于文本文件。链接存在期间，源与目标都会被标记为只读。 |

---

## 窗口



定义了自己 INI 段的 [XNAWindow](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindow.cs) 子类。

### [LoadingScreen](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/LoadingScreen.cs)



```ini
; LoadingScreen.ini
[LoadingScreen]
RandomBackgroundTextures=  ; 逗号分隔的字符串列表，
                           ; 用作 BackgroundTexture 随机选择的文件路径。
RandomBackgroundTexturesPath= ; string, 随机背景纹理的可选子路径前缀。为空或字面 `Resources` 时不加前缀；
                           ;          否则前缀会被拼接到路径前（例如 `Themes/MyTheme`）。
```

默认加载画面纹理从 `loadingscreen.png` 加载；当 `RandomBackgroundTextures` 非空时，随机选择其中一个纹理并覆盖
默认值。`[LoadingScreen]` 段缺失时窗口回退到 `GenericWindow.ini` 的 `[GenericWindow]` 段。

### [MainMenu](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/MainMenu.cs)



背景视频设置（仅 Windows 引擎）。所有键都从 `MainMenu.ini` 的 `[MainMenu]` 段读取。

```ini
[MainMenu]
BackgroundVideo=                 ; string,  背景视频文件的路径，解析优先级与主菜单主题相同
                                 ;          （[MainMenu] 键 > [General] MainMenuThemePath > 默认
                                 ;          MainMenu/mainmenubg.mp4）。
BackgroundVideoLooping=true      ; boolean, 视频是否循环（默认 true）。
BackgroundVideoMuted=false       ; boolean, 主题视频是否没有音轨。为 true 时视频永远不能
                                 ;          在音频优先级上超过菜单音乐。
BackgroundVideoVolume=100        ; float,   视频音量百分比，再乘以客户端音量设置（默认 100）。
BackgroundVideoFrameInterval=33  ; integer, 捕获帧之间的最小毫秒数；更小更流畅但更耗 CPU
                                 ;          （默认 33 = 约 30fps）。
BackgroundMusic=                 ; string,  菜单音乐文件的路径，解析优先级与 BackgroundVideo 相同。
BackgroundVideoHotkeys=true      ; boolean, 下面视频快捷键的总开关（默认 true）。
BackgroundVideoPauseHotkey=P     ; key,     主菜单为活动窗口时切换视频暂停/恢复状态（默认 P）。
                                 ;          设为 None 以禁用。
BackgroundVideoMuteHotkey=V      ; key,     切换视频音频静音状态（默认 V）。设为 None 以禁用。
```

> [!NOTE]
> `BackgroundVideoAlpha` 与 `BackgroundVideoAutoPlay` 会从本段解析但**当前未被使用** — 视频背景构造时不接收
> 透明度或自动播放参数，因此设置它们没有效果。视频是否播放由用户设置 `EnableBackgroundVideo` 控制（见下）。

音频优先级由两个**用户设置**驱动（通过 `UserINISettings` 持久化在 `Settings.ini` 中，**不在** `MainMenu.ini` 中）：

- `PlayMainMenuMusic` — `[Audio]` 段，默认 `true`。
- `EnableBackgroundVideoSound` — `[Video]` 段，默认 `false`。为 `false` 时视频按静音处理。
- `EnableBackgroundVideo` — `[Video]` 段，默认 `false`。背景视频本身的总开关。

说明：

- 暂停与静音快捷键是普通按键（无需修饰键，与主菜单按钮快捷键一致），并且只在主菜单是聚焦输入窗口且没有游戏运行时生效。避免使用主菜单按钮已占用的值（`C`、`L`、`S`、`M`、`N`、`O`、`E`、`T`、`R`、`X`）或 `TopBar` 占用的值（`F1`-`F4`、`F12`）。
- 游戏运行时，背景视频会自动暂停（其音频淡出）以节省资源；返回主菜单时恢复。用快捷键手动暂停的状态会被记住，不会被自动恢复覆盖。
- 用快捷键静音视频音频会让菜单音乐接管（视频让出优先级）；取消静音会再次淡出菜单音乐。该选择会一直记住，直到 `EnableBackgroundVideoSound` 或 `PlayMainMenuMusic` 设置被更改。

### [CampaignSelector](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)



#### 任务属性



`Battle.ini` 的任务段以及自定义任务地图文件的 `[ClientMissionConfig]` 段支持以下键：

```ini
[MISSION_SECTION]                    ; Mission
CD=0                                 ; integer, CD 编号。
Side=0                               ; integer, 任务的阵营索引。
Scenario=                            ; string,  地图文件的相对路径。
Description=Undefined mission        ; string,  任务显示名称。支持本地化。
SideName=                            ; string,  任务图标资源前缀，不是完整路径。客户端加载时会追加
                                     ;          `icon.png`（例如 `SideName=GDI` 加载 `GDIicon.png`）。
LongDescription=                     ; string,  任务描述文本。支持本地化，用 `@` 换行。
FinalMovie=none                      ; string,  任务完成后播放的影片。
RequiredAddon=false                  ; boolean, 任务是否要求资料片。
Enabled=true                         ; boolean, 任务是否可选。
BuildOffAlly=false                   ; boolean, 玩家能否在盟军建筑上建造。
PlayerAlwaysOnNormalDifficulty=false ; boolean, 无论难度滑块如何，强制人类玩家为普通难度。
Tags=                                ; 逗号分隔的字符串，用于战役标签选择器过滤的标签。自定义任务
                                     ;          始终带有 `CUSTOM` 标签。
PreviewImage=                        ; string,  任务预览图片相对于 `Resources/Mission Previews/` 的路径。
ScenarioMapINI=                      ; boolean, 为此任务覆盖 `CopyMissionsToSpawnmapINI`。
Supplement=                          ; boolean, 为此任务覆盖 `CustomMissionSupplementEnable.Battle`。
MissionSpawnIniOptions=              ; string,  `Battle.ini` 中一个段的名称，播放任务时该段的键会被
                                     ;          写入 spawnmap.ini。
```

`Battle.ini` 的 `[Battles]` 段把列表条目映射到任务段：

```ini
[Battles]
0=MISSION_SECTION
1=ANOTHER_MISSION
```

#### 自定义任务地图文件

放置在 `CustomMissionPath` 目录（参见 ClientDefinitions.ini）中的自定义任务 `.map` 文件会扫描两个 INI 段：

- **`[ClientMissionConfig]`** — **必需**，地图才会被识别为自定义任务。支持任务属性中的客户端侧键，但对自定义任务而言 `Scenario` 由 `.map` 文件名/路径派生，`Tags` 始终为 `CUSTOM`。
- **`[GameMissionConfig]`** — **可选**。键值对在启动时写入 `spawn.ini`。用于加载画面配置及其他引擎级设置。空的 `LS640BkgdName`/`LS800BkgdName`/`LS800BkgdPal` 值会被跳过，存在加载画面键时设置 `Settings.ReadMissionSection=Yes`。

```ini
; 在自定义任务 .map 文件中

[ClientMissionConfig]
Description=My Custom Mission
Side=0
Enabled=true

[GameMissionConfig]
; 可选。启动时写入 spawn.ini。
; 如果存在加载画面键，spawn.ini 中会设置 ReadMissionSection=Yes。
```

如果 `[GameMissionConfig]` 不存在或没有指定加载画面键，客户端会自动查找 `.shp` 和 `.pal` 补充文件作为加载画面资源的回退。

注意：补充任务文件必须在 `ClientDefinitions.ini` 中使用 `CustomMissionPath` 配合 `CustomMissionSupplementFileNExtension` 与 `CustomMissionSupplementFileNCopyAs` 配置为顺序的 `(extension, copy-as filename)` 对，其中 `N` 是顺序编号。

#### [pnlMissionPreview](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)



_（继承自 [XNAPanel](#xnapanel)）_

你可以为战役选择器中的每个任务设置预览图片，即任务预览面板。

要启用此功能，请在 `Resources` 文件夹内创建 `Mission Previews` 文件夹，放入你想要的图片并将其重命名为 `Default.png`。

要调整面板大小和位置，请修改 `CampaignSelector.ini` 中的 `pnlMissionPreview`。它继承 `XNAPanel` 的全部属性。

```ini
[pnlMissionPreview]          ; XNAPanel
...
```

要配置每个任务在 `Resources/Mission Previews` 文件夹中使用哪张预览图，请在 `Battle.ini` 的任务段（或自定义任务的 `[ClientMissionConfig]`）中添加 `PreviewImage` 属性，并将其值设为图片文件相对于 `Resources/Mission Previews` 文件夹的路径：

```ini
[YourMissionSection]
PreviewImage= ; string, 图片文件相对于 `Resources/Mission Previews` 的路径。
```

如果任务未设置 `PreviewImage`，默认使用 `Resources/Mission Previews/Default.png`。

#### [tbMissionDescription](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Campaign/CampaignSelector.cs)



_（继承自 [XNATextBlock](#xnatextblock)）_

此控件在战役选择器中显示任务描述。注意，当任务预览面板激活时，任务描述文本块的*默认*大小会自动改变。

要调整文本块的大小和位置，请修改 `CampaignSelector.ini` 中的 `tbMissionDescription`。它继承 `XNATextBlock` 的全部属性。

```ini
[tbMissionDescription]       ; XNATextBlock
...
```

#### 战役标签选择器

当 `ClientDefinitions.ini` 中 `CampaignTagSelectorEnabled=true` 时，标签选择器窗口会出现在战役选择器之前。它允许玩家按任务的 `Tags` 属性过滤任务。

使用命名模式 `ButtonTag_{TagName}` 在 `CampaignTagSelector.ini` 中定义标签按钮：

```ini
[CampaignTagSelector]
$CC00=ButtonTag_Story:XNAClientButton
$CC01=ButtonTag_Challenge:XNAClientButton
$CC02=btnShowAllMission:XNAClientButton
$CC03=btnCancel:XNAClientButton

[ButtonTag_Story]
; 按钮属性...

[ButtonTag_Challenge]
; 按钮属性...

[btnShowAllMission]
; "显示所有任务" 按钮 - 无论标签如何都显示所有任务。
```

`ButtonTag_{TagName}` 中的标签名会与任务的 `Tags` 值匹配。自定义任务自动获得 `CUSTOM` 标签。

#### 战役游戏选项与强制 Spawn 选项

可以将 `CampaignCheckBox` 与 `CampaignDropDown` 控件添加到 `CampaignSelector.ini`，为战役任务提供玩家可选选项。参见 [GameSessionCheckBox](#gamesessioncheckbox) 与 [GameSessionDropDown](#gamesessiondropdown)。

`GameOptions.ini` 中的 `[CampaignForcedSpawnIniOptions]` 段定义了对战役任务始终写入 `spawn.ini` 的键，与 UI 选项无关。这与多人游戏的 `[ForcedSpawnIniOptions]` 段是分开的。参见 GameOptions.ini。

---

## 全局配置文件



### ClientDefinitions.ini



`ClientDefinitions.ini` 定义客户端的全局设置：游戏类型、推荐分辨率、启动游戏的可执行文件等。由 `ClientCore/ClientConfiguration.cs` 读取。

#### `[Settings]`

```ini
[Settings]
ClientGameType=                      ; string,  用于游戏专属行为的客户端类型（例如 RA/YR/TS/DTA）。
LocalGame=                           ; string,  游戏标识（默认 "DTA"）。
WindowTitle=                         ; string,  游戏窗口标题。支持本地化。
GameExecutableNames=Game.exe         ; 逗号分隔的字符串，要查找的游戏可执行文件。
GameLauncherExecutableName=          ; string,  启动游戏客户端的可执行文件。
UnixGameExecutableName=wine-dta.sh   ; string,  Linux/macOS 上使用的游戏可执行文件。
LauncherExe=                         ; string,  选择正确主客户端可执行文件的程序。
ModMode=false                        ; boolean, 启用模组专属行为。
RegistryInstallPath=TiberianSun      ; string,  存储游戏安装路径的注册表键。
LongGameName=Tiberian Sun            ; string,  游戏的完整显示名称。
MaxNameLength=16                     ; integer, 玩家名称的最大长度。
RequiredFiles=                       ; 逗号分隔的字符串，进行游戏必须存在的文件。
ForbiddenFiles=                      ; 逗号分隔的字符串，阻止游戏运行的文件。
MapFileExtension=map                 ; string,  主地图文件的扩展名。
SupplementalMapFileExtensions=       ; 逗号分隔的字符串，创建 spawnmap.ini 时要复制的补充地图文件
                                     ;          （例如 bin,mix）。
DiscordAppId=                        ; string,  用于 Rich Presence 的 Discord 应用 ID。为空时禁用。
DisableDiscordIntegration=false      ; boolean, （NetworkDefinitions.ini）禁用 Discord 集成。
SendSleep=2500                       ; integer, 网络发送休眠毫秒数。
LoadingScreenCount=2                 ; integer, 轮换的加载画面数量。
SidebarHack=false                    ; boolean, 启用侧边栏兼容性补丁。
UseIsometricCells=true               ; boolean, 地图预览是否使用等距格子。
WaypointCoefficient=128              ; integer, 路径点坐标系数。
MapCellSizeX=48                      ; integer, 地图预览的格子宽度。
MapCellSizeY=24                      ; integer, 地图预览的格子高度。
UseBuiltStatistic=false              ; boolean, 将建造的建筑写入统计日志。
StatisticsLogFileName=DTA.LOG        ; string,  统计日志文件名。
WindowedModeKey=Video.Windowed      ; string,  切换游戏窗口模式的 INI 键。
MinimumRenderWidth=1280              ; integer, 客户端最小渲染宽度。
MinimumRenderHeight=768              ; integer, 客户端最小渲染高度。
MaximumRenderWidth=1280              ; integer, 客户端最大渲染宽度。
MaximumRenderHeight=800              ; integer, 客户端最大渲染高度。
AllowClientMinimumRenderIndefinitely=false ; boolean, 系统无法处理最大尺寸时保持客户端在最小渲染尺寸。
RecommendedResolutions=              ; 逗号分隔的字符串，显示选项中展示的分辨率。默认为
                                     ;          `{MinimumRenderWidth}x{MinimumRenderHeight},{MaximumRenderWidth}x{MaximumRenderHeight}`。
MinimumIngameWidth=640               ; integer, 游戏内最小分辨率宽度。
MinimumIngameHeight=480              ; integer, 游戏内最小分辨率高度。
MaximumIngameWidth=                  ; integer, 游戏内最大分辨率宽度（默认 int.MaxValue）。
MaximumIngameHeight=                 ; integer, 游戏内最大分辨率高度（默认 int.MaxValue）。
CustomIngameResolutions=             ; 逗号分隔的字符串，额外的游戏内分辨率。
CopyResolutionDependentLanguageDLL=true ; boolean, 复制与分辨率相关的语言 DLL。
SettingsFile=Settings.ini            ; string,  设置控件写入的设置 INI 文件名。
MPMapsPath=INI/MPMaps.ini            ; string,  多人游戏地图 INI 的路径。
KeyboardINI=Keyboard.ini             ; string,  热键配置写入的游戏键盘 INI 文件。
KeyboardHotkeySection=               ; string,  热键使用的键盘 INI 段。RA 类型客户端默认为 "WinHotKeys"，
                                     ;          其他为 "Hotkey"。
ExtraCommandLineParams=              ; string,  传给游戏可执行文件的额外命令行参数。
BattleFSFileName=BattleFS.ini        ; string,  战斗文件系统 INI 的名称。
MapEditorExePath=FinalSun/FinalSun.exe ; string, 地图编辑器可执行文件路径（Windows）。
UnixMapEditorExePath=                ; string,  地图编辑器可执行文件路径（Unix）。默认为 MapEditorExePath。
FSIniPath=FinalSun/FinalSun.ini      ; string,  地图编辑器设置 INI 的路径。
TrustedDomains=                      ; 逗号分隔的字符串，无需确认对话框即可在默认浏览器中打开的域名。
                                     ;          示例：cncnet.org,github.com,moddb.com。
LongSupportURL=                      ; string,  支持页面 URL。
ShortSupportURL=                     ; string,  短支持页面 URL。
ChangelogURL=                        ; string,  更新日志 URL。
CreditsURL=                          ; string,  制作人员 URL。
ManualDownloadURL=                   ; string,  手册下载 URL。
ShowDevelopmentBuildWarnings=true    ; boolean, 显示开发版构建警告。
ShowGameIconInGameList=true          ; boolean, 在游戏列表中显示游戏图标。
SaveSkirmishGameOptions=false        ; boolean, 跨会话保存上次使用的遭遇战游戏选项。
SaveCampaignGameOptions=false        ; boolean, 跨会话保存上次使用的战役游戏选项。
CreateSavedGamesDirectory=false      ; boolean, 创建存档目录。
DisableMultiplayerGameLoading=false  ; boolean, 禁用多人游戏加载画面。
DisplayPlayerCountInTopBar=false     ; boolean, 在顶栏显示玩家数量。
ReturnToMainMenuOnMissionLaunch=true ; boolean, 启动任务时返回主菜单。
CampaignTagSelectorEnabled=false     ; boolean, 启用战役标签选择器窗口。
CampaignGameSpeedControlEnable=false ; boolean, 在战役选择器中启用游戏速度滑块（1-7）。
UseNetPlayerSameNameRecognition=true ; boolean, 为网络玩家启用同名识别。
CustomMissionPath=Maps/CustomMissions ; string, 存放玩家自制地图的文件夹。
CustomMissionSupplementEnable=true   ; boolean, 复制自定义任务补充文件的总开关。
CustomMissionSupplementEnable.Battle=true ; boolean, Battle.ini 任务的开关。默认为 CustomMissionSupplementEnable。
CustomMissionSupplementEnable.Custom=true  ; boolean, 自定义任务的开关。默认为 CustomMissionSupplementEnable。
CustomMissionSupplementFile0Extension=csf ; string, 补充文件 0 的扩展名。
CustomMissionSupplementFile0CopyAs=stringtable99.csf ; string, 补充文件 0 的目标文件名（存在
                                     ;          Extension 键时必填）。
CustomMissionSupplementFile1Extension=pal ; ...
CustomMissionSupplementFile1CopyAs=custommission.pal ; ...
CustomMissionSupplementFile2Extension=shp ; ...
CustomMissionSupplementFile2CopyAs=custommission.shp ; ...
; 补充文件在游玩自定义任务时复制到游戏文件夹。
; 编号缺失时迭代停止；每个 Extension 值必须唯一，
; 存在 Extension 时 CopyAs 必填（缺少 CopyAs 或
; 扩展名重复会抛出 ClientConfigurationException）。
CopyMissionsToSpawnmapINI=true       ; boolean, 将任务的 `[GameMissionConfig]` 写入 spawnmap.ini。
CopyMissionsToSpawnmapINI.Battle=true  ; boolean, Battle.ini 任务的逐任务覆盖。
CopyMissionsToSpawnmapINI.Custom=true  ; boolean, 自定义任务的逐任务覆盖。
AllowedCustomGameModes=Standard,Custom Map ; 逗号分隔的字符串，允许出现自定义（非官方）地图的游戏模式。
                                     ;          官方地图不受影响。
InactiveHostWarningMessageSeconds=0  ; integer, 警告挂机房主之前的秒数。
InactiveHostKickSeconds=0            ; integer, 踢出挂机房主之前的额外秒数（总计 = 踢出 + 警告）。
SkillLevelOptions=Any,Beginner,Intermediate,Pro ; 逗号分隔的字符串，难度等级选项。
DefaultSkillLevelIndex=0             ; integer, 默认难度等级索引（夹紧到选项列表范围）。
CompatibilityCheckExecutables=       ; 逗号分隔的字符串，检查 DirectDraw 兼容模式问题的可执行文件。
                                     ;          示例：CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe。
DisallowJoiningIncompatibleGames=false ; boolean, 阻止加入不同游戏版本的房间。
UseClientRandomStartLocations=false  ; boolean, 让客户端随机化起始位置。
AllowedAllAspectsWindow.INItializable=true ; boolean, 在窗口上启用表达式属性（$X、$Y、...）。
UselbMissionDescription=false        ; boolean, 让战役选择器中的任务描述可滚动。
DefaultFrameSendRate=7               ; integer, 写入 spawn.ini 的默认 FrameSendRate。
DefaultProtocolVersion=2             ; integer, 写入 spawn.ini 的默认 Protocol。
DefaultMaxAhead=0                    ; integer, 写入 spawn.ini 的默认 MaxAhead。
CnCNetLiveStatusIdentifier=cncnet5_ts ; string, 用于 CnCNet 在线状态的身份标识。
```

#### `[Translations]`

```ini
[Translations]
TranslationIniName=Translation.ini    ; string, 翻译 INI 文件名（支持逗号分隔的列表）。
TranslationsFolder=Resources/Translations ; string, 存放翻译文件的文件夹。
GameFileX=path/to/source.file,path/to/destination.file[,checked] ; 翻译游戏文件。`X` 是任意文本。
                                     ;          `checked` 必须字面为 `CHECKED`。语法无效会抛出
                                     ;          IniParseException。
```

#### `[Themes]`

```ini
[Themes]
0=ThemeName,Themes/ThemePath ; 索引 -> 主题名称与相对路径。
```

#### `[UserDefaults]`

```ini
[UserDefaults]
BorderlessWindowedClient=true ; boolean, 无边框窗口模式的默认值。
IntegerScaledClient=false     ; boolean, 整数缩放选项的默认值。
WriteInstallationPathToRegistry=true ; boolean, 将安装路径写入注册表。
```

### DTACnCNetClient.ini



客户端逐用户设置文件（`ClientCore/ClientConfiguration.cs`），位于资源目录。它定义 UI 颜色、提示行为、音频冷却时间以及常量表。

#### `[General]`

```ini
[General]
MainMenuThemePath=               ; string,  主菜单资源的前缀子路径，例如 "Themes/MyriaDimensionTheme"。
MainMenuTheme=mainmenu           ; string,  空格分隔的主菜单主题列表；随机选择一个。
AlphaRate=0.005                  ; float,   控件默认每 100 毫秒的透明度变化率。
CheckBoxAlphaRate=0.05           ; float,   复选框默认每 100 毫秒的透明度变化率。
IndicatorAlphaRate=0.05          ; float,   指示器默认每 100 毫秒的透明度变化率。
UILabelColor=0,0,0               ; color,   标签默认文本颜色。
HintTextColor=128,128,128        ; color,   提示文本颜色。
DisabledButtonColor=108,108,108  ; color,   禁用按钮的文本颜色。
AltUIColor=255,255,255           ; color,   备用 UI 颜色。
ButtonHoverColor=255,192,192     ; color,   按钮悬停颜色。
AltUIBackgroundColor=196,196,196 ; color,   备用 UI 背景颜色。
WindowBorderColor=128,128,128    ; color,   窗口边框颜色。
PanelBorderColor=255,255,255     ; color,   面板边框颜色。
ListBoxHeaderColor=255,255,255   ; color,   列表框表头文本颜色。
ListBoxFocusColor=64,64,168      ; color,   列表框焦点/高亮颜色。
HoverOnGameColor=32,32,84        ; color,   游戏列表中游戏的悬停颜色。
DefaultChatColor=0,255,0         ; color,   默认聊天消息颜色。
AdminNameColor=255,0,0           ; color,   聊天中管理员名称颜色。
PrivateMessageOtherUserColor=196,196,196 ; color, 收到的私信颜色。
PrivateMessageColor=128,128,128  ; color,   发送的私信颜色。
DefaultPersonalChatColorIndex=0  ; integer, 默认个人聊天颜色索引。
MapPreviewNameBackgroundColor=0,0,0,144 ; color, 地图预览名称背景。
MapPreviewNameBorderColor=128,128,128,128 ; color, 地图预览名称边框。
StartingLocationHoverColor=255,255,255,128 ; color, 起始位置悬停 remap 颜色。
StartingLocationsUsePlayerRemapColor=false ; boolean, 起始位置使用玩家的 remap 颜色。
DropDownScrollBarThumbColor=     ; color,   下拉框滚动条滑块颜色。
DropDownScrollBarTrackColor=     ; color,   下拉框滚动条轨道颜色。
DropDownScrollBarBorderColor=    ; color,   下拉框滚动条边框颜色。
ToolTipFontIndex=0               ; integer, 提示使用的字体索引。
ToolTipOffsetX=0                 ; integer, 提示的水平偏移。
ToolTipOffsetY=0                 ; integer, 提示的垂直偏移。
ToolTipMargin=4                  ; integer, 提示的像素边距。
ToolTipDelay=0.67                ; float,   提示出现前的秒数。
ToolTipAlphaRate=4.0             ; float,   提示每秒淡入速度。
```

#### `[Audio]`

```ini
[Audio]
SoundGameLobbyJoinCooldown=0.25     ; float, 游戏大厅加入音效之间的最小秒数。
SoundGameLobbyLeaveCooldown=0.25    ; float, 离开音效之间的最小秒数。
SoundMessageCooldown=0.25           ; float, 大厅聊天消息音效之间的最小秒数。
SoundPrivateMessageCooldown=0.25    ; float, 私信音效之间的最小秒数。
SoundGameLobbyGetReadyCooldown=5.0  ; float, "准备就绪" 音效之间的最小秒数。
SoundGameLobbyReturnCooldown=1.0    ; float, "返回" 音效之间的最小秒数。
```

#### `[ParserConstants]` / `[SameNameConstants]`

参见常量。

### Settings.ini（UserINISettings）

逐用户设置文件（`ClientCore/Settings/UserINISettings.cs`），写入资源目录下的 `SettingsFile`（默认 `Settings.ini`）。
它持久化用户选项，也是 `Setting*` 控件（参见 XNAOptionsPanel 控件）使用的存储。重要键：

```ini
[Audio]
PlayMainMenuMusic=true             ; boolean, 播放主菜单音乐。默认 `true`。

[Video]
EnableBackgroundVideo=false        ; boolean, 主菜单背景视频的总开关。默认 `false`。
EnableBackgroundVideoSound=false   ; boolean, 启用背景视频的音轨。默认 `false`。
WindowedMode=                      ; boolean, 窗口模式（键名来自 `WindowedModeKey`，默认 `Video.Windowed`）。
NoWindowFrame=                     ; boolean, 无边框窗口模式。
BorderlessWindowedClient=true      ; boolean, 无边框窗口客户端选项的默认值。
IntegerScaledClient=false          ; boolean, 整数缩放选项的默认值。

[Options]
GameSpeed=1                        ; integer, 游戏内速度设置。
```

`SettingCheckBox`/`SettingDropDown` 控件按 `SettingSection`（见 `[CustomSettings]`）保存，键名为
`CONTROLNAME_Checked` / `CONTROLNAME_Value` / `CONTROLNAME_SelectedIndex`，取决于写入模式。

### NetworkDefinitions.ini



```ini
[Settings]
CnCNetTunnelListURL=      ; string, CnCNet 隧道列表的 URL。
CnCNetPlayerCountURL=     ; string, CnCNet 玩家数量 API 的 URL。
CnCNetMapDBDownloadURL=   ; string, 地图数据库下载 API 的 URL。
CnCNetMapDBUploadURL=     ; string, 地图数据库上传 API 的 URL。
DisableDiscordIntegration=false ; boolean, 禁用 Discord 集成。

[IRCServers]
0=irc.server.example ; IRC 服务器地址；每个非空值都会被添加。
```

### MPMaps.ini



游戏模式在 `MPMaps.ini` 的 `[GameModes]` 段中定义。每个游戏模式都可以有一个同名配置段。

```ini
[GameModes]
0=Standard
1=No Bases
2=Infantry Only

[Standard]
UIName=Standard
; 游戏模式属性...
```

#### 游戏模式属性



```ini
[GAME_MODE_NAME]                       ; GameMode
UIName=                                ; string,  显示名称。默认为段名。
MinPlayersOverride=                    ; integer, 最小玩家数的覆盖值。
MaxPlayersOverride=                    ; integer, 最大玩家数的覆盖值。
DisallowedPlayerSides=                 ; 逗号分隔的整数，任何玩家都不能选择的阵营索引。
DisallowedHumanPlayerSides=            ; 逗号分隔的整数，人类玩家不能选择的阵营索引。
DisallowedComputerPlayerSides=         ; 逗号分隔的整数，AI 玩家不能选择的阵营索引。
DisallowedPlayerColors=                ; 逗号分隔的整数，任何玩家都不能选择的颜色索引。
DisallowedPlayerSides.StartN=          ; 逗号分隔的整数，按起始位置的阵营限制（与全局列表取并集）。
DisallowedPlayerColors.StartN=         ; 逗号分隔的整数，按起始位置的颜色限制。
ForcedOptions=                         ; string,  一个 INI 段的名称，其键成为强制的复选框/下拉框值。
ForcedSpawnIniOptions=                 ; string,  一个 INI 段的名称，其键值对写入 spawn.ini 的
                                       ;          [Settings]。默认为 `{Name}ForcedSpawnIniOptions`。
MapCodeIniName=                        ; string,  `INI/Map Code/` 中地图代码 INI 文件的名称。默认为
                                       ;          `{Name}.ini`。别名：MapCodeININame。
RandomizedMapCodeIniNames=             ; 逗号分隔的字符串，额外的随机化地图代码 INI 名称。
RandomizedMapCodesCount=1              ; integer, 要挑选的随机化地图代码数量。
```

#### GameModeMapBase 属性



以下属性由地图（`MPMaps.ini` 的地图段）与游戏模式共用。当地图和游戏模式同时定义同一属性时，除非另有说明，地图值优先。

```ini
; 地图段键：
[MAP_NAME]
ClientMinPlayer=                      ; integer,  最小玩家数（客户端侧）。备用键名：`MinPlayers`、`MinPlayer`。
ClientMaxPlayer=                      ; integer,  最大玩家数（客户端侧）。备用键名：`MaxPlayers`、`MaxPlayer`。
EnforceMinPlayers=                    ; boolean,  是否强制最小玩家数。
EnforceMaxPlayers=                    ; boolean,  是否强制最大玩家数。
AllowedStartingLocations=             ; 逗号分隔的整数，允许的起始位置。从 0 开始。未设置时
                                      ;          所有起始位置都允许（最多 8 个）。
IsCoopMission=                        ; boolean,  将地图标记为联合作战任务。
ClientMultiplayerOnly=                ; boolean,  地图是否不能在遭遇战中使用（自定义地图）。
MultiplayerOnly=                      ; boolean,  该模式的地图是否不能在遭遇战中使用（游戏模式）。
HumanPlayersOnly=                     ; boolean,  是否禁止 AI 玩家。
ForceRandomStartLocations=            ; boolean,  强制随机起始位置。
ForceNoTeams=                         ; boolean,  强制不分队。
CoopDifficultyLevel=                  ; integer,  联合作战难度覆盖。

; 游戏模式段键：
[GAME_MODE]
MinPlayersOverride=                   ; integer,  最小玩家数。优先级高于 Map.ClientMinPlayer。
MaxPlayersOverride=                   ; integer,  最大玩家数。优先级高于 Map.ClientMaxPlayer。
EnforceMinPlayers=                    ; boolean,  是否强制最小玩家数。
EnforceMaxPlayers=                    ; boolean,  是否强制最大玩家数。
AllowedStartingLocations=             ; 逗号分隔的整数，允许的起始位置。
IsCoopMission=                        ; boolean,  将模式标记为联合作战。
MultiplayerOnly=                      ; boolean,  该模式的地图是否不能在遭遇战中使用。
HumanPlayersOnly=                     ; boolean,  是否禁止 AI 玩家。
ForceRandomStartLocations=            ; boolean,  强制随机起始位置。
ForceNoTeams=                         ; boolean,  强制不分队。
CoopDifficultyLevel=                  ; integer,  联合作战难度覆盖。
```

玩家数量的优先级解析：
- `MaxPlayers`：`GameMode.MaxPlayersOverride` > `Map.ClientMaxPlayer` > `Map.MaxPlayer`
- `MinPlayers`：`GameMode.MinPlayersOverride` > `Map.ClientMinPlayer` > `Map.MinPlayer`

#### 地图属性



地图段支持的额外键：

```ini
[MAP_NAME]
BaseSection=                           ; string,  要继承键的另一地图段名称（先合并）。
Description=Unnamed map                ; string,  地图描述。
Author=Unknown author                 ; string,  地图作者。
GameModes=Default                      ; string,  该地图出现的游戏模式。
PreviewImage=                          ; string,  地图预览图片路径。
Briefing=                              ; string,  联合作战简报文本。
SpawnIniBriefing=                      ; string,  写入 spawn.ini 的简报。
CooperativeLoadScreenSettings=false    ; boolean, 启用联合作战加载画面设置。
CooperativeLoadScreen=                 ; string,  联合作战加载画面文件。
CooperativeLoadScreenPallet=           ; string,  联合作战加载画面调色板文件。
Credits=-1                             ; integer, 初始资金（-1 = 默认）。
UnitCount=-1                           ; integer, 初始单位数量（-1 = 默认）。
NeutralColor=-1                        ; integer, 中立颜色索引（-1 = 默认）。
SpecialColor=-1                        ; integer, 特殊颜色索引（-1 = 默认）。
Bases=                                 ; boolean, 玩家是否以基地开局。
ExtraTextureN=name,x,y[,level[,toggleable]] ; 地图预览的额外纹理放置。
LocalSize=WIDTH,HEIGHT                 ; 2 integers, 预览用地图尺寸（别名：Size，或 X/Y/Width/Height）。
WaypointN=X,Y                         ; 2 integers, 路径点坐标（从 1 开始）。
TeamStartMappingN=INDEX               ; integer, 队伍起始映射。
TeamStartMappingNName=                 ; string,  队伍起始映射名称。
ForcedOptions=                         ; 逗号分隔的字符串，其键成为强制的复选框/下拉框值的 INI 段名。
ForcedSpawnIniOptions=                 ; 逗号分隔的字符串，其键值对写入 spawn.ini 的 INI 段名。
MissionSpawnIniOptions=SourceSection:TargetSection ; 地图专属的 spawn INI 段映射。
ExtraIniName=MyExtraCode.ini           ; string,  游戏启动时合并进地图 INI 的文件名（位于 `INI/Map Code/`）。
                                       ;          别名：ExtraININame。
```

### GameOptions.ini



`GameOptions.ini` 定义阵营、随机选择器、多人游戏颜色与强制 spawn 选项。

```ini
[General]
Sides=GDI,Nod,Allies,Soviet            ; 逗号分隔的字符串，可玩的阵营。代码默认为
                                       ;          "GDI,Nod,Allies,Soviet"；随附文件使用 "GDI,Nod"。
InternalSideIndices=                   ; 逗号分隔的整数，内部阵营索引。
SpectatorInternalSideIndex=            ; integer, 观众使用的内部阵营索引。
StartingLocationAngularVelocity=0.015  ; float,   起始位置旋转速度。代码默认 `0.015`
                                       ;          （随附文件使用 0.0075）。
ReservedStartingLocationAngularVelocity=-0.0075 ; float, 预留起始位置旋转速度。代码默认
                                       ;          `-0.0075`（随附文件使用 0.05）。
RandomColor=255,255,255                ; color,   随机颜色条目。代码默认 "255,255,255"
                                       ;          （随附文件使用 168,168,168）。

[MPColors]
Gold=255,223,94,0                      ; name=R,G,B,gameColorIndex - 多人游戏颜色。颜色名称是键名
                                       ;          （不是索引）；gameColorIndex 是游戏内颜色 ID。

[RandomSelectors]
Name=0,1,2                             ; 选择器名称 -> 逗号分隔的阵营索引。超出有效阵营范围的索引会被
                                       ;          忽略；选择器需要多于一个有效阵营索引才会注册。

[ForcedSpawnIniOptions]
FogOfWar=no                            ; 多人游戏始终写入 spawn.ini [Settings] 的键。

[CampaignForcedSpawnIniOptions]
AutoSaveInterval=0                     ; 战役任务始终写入 spawn.ini [Settings] 的键。
```

#### ForcedSpawnIniOptions

强制 spawn 选项定义无论 UI 设置如何都始终写入 `spawn.ini` 的键。它们可以在多个层级定义：

1. **全局** — `GameOptions.ini` 中的 `[ForcedSpawnIniOptions]`：应用于所有多人游戏。
2. **战役** — `GameOptions.ini` 中的 `[CampaignForcedSpawnIniOptions]`：仅应用于战役任务。
3. **按游戏模式** — `MPMaps.ini` 中的每个游戏模式段都可以指定 `ForcedSpawnIniOptions=SectionName`，指向一个键会写入 `spawn.ini` 的段。
4. **按地图** — `MPMaps.ini` 中的地图可以指定 `ForcedSpawnIniOptions=SectionName`（多个段用逗号分隔）。

多人游戏的 spawn.ini 写入顺序：
1. 游戏大厅复选框/下拉框
2. 来自 `GameOptions.ini` 的全局 `[ForcedSpawnIniOptions]`
3. 游戏模式专属的强制选项
4. 地图专属的强制选项

在 `MPMaps.ini` 中：

```ini
[MyGameMode]
ForcedSpawnIniOptions=MyModeForcedOptions

[MyModeForcedOptions]
; 该游戏模式激活时，这里的键会写入 spawn.ini。
SomeOption=value

[MyMap]
ForcedSpawnIniOptions=MyMapForcedOptions

[MyMapForcedOptions]
; 游玩这张特定地图时，这里的键会写入 spawn.ini。
AnotherOption=value
```

### KeyboardCommands.ini



`KeyboardCommands.ini` 定义客户端写入游戏键盘 INI 文件的游戏内热键命令，该文件名由 `ClientDefinitions.ini` 的 `[Settings]` 中的 `KeyboardINI` 定义。每个段代表一个游戏命令及其默认键位绑定。

该文件位于 `Resources` 目录，由热键配置窗口读取。

```ini
[CommandName]
UIName=Display name       ; string,  热键配置 UI 中命令的显示名称。
Category=CategoryName     ; string,  热键配置 UI 下拉框中使用的分组类别。
Description=Description   ; string,  热键配置 UI 中显示的描述文本。
DefaultKey=0              ; integer, 默认的 TS 编码键值（低字节 = 键码，高字节 = 修饰键标志）。
                          ;          没有默认热键的命令使用 0。
DisableModifierKeys=false ; boolean, 阻止修饰键（Ctrl、Shift、Alt）与此命令组合。
                          ;          为 true 时只能分配单个按键。默认为 false。
```

命令属性：

- **`UIName`** — 命令的显示名称。支持通过 `INI:Hotkeys:{CommandName}:UIName` 本地化。
- **`Category`** — 用于在热键配置下拉框中分组命令的类别。支持通过 `INI:HotkeyCategories:{Category}` 本地化。
- **`Description`** — 选中命令时显示的描述文本。支持通过 `INI:Hotkeys:{CommandName}:Description` 本地化。
- **`DefaultKey`** — TS 编码整数格式 `(modifier << 8) + key` 的默认键位。修饰键标志：0 = 无，1 = Shift，2 = Ctrl，4 = Alt。命令没有默认热键时设为 `0`。
- **`DisableModifierKeys`** — 为 `true` 时，热键配置窗口不允许为此命令组合修饰键。只能分配一个不带 Ctrl、Shift 或 Alt 的按键。这对某些不支持修饰键组合的命令很有用。

示例：

```ini
[PlanningMode]
UIName=Waypoint Mode
Category=Interface
Description=Enable waypoint mode.
DefaultKey=90
DisableModifierKeys=true
```

配置好的热键会写回游戏键盘 INI（`KeyboardINI`，写入 `KeyboardHotkeySection` 定义的段）。如果 `SettingsFile` 等于 `KeyboardINI`，则改用设置 INI，以便未保存的更改可以被取消。

---

## 完整示例



下面是一个把上文机制组合起来的完整可用示例。它声明了一个小型的 INItializable 窗口，包含一个面板、一个标签、一个切换另一面板的复选框、一个在两个选项面板间切换的下拉框，以及一个打开 URL 的按钮。

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
; 复选框状态变化时切换 pnlAdvanced 的可见性
$Toggles=pnlAdvanced

[ddMode]
Option_ModeA=Mode A
Option_ModeB=Mode B
$X=10
$Y=70
$Width=180
; 选中选项 0 时显示 pnlModeA，选中选项 1 时显示 pnlModeB
$Toggle0=pnlModeA
$Toggle1=pnlModeB

[pnlAdvanced]
BackgroundTexture=panel_adv.png
$X=200
$Y=40
$Width=180
$Height=60
; 在 chkAdvanced 勾选前保持隐藏

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
; 在 ddMode 选中选项 1 前保持隐藏（由 $Toggle1 控制）

[btnMore]
Text=More information
URL=https://cncnet.org
$X=10
$Y=250
$Width=120
```
