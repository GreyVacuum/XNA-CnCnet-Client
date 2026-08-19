# 翻译（Translation）

客户端内置翻译支持。翻译系统的目标是让非程序员也能轻松地把基于 XNA CnCNet 客户端的模组和游戏翻译成他们选择的语言。

翻译系统支持以下功能：
- 翻译客户端内置文本字符串；
- 在不修改 INI 文件本身的前提下翻译 INI 定义的文本值；
- 按翻译调整客户端控件的 INI 尺寸与位置值；
- 在翻译中提供自定义客户端资源覆盖（包括通用与主题专属资源；例如带文字的翻译按钮，或不同 CJK 变体的字体）；
- 根据系统语言设置自动检测客户端的初始语言（在尚未保存语言设置时发生）；
- 可配置的一组要复制到游戏目录的文件（用于游戏内翻译）；
- 生成翻译模板/占位文件，方便翻译。

## 翻译结构

翻译系统默认读取 `Resources/Translations` 目录中的文件夹。该目录中的每个文件夹都被视为一个翻译，可以包含主翻译 INI（含翻译元数据与翻译值）、通用资源（优先级高于 `Resources` 文件夹下相同相对路径的内容）、主题专属翻译 INI 和主题专属资源（对 `Resources/[主题名]` 的覆盖），放在与它们要覆盖的主主题文件夹同名的文件夹中。

例如：

```md
- Resources
  - Some Theme Folder
    * someThemeAsset.png
    * ...
  - Translations
    - ru
      - Some Theme Folder
        * Translation.ini
        * someThemeAsset.png
        * ...
      * Translation.ini
      * someAsset.png
      * ...
    - uk
      * ...
    - zh-Hans
      * ...
    - zh-Hant
      * ...
  * someAsset.png
  * ...
```

### 文件夹命名与自动语言检测

