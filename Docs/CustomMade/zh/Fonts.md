# 字体（Fonts）

客户端支持两种字体类型：

- **TrueType** – TTF/OTF 字体。客户端使用 FontStashSharp 渲染这些字体。
- **SpriteFont** – 预编译的 XNA/MonoGame 位图字体（.xnb 文件）。

> **重要：** TrueType 与 SpriteFont 字体**不能**在同一回退链中混用。回退只发生在 TrueType 字体之间。SpriteFont 字体索引不能回退到 TrueType 字体，反之亦然。每个字体索引只能完全属于其中一种类型。

字体配置通过放置在 `Resources` 目录中的 `Fonts.ini` 完成。

## Fonts.ini 的位置

客户端按其资源搜索路径查找 `Fonts.ini`，加载第一个找到的文件（按顺序）：

1. 翻译 + 主题文件夹（例如 `Translations/ko-KR/Allied/Fonts.ini`）
2. 主题文件夹（例如 `Resources/Allied/Fonts.ini`）
3. 共享主题文件夹（`Resources/Themes`）
4. 翻译文件夹（例如 `Translations/ko-KR/Fonts.ini`）
5. 基础资源文件夹（`Resources/Fonts.ini`）
6. 游戏根目录

这样既允许翻译在不改动基础配置的前提下提供自己的字体，也允许 `Resources/Themes` 中的共享主题提供字体而不会被翻译覆盖。

## 配置

```ini
[TextShaping]
; HarfBuzz 文本整形。复杂文字（阿拉伯文、希伯来文）与 ZWJ 表情符号必需。
; 纯拉丁文字（英、西、法）和/或纯 CJK 文字（中、日、韩）可关闭以获得更好性能。
Enabled=true
EnableBiDi=true       ; 双向文本支持（混合 LTR/RTL）
CacheSize=100         ; 整形文本缓存条目数。

[Fonts]
Count=6   ; 定义的字体索引总数，包括回退字体。

[Font0]
Type=TrueType               ; 类型："TrueType" 或 "SpriteFont"
Path=MozillaText-Bold.ttf   ; 相对于 Fonts.ini 所在目录的路径
Size=14                     ; 字体像素高度（仅 TrueType；SpriteFont 忽略）
Fallback=4                  ; 可选。字符缺失时尝试的字体索引。
                            ; 会递归跟随回退字体自身的 Fallback。

[Font1]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=16
Fallback=4

[Font2]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=18
Fallback=5

[Font3]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=20
Fallback=5

; 仅用作回退目标的字体索引——不被 UI 控件直接引用。
[Font4]
Type=TrueType
Path=NotoSansSC-Regular.ttf
Size=14

[Font5]
Type=TrueType
Path=NotoSansSC-Regular.ttf
Size=18

[FontRendering]
; 可选段。FontStashSharp 高级光栅化设置。
; 当整个段完全缺席时，使用下面显示的默认值。
; 注意：如果写了本段但省略了某个键，该键会回退到逐键的
; INI 默认值（0、0、1）——参见下方参考表。
KernelWidth=4
KernelHeight=4
FontResolutionFactor=5
TextureWidth=1024
TextureHeight=1024
GlyphRenderResult=Premultiplied
```