翻译文件夹名称用于与系统区域代码（按 BCP-47 定义）匹配，因此建议按该标准命名翻译文件夹（例如参考 [Windows 使用的区域设置](https://learn.microsoft.com/ru-ru/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c) 的编码方式）。这样客户端可以根据系统区域选择适当的翻译，并自动获取翻译名称。

> [!NOTE]
> 除非你要为特定国家/地区做翻译（例如 `en-US` 与 `en-GB`），建议直接使用[语言代码](http://www.loc.gov/standards/iso639-2/php/code_list.php)（例如 `ru`、`de`、`en`、`zh-Hans`、`zh-Hant` 等）。

文件夹名称不一定要与现有区域代码匹配。但那样的话需要在翻译 INI 中提供显式名称，而且该翻译无论如何都不会被自动选中。

> [!NOTE]
> 硬编码的客户端字符串可以用内置语言翻译覆盖。本客户端分发的内置语言代码是 **`zh`**（参见 `ProgramConstants.HARDCODED_LOCALE_CODE`），因此创建 `Resources/Translations/zh/Translation.ini` 文件即可覆盖硬编码字符串。因为内置语言字符串始终可用，即使客户端没有其他翻译，默认也会选中该语言。

### 翻译 INI 格式

```ini
[General]          ; 翻译元数据
Name=Some Language ; string, 设置后用于替代系统提供的名称
Author=Someone     ; string
MapEncoding=UTF-8  ; string, 定义用于把地图文件加载到 spawnmap.ini 的地图编码名称。默认为 UTF-8（无 BOM）。'Auto' 选项让客户端尝试猜测编码。请省略此行或指定 'UTF-8'。只有在你确实知道自己在做什么时，才指定 'Auto' 或非 'UTF-8' 的编码。

[Values]             ; 翻译的键值对
Some:Key=Some Value  ; string, 见下方说明
```

#### 翻译值键格式

示例：
```ini
INI:HotkeyCategories:Interface=Интерфейс  ; Interface
INI:Hotkeys:AllToCheer:Description=Приказать вашей пехоте ликовать.  ; Make all of your infantry units cheer.
INI:Hotkeys:AllToCheer:UIName=Ликовать  ; Cheer
INI:Controls:CheaterScreen:lblCheater:Text=Обнаружены изменения!  ; Modifications Detected!
Client:DTAConfig:ForceUpdate=Принудительное обновление  ; Force Update
INI:Controls:UpdaterOptionsPanel:btnForceUpdate:Location=320,213
INI:Controls:UpdaterOptionsPanel:btnForceUpdate:Size=220,23
```

`[Values]` 段中的每个键都由几个用 `:` 连接的元素组成，语义各不相同。结构可以这样描述（列表层级表示位置）。
- `Client` - 客户端内置文本字符串。
  - 第 2 和第 3 部分通常分别表示字符串的"命名空间"/类别与字符串名，由开发人员任意选择。
- `INI` - INI 定义的值。
  - `Controls` - 表示所有 INI 定义的控件值。
    - `[父控件名]` - 要翻译值的控件的父控件名称。指定 `Global` 而非父控件名，可以为该控件的所有实例指定相同的翻译值（父控件专属定义仍然优先）
      - `[控件名]` - 要翻译值的控件名称。
        - `[属性名]` - 被翻译的属性名称。当前支持：
          - 每个控件：`Text`、`Size`、`Width`、`Height`、`Location`、`X`、`Y`、`DistanceFromRightBorder`、`DistanceFromBottomBorder`；
          - 带提示的控件：`ToolTip`；
          - 建议文本框：`Suggestion`；
          - 链接按钮：`URL`、`UnixURL`；
          - 设置/游戏选项下拉框：`ItemX`（X 为索引）；
          - 游戏选项下拉框：`OptionName`；
          - INItializable 窗口系统：`$X`、`$Y`、`$Width`、`$Height`；
          - 使用动态（表达式）属性的标签：`$TextAnchor`、`$AnchorPoint`；
          - 动态左键动作：`$LeftClickAction`。
  - `Sides` - 游戏/模组阵营名称的子类别。
  - `Colors` - 游戏/模组颜色名称的子类别。
  - `Themes` - 游戏/模组主题名称的子类别。
  - `GameModes` - 游戏/模组游戏模式的子类别。
    - `[name]` - 唯一标识游戏模式。
      - `[属性名]` - 被翻译的属性名称。仅支持 `UIName`。
  - `Maps` - 游戏/模组地图的子类别（不支持自定义地图）。
    - `[地图路径]` - 唯一标识地图。
      - `[属性名]` - 被翻译的属性名称。仅支持 `Description`（地图名）与 `Briefing`。
  - `Missions` - 游戏/模组单人任务的子类别。
    - `[任务段名]` - 唯一标识任务（取自 `Battle*.ini`）。
      - `[属性名]` - 被翻译的属性名称。仅支持 `Description`（任务名）与 `LongDescription`（实际描述）。
  - `CustomComponents` - 游戏/模组自定义组件的子类别。
    - `[自定义组件 INI 名]` - 唯一标识自定义组件。
      - `[属性名]` - 被翻译的属性名称。仅支持 `UIName`。
  - `UpdateMirrors` - 游戏/模组更新下载镜像的子类别。
    - `[镜像名]` - 唯一标识镜像。
      - `[属性名]` - 被翻译的属性名称。仅支持 `Name` 与 `Location`。
  - `Hotkeys` - 游戏/模组热键的子类别。
    - `[INI 名]` - 唯一标识热键。
      - `[属性名]` - 被翻译的属性名称。仅支持 `UIName` 与 `Description`。
  - `HotkeyCategories` - 游戏/模组热键类别的子类别。
  - `ClientDefinitions` - 不言自明。
    - `WindowTitle` - 不言自明，仅在 `ClientDefinitions.ini` 中设置时有效。

> [!WARNING]
> 你只能翻译 INI 中实际使用过的值！也就是说，为控件属性定义翻译值（例如在定义了 `Location` 时翻译 `X` 和 `Y`），但该属性不在 INI 中——**不会产生任何效果**。

> [!IMPORTANT]
> 如果按钮有 `IdleTexture` 键，请务必把它放在按钮段的第一个键位置，否则你将无法从 `Translation.ini` 调整按钮尺寸，因为 `IdleTexture` 会改变按钮的大小。

### 包含其他文件

主 `Translation.ini` 可以通过 `[INISystem]` 段包含其他 INI 文件，例如复用一个共享的 `ClientTranslation.ini`：

```ini
[INISystem]
BasedOn=ClientTranslation.ini   ; 逗号分隔的文件列表，相对于 Translation.ini 解析；
                                ; 支持 $THEME_DIR$ 变量。Translation.ini 中的值覆盖被包含文件。
```

## 游戏内翻译设置

翻译系统的游戏内翻译支持要求模组/游戏作者指定翻译者可以提供哪些文件来翻译游戏。文件在 `ClientDefinitions.ini` 的 `[Translations]` 段中以 `GameFileX=path/to/source.file,path/to/destination.file[,checked]` INI 键语法指定（X 可以是任意文本，用来帮助排序），值的逗号分隔部分含义如下：
1) 相对于当前选中翻译目录的源文件路径；
2) 要复制到的目标位置，相对于游戏根目录；
3) （可选）`checked` 表示该文件接受完整性检查（如果此文件可能被用于作弊应开启），未指定则该文件不检查。

每个键必须以 `GameFile` 开头，包含 2 到 3 个逗号分隔部分（第 3 部分存在时必须字面为 `CHECKED`，不区分大小写）。语法无效会在客户端启动时抛出 `IniParseException`。