字体路径相对于 `Fonts.ini` 所在目录。`/` 与 `\` 均可接受。

### 属性参考

#### `[Font#]` 属性

| 属性 | 适用类型 | 说明 |
|----------|-----------|-------------|
| `Type` | 两者 | `TrueType` 或 `SpriteFont`（默认 `SpriteFont`）。 |
| `Path` | 两者 | 相对于 `Fonts.ini` 目录的文件路径。对 SpriteFont，`.xnb` 扩展名可选——会自动剥离并重新追加。 |
| `Size` | TrueType | 字体像素高度（默认 `16`）。这是 em-square 高度。字符的实际渲染高度可能因字体度量而略小。对回退字体忽略。 |
| `Fallback` | TrueType | 字符找不到时使用的另一个 TrueType 字体索引。链会被递归跟随；循环引用会被检测并忽略。 |

#### `[TextShaping]`

| 属性 | 默认 | 说明 |
|----------|---------|-------------|
| `Enabled` | `false` | 启用 HarfBuzz 文本整形。复杂文字（阿拉伯文、希伯来文、印地文）与 ZWJ 表情符号序列必需。纯拉丁或纯 CJK 文字建议关闭以获得更好性能。若启用但无法加载原生 HarfBuzz 库，会被强制置回 `false`。 |
| `EnableBiDi` | `true` | 为混合 LTR/RTL 文本启用双向文本支持。仅在 `Enabled=true` 时生效。 |
| `CacheSize` | `100` | 整形文本缓存条目数。CJK 或其他大字符集语言建议使用 1000 或更多。小于 1 的值会被重置为 100。 |

#### `[FontRendering]`

本表的默认值用于**整个** `[FontRendering]` 段缺席时（类默认值）。如果段存在但某个键缺失，该键使用括号中显示的逐键 INI 回退值。

| 属性 | 默认（段缺席） | 说明 |
|----------|--------------------------|-------------|
| `KernelWidth` | `4`（INI：`0`） | FontStashSharp 光栅化字形时应用的水平模糊核大小。非负。 |
| `KernelHeight` | `4`（INI：`0`） | 垂直模糊核大小。非负。 |
| `FontResolutionFactor` | `5`（INI：`1`） | 内部字形光栅化尺寸的倍率。大于 `1` 产生更清晰的输出，但图集更大。非负。 |
| `TextureWidth` | `1024` | 每个 FontStashSharp 图集页面的像素宽度（最小 1）。 |
| `TextureHeight` | `1024` | 每个 FontStashSharp 图集页面的像素高度（最小 1）。 |
| `GlyphRenderResult` | `Premultiplied` | 字形 alpha 的编码方式：`Premultiplied`（匹配预乘 alpha 的 `SpriteBatch`）、`NonPremultiplied`（匹配 `AlphaBlend`）或 `NoAntialiasing`（像素风字体的硬 1 位边缘）。匹配不区分大小写；非法值回退到 `Premultiplied`。 |

## 字符回退

回退通过 `Fallback` 属性**按字体索引**配置。渲染一个字符时：

1. 先尝试字体索引中定义的主字体
2. 找不到则沿 `Fallback` 链查找——加载被引用索引的字体文件，然后该索引的 fallback，依此类推
3. 整条链之后仍然找不到，则渲染为 `?`

回退链中的所有字体都按**发起**字体索引指定的尺寸渲染，而不是回退目标的尺寸。回退目标的 `Size` 仅在该字体索引本身作为主字体时使用。

这种按字体设计允许不同字体索引拥有不同的回退链。例如，20 号粗体字可以回退到与 14 号常规字不同的字体。

> **注意：** 回退只发生在 TrueType 字体之间。SpriteFont 索引不支持回退——如果字符在 SpriteFont 中缺失，会渲染为默认字符（`?`）。

## 字体索引

UI 控件通过索引引用字体，对应 `[Font0]`、`[Font1]` 等：

```ini
[MyLabel]
FontIndex=1
```

```csharp
myLabel.FontIndex = 1;
```

## 推荐字体
英文文本：
- [Roboto_SemiCondensed-Medium.ttf](https://fonts.google.com/specimen/Roboto)。
- [MozillaText-Medium.ttf](https://fonts.google.com/specimen/Mozilla+Text)。

CJK 文本：
- Noto CJK 字体，如 [NotoSansSC-Medium.ttf](https://fonts.google.com/noto/specimen/Noto+Sans+SC)（中文）、[NotoSansKR-Medium.ttf](https://fonts.google.com/noto/specimen/Noto+Sans+KR)（韩文）或 [NotoSansJP-Medium.ttf](https://fonts.google.com/noto/specimen/Noto+Sans+JP)（日文）。

## 推荐的像素级完美字体

如果你喜欢当前 SpriteFont 渲染风格——像素级完美、锐利、无抗锯齿的字体渲染——你只能选择一小部分 TTF/OTF 字体，并且必须预处理它们。

英文文本：
- Windows 自带的 Arial `arial.ttf`。预处理后在 14 px 下看起来锐利。
- Windows 自带的 Microsoft Sans Serif `micross.ttf`。预处理后在 14 px 下看起来锐利。

CJK 文本，可考虑以下选项：
- [GNU Unifont](https://unifoundry.com/unifont/)。Unicode 覆盖广，渲染像素级完美。**仅**支持 16 px 字号。没有粗体/斜体变体。
- [WenQuanYi Bitmap Song TTF](https://github.com/AmusementClub/WenQuanYi-Bitmap-Song-TTF)。这已经是"预处理"过的像素级完美字体。注意字号与文件名中的声称尺寸不对应，例如对 `WenQuanYi.Bitmap.Song.14px.ttf` 需要指定 `Size=15`。没有粗体/斜体变体。
- Windows 默认中文字体 SimSum `simsun.ttc`。在多种尺寸下都像素级完美（推荐 14 像素），但使用前**必须**按照下方"预处理字体文件"的说明处理字体文件。没有粗体/斜体变体。

## 示例

以下示例展示如何配置 `Fonts.ini`。

### 仅英文

```ini
[TextShaping]
Enabled=false
EnableBiDi=false
CacheSize=100

[Fonts]
Count=4

[Font0]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=14

[Font1]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=16

[Font2]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=18

[Font3]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=20
```

### 韩文翻译 + 中文回退

韩文字体为主，中文字体回退韩文字体缺失的任何字符。

```ini
[TextShaping]
Enabled=true
EnableBiDi=false
CacheSize=100

[Fonts]
Count=6

[Font0]
Type=TrueType
Path=NotoSansKR-Medium.ttf
Size=14
Fallback=4

[Font1]
Type=TrueType
Path=NotoSansKR-Medium.ttf
Size=16
Fallback=4

[Font2]
Type=TrueType
Path=NotoSansKR-Medium.ttf
Size=18
Fallback=5

[Font3]
Type=TrueType
Path=NotoSansKR-Medium.ttf
Size=20
Fallback=5

[Font4]
Type=TrueType
Path=NotoSansSC-Medium.ttf
Size=14

[Font5]
Type=TrueType
Path=NotoSansSC-Medium.ttf
Size=18
```

### 英文 + CJK 回退

英文字体为主，CJK 字体回退。英文字体中没有的字符（如中文）自动使用回退字体。

```ini
[TextShaping]
Enabled=false
EnableBiDi=false
CacheSize=100

[Fonts]
Count=5

[Font0]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=14
Fallback=4

[Font1]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=16
Fallback=4

[Font2]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=18
Fallback=4

[Font3]
Type=TrueType
Path=MozillaText-Bold.ttf
Size=20
Fallback=4

[Font4]
Type=TrueType
Path=NotoSansSC-Medium.ttf
Size=16
```

### SpriteFont（旧式）

```ini
[Fonts]
Count=4

[Font0]
Type=SpriteFont
Path=SpriteFont0

[Font1]
Type=SpriteFont
Path=SpriteFont1

[Font2]
Type=SpriteFont
Path=SpriteFont2

[Font3]
Type=SpriteFont
Path=SpriteFont3
```

`Path` 中的 `.xnb` 扩展名可选——省略时会自动补上。`SpriteFont0.xnb`、`SpriteFont1.xnb` 等文件必须存在于 Resources 文件夹中。

## 预处理字体文件

如果你喜欢类似 SpriteFont 的像素级完美渲染风格，我们建议预处理 TTF/OTF 字体文件。预处理会生成一个针对特定像素尺寸优化了矢量轮廓的新 TTF 文件，可以显著改善某些字体（尤其是带内嵌位图的 CJK 字体）的渲染质量。不过，并非所有字体预处理后都更好看，有些可能出现奇怪的外观。

请遵循以下指南。我们假设你有一台 Windows 10/11 PC，但类似步骤在 Linux/Mac 上同样适用。

1. 你的字体文件扩展名是什么？
    - 如果是 `.ttc`，请先按"TTC 字体"一节中的说明提取 TTF 文件。
    - 如果是 `.ttf` 或 `.otf`，继续第 2 步。

2. 从 https://fontforge.org/ 下载并安装 FontForge。在 FontForge 中打开你的字体文件。如果 FontForge 显示"Bad Font Name"警告，可以忽略此对话框。

3. 是否出现"Load Bitmap Font"对话框？
    - 是。这说明你的 TTF/OTF 文件**必须**预处理。选择一个字号（例如 14 px）并点击"OK"。记住你选择的尺寸。跳到第 5 步。
    - 否。你的字体文件不预处理也能正常工作。不过，如果你喜欢像素级完美渲染风格，可能仍想预处理，但务必在最后检查渲染质量。大多数字体预处理后会变差，但 Arial 和 Microsoft Sans Serif 是例外。想预处理就跳到第 4 步，否则直接在 `Fonts.ini` 中使用原始 TTF/OTF 文件，无需预处理。

对话框应类似这样：

![FontForge Load Bitmap Font 对话框](Images/ttf-fontforge-preprocess-bitmap-font-dialog.png)

4. 本步骤仅适用于：字体文件没有显示"Load Bitmap Font"对话框，或对话框不包含你期望的字号。选择 Element -> Bitmap Strikes Available。如果"Pixel Sizes"为空或不包含你之前选择的尺寸，需要手动填入像素尺寸并点击"OK"。

![FontForge Bitmap Strikes Available 对话框](Images/ttf-fontforge-preprocess-secondary-font-bitmap-available.png)

![FontForge Bitmap Strikes Available 对话框（已填入像素尺寸）](Images/ttf-fontforge-preprocess-secondary-font-create-bitmap.png)

5. File -> Generate Fonts。选择"No Outline Font"并将字体类型选为"BDF"，把文件保存为 `myfont.bdf`。如果被询问，BDF 分辨率使用 96 DPI。

![FontForge Generate Fonts 对话框](Images/ttf-fontforge-preprocess-generate-font.png)

6. 用支持打开大文件的文本编辑器（例如 VSCode 和 EmEditor）打开 `myfont.bdf`。检查第一行是 `STARTFONT 2.1` 还是 `STARTFONT 2.2`。

7. 如果第一行是 `STARTFONT 2.2`，需要使用 https://github.com/SadPencil/BdfToolSP 将 BDF 文件降级到 2.1 版。即使不是 2.2，你也可以通过运行降级命令用 BdfToolSP 清理 BDF 文件。此外，强烈建议用 `--font-name` 参数运行降级命令，以便设置新的字体名（例如"My Font 14px"），避免与原始 TTF/OTF 文件冲突。我们也在 `/AdditionalFiles/PreprocessFont` 目录中存放了一份 BdfToolSP 副本，但它可能过时。

8. 检查 BDF 文件中是否存在 `ENCODING 65`（字符 'A'）。这表示提取的位图字体是否包含基本 ASCII 字符。
    - 是。跳到第 11 步。
    - 否。说明提取的位图字体不包含基本 ASCII 字符，需要与另一个包含缺失字符的字体合并作为回退目标。

9. 本步骤仅适用于需要提供回退字符的用户。选择一个基本字体，例如 `micross.ttf`、`arial.ttf`。用 FontForge 打开它。对这个字体重复第 2 到第 7 步，保存为 `secondary.bdf`。

10. 使用 https://github.com/SadPencil/BdfToolSP 把 `myfont.bdf` 和 `secondary.bdf` 合并成 `merged.bdf`。把 `merged.bdf` 重命名为 `myfont.bdf`。同样，强烈建议用 `--font-name` 参数运行合并命令以设置唯一字体名，避免冲突。

11. 下载并安装最新 LTS 版本的 Eclipse Temurin（原名 AdoptOpenJDK）。如果已经安装了 JRE，可以跳过此步。

12. 从 https://github.com/kreativekorp/bitsnpicas/releases/ 下载 `BitsNPicas.jar`。一般来说应下载最新版本，但如果遇到问题，可以尝试 v2.2。我们也在 `/AdditionalFiles/PreprocessFont` 目录中存放了一份副本，但它可能过时。

13. 运行 `java -jar BitsNPicas.jar convertbitmap -f ttf -o myfont.ttf myfont.bdf` 创建预处理后的 TTF 文件。

14. 双击 TTF 文件。示例文本应能正确显示。如果字号与第 4 步选择的尺寸不匹配，你可能会觉得字体变丑了，但这意味着你已经成功预处理了字体文件。现在可以在 `Fonts.ini` 中使用这个 TTF 文件，并填上第 4 步选择的字号。

## TTC 字体

TTC（TrueType Collection）文件在一个文件中打包了多个字体。客户端只支持 TTF/OTF 文件——你需要先从 TTC 中提取想要的字体。

从 TTC 提取 TTF 的工具：

- 在线： [everythingfonts.com/ttc-to-ttf](https://everythingfonts.com/ttc-to-ttf) 或 [transfonter.org/ttc-unpack](https://transfonter.org/ttc-unpack)

- 使用 Python 和 fonttools 本地提取：
1. 安装最新 Python 3。
2. 运行 `python3 -m venv venv` 和 `venv\Scripts\activate`（Windows）或 `source venv/bin/activate`（Linux/Mac）创建并激活虚拟环境。
3. 运行 `pip install fonttools`（如果最新版有问题，用 `pip install fonttools==4.62.1`）。
4. 创建包含以下内容的 `extract_ttc.py`（[来源](https://github.com/fonttools/fonttools/discussions/2647#discussioncomment-3093867)）：
```python
#!/usr/bin/env python
from fontTools.ttLib.ttCollection import TTCollection
import os
import sys

filename = sys.argv[1]
ttc = TTCollection(filename)
basename = os.path.splitext(os.path.basename(filename))[0]
for i, font in enumerate(ttc):
    font.save(f"{basename}_{i}.ttf")
```
5. 运行 `python extract_ttc.py yourfont.ttc` 提取 TTF 文件。

- 使用 BREAKTTC 本地提取：
1. 从 https://archive.org/details/microsoft-truetype-sdk 下载 Microsoft TrueType SDK
2. 从 SDK 中提取 `TTC\breakttc.exe`。
3. 从 https://www.dosbox.com/ 下载并安装 DosBox
4. 在 DosBox 中运行 `breakttc.exe yourfont.ttc` 提取 TTF 文件。

- 另见： [Stack Overflow — Convert or extract TTC font to TTF](https://stackoverflow.com/questions/15455895/convert-or-extract-ttc-font-to-ttf-how-to)

## 已知限制

### 内嵌位图字体没有像素级完美渲染

某些 TTF/OTF 字体（尤其是 SimSun 或 WenQuanYi Zen Hei 等 CJK 字体）包含针对特定尺寸优化的内嵌位图字形。FontStashSharp 不使用这些内嵌位图——它始终从矢量轮廓光栅化。这意味着这些字体在其预期像素尺寸下可能看起来比预期更模糊。

如果需要像素级完美的 CJK 渲染，要么使用矢量轮廓已针对特定像素尺寸优化的字体（例如 16 px 的 [Unifont](https://unifoundry.com/unifont/index.html)，其位图数据已转换为矢量轮廓），要么遵循上方"预处理字体文件"的说明。无论哪种情况，预处理字体只有一个最佳尺寸，在其他尺寸下使用可能看起来更差。

### SpriteFont 没有回退

SpriteFont 索引不能使用 `Fallback` 属性。如果需要对缺失字符进行回退，请为所有字体索引使用 TrueType 字体。

### 不支持可变字体

与 TTC 文件不同，可变字体（`.ttf` 或 `.otf` 扩展名）在一个文件中包含多个字体变体（例如不同的字重）。客户端不支持可变字体。

### 不支持假粗体

某些字体（尤其是 SimSun 等 CJK 字体）只提供常规字重，没有粗体变体。Windows 在请求粗体时通过算法加粗常规字体来提供"假粗体"效果。但 FontStashSharp 不支持这种假粗体效果。如果需要粗体变体，必须选择包含真实粗体字重的字体文件。

## 常见问题

### 我应该如何在 TTF/OTF 和 SpriteFont 之间选择？

以下情况选择 SpriteFont：
1. 想要极致性能。
2. 想要像素级完美、无抗锯齿的渲染风格。
3. 不需要对缺失字符进行回退。
4. 不关心 RTL 文本或复杂文字。
5. 只需要约 10,000 个或更少的字符。

以下情况选择 TTF/OTF：
1. 不介意额外的字体渲染开销。
2. 不介意略模糊的渲染风格。
3. 需要对缺失字符进行回退。
4. 需要正确支持 RTL 文本和复杂文字。
5. 需要覆盖大的字符集。

### 我可以把预处理后的 TTF 字体文件当作普通 TTF 字体用于其他应用吗？

绝对不行！预处理后的 TTF 文件本质上是矢量化位图字体，只在其预处理的特定尺寸下好看。在其他尺寸或其他应用中使用很可能导致糟糕的渲染质量。

## 故障排查

**字体未加载** — 检查 `client.log` 中的 `FontManager:` 消息，并确认文件路径正确且是有效的 TTF/OTF。

**使用了错误的字体** — 记住先尝试主字体，然后按顺序沿回退链。检查是否有其他位置的 `Fonts.ini` 被加载。

**字符渲染为 `?`** — 该字符不在回退链的任何字体中。添加一个覆盖它的字体作为回退目标，并在字体索引上设置 `Fallback`。

**性能问题** — 不需要时关闭 `TextShaping`，缩短回退链长度；CPU 或 GPU 占用高时增大 `CacheSize`，内存占用高时减小 `CacheSize`。