> [!IMPORTANT]
> 处理翻译游戏文件时，默认情况下翻译系统会尝试把目标文件创建为[硬链接](https://learn.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions)。如果创建硬链接失败，系统将改为复制文件。
>
> 建议翻译者始终在源文件夹中的文件上工作，避免编辑目标文件夹中的副本。这很重要，因为当取消选择某语言时，客户端会自动删除目标文件夹中的文件。请注意，即使源文件与对应的目标文件是硬链接，在文本编辑器中编辑任一文件也可能导致两种后果之一：要么两个文件被同时更新，要么硬链接被破坏，只有被编辑的文件收到更新。这就是为什么建议始终在源文件上工作。
>
> 要在 Windows 资源管理器中看到链接，可以安装[此扩展](https://schinagl.priv.at/nt/hardlinkshellext/linkshellextension.html)。

> [!WARNING]
> 如果你在游戏内翻译文件中包含 checked 文件，意味着用户如果包含这些文件就无法进行自定义翻译，而你**在不对这些文件触发"文件被修改/作弊者"警告的情况下**也无法使用自定义组件。此机制是为那些无法以防作弊安全方式提供翻译的游戏和模组设计的，所以请只在别无选择时使用，否则不要指定此参数。

`ClientDefinitions.ini` 中的配置示例：
```ini
[Translations]
GameFileTranslationMix=translation.mix,expandmo98.mix
GameFile_GDI01=Missions/g0.map,Maps/Missions/g0.map
GameFile_NOD01=Missions/n0.map,Maps/Missions/n0.map
GameFile_DLL_SD=Resources/language_800x600.dll, Resources/language_800x600.dll
GameFile_DLL_HD=Resources/language_1024x720.dll,Resources/language_1024x720.dll
```

这会使当前翻译文件夹（比如 `Resources/Translations/ru`）中的 `translation.mix` 文件在游戏启动时被复制到游戏根目录并命名为 `expandmo98.mix`。

> [!WARNING]
> 此功能仅用于*游戏*文件，不用于 INI、主题资源等*客户端*文件！

## 建议的翻译工作流程

0. 在模组的设置 INI 文件（例如：`SUN.INI`、`RA2MD.INI`）的 `[Options]` 段追加 `GenerateTranslationStub=true`。这将使客户端在 `Client` 文件夹中生成一个 `Translation.ini` 文件，包含所有（几乎；见下方注意事项）可翻译的文本值，按键名按字母顺序排列。没有翻译的值会被注释掉；如果已经加载了某个翻译，则现有值和元数据会保留到 stub ini 中。
   - 你也可以在同一位置指定 `GenerateOnlyNewValuesInTranslationStub=true`，只输出缺失的值而不是 stub 中的所有内容，这可能更符合你的工作流程。
   - 非文本值（例如尺寸和位置）不会写入 stub INI，但需要时仍可手动编写。
1. 在 `Resources/Translations` 中创建一个使用所需语言代码命名的文件夹（见上文），把 `Client` 文件夹中的 `Translation.ini` 放进去，然后开始翻译字符串并取消已翻译行的注释。
   - 硬编码字符串在同一客户端二进制之间共享且与模组无关，因此你可以复用你或别人为你要翻译的语言制作的所有带 `Client` 前缀的字符串。或者使用主 `Translation.ini` 中的 `[INISystem]->BasedOn=` 包含一个单独的、包含所有 `Client` 前缀字符串的文件（例如 `ClientTranslation.ini`）。
   - **注意事项：** 硬编码的控件尺寸/位置值完全不会从翻译文件读取；作为变通，请模组作者为该控件指定你将用 INI 定义调整的尺寸/位置值，以便可以通过翻译系统调整。
   - 为加快工作流程，建议使用支持多选编辑的编辑器，例如 [Visual Studio Code](https://code.visualstudio.com)，以便批量选择值。选中第一行未翻译行的 `=`，按 `Ctrl+D` 若干次选中其余未翻译行的 `=`，按 `→`，然后 `Shift+End`。这样会选中你标记行的所有未翻译值，复制它们，去 [DeepL](https://www.deepl.com)（推荐）或任何其他翻译器，粘贴文本、修正翻译、复制回来并粘贴到相同位置。VSCode 会自动把行拆回来，所以不需要逐行输入。
     - DeepL 还会添加它自己的"translated with"行，所以你可能需要把文本粘贴到某个中间文件/窗口/标签页，删除该行，再复制一次。
2. 对每个翻译资源（包括主题专属资源），必须在翻译文件夹中复制原始资源相对于 `Resources` 文件夹的精确路径。资源名称也应与原始资源相同。它们会自动覆盖未翻译的资源。
3. 如果需要主题专属翻译值——在翻译文件夹的主题子文件夹中创建 `Translation.ini`，把需要的键值覆盖放在 `[Values]` 段中（此文件不读取元数据；如果主 `Translation.ini` 不存在，此文件根本不会被读取）。
4. （可选）查阅 `ClientDefinitions.ini`->`[Translations]`->`GameFileX` 中指定的游戏/模组专属游戏内翻译文件，和/或咨询游戏/模组作者获取游戏内翻译的文件列表。把你准备好的游戏内翻译放入指定名称的文件中（值的第一个部分），并放在你的翻译文件夹中。
   - 如果游戏/模组有完整性检查的翻译文件——联系游戏/模组作者把你的翻译包含到游戏/模组包中，这样游戏内翻译不会让你的或用户的安装触发在线"文件被修改"警告。

翻译愉快！

## 其他

- Discord 状态、游戏广播、统计等使用未翻译的名称，以便其他玩家看到更通用的英文名，并且不会在翻译改变时被锁定。
- 翻译后，原始地图名仍会显示在提示中，并可通过上下文菜单复制。
- 在适用处，搜索会同时使用翻译名和未翻译名（地图和大厅搜索）。
