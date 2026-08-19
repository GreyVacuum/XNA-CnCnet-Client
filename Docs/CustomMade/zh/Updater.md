# 关于使用 XNA CnCNet 客户端更新器功能的说明

更新器相关文件
-------------------

### 开发者文件
**这些文件仅供模组开发者使用，不得分发给其他人！**
- **版本文件写入器（Version File Writer）**：为更新器写入版本文件的软件。可执行文件与示例配置文件[包含在客户端仓库中](../AdditionalFiles/VersionFileWriter)。程序源码见[此处](https://github.com/Starkku/VersionWriter)。
- **更新服务器脚本**（`preupdateexec` 与 `updateexec`，示例文件[包含在客户端仓库中](../AdditionalFiles/UpdateServerScripts)）：可用于重命名、移动或删除文件与目录的脚本文件。它们分别在更新前和更新后被更新器下载并执行。它们可以放在下载镜像指定的同一服务器文件夹中。注意：即使后续更新过程本身失败，`preupdateexec` 所做的更改**也不会**被回滚。此外，无论当前本地或服务器版本状态与信息如何，两个脚本都会被执行。

### 可分发文件
- **更新器配置文件**（`Resources/UpdaterConfig.ini`，随客户端仓库的[默认资源](../DXMainClient/Resources/DTA)一起提供）：客户端的更新器配置，设置更新器的下载镜像和可用的自定义组件信息。如果找不到该文件，客户端会回退到使用旧式 `updateconfig.ini`，其语法不同且不允许设置自定义组件信息。
- **第二阶段更新器**（`Resources/Binaries/Updater/SecondStageUpdater.exe`，现在是客户端二进制的一部分）：第二阶段更新器可执行文件，在所有文件下载完成后把它们复制到正确位置，完成后重新启动客户端。客户端启动器可执行文件从 `Resources/ClientDefinitions.ini` 的 `LauncherExe` 键读取（非 Windows 平台改用 `UnixLauncherExe`；存在 `Launcher\{名称}-{OSArchitecture}{扩展名}` 文件时优先使用）。如果该文件不存在或由于任何原因无法读取，第二阶段更新器完成后客户端不会自动重启。

基本用法
-----------

## 快速指南
1. 搭建一个 Web 服务器，创建一个公开可访问的目录用于下载更新。
2. 在客户端配置中，把上述目录的 URL 添加到 `Resources/UpdaterConfig.ini` 的可用下载镜像列表中。
3. 修改文件与 `VersionConfig.ini`。
4. 运行 `VersionWriter.exe`。
5. 把 `VersionWriter-CopiedFiles` 的内容与更新服务器脚本上传到上述 Web 服务器目录。

## 详细说明
要通过 XNA CnCNet 客户端获得自动更新，需要搭建一个更新 Web 服务器，以便更新过程中客户端下载更新文件。文件的 URL 路径（不含更新位置部分）必须与文件相对于模组文件夹的本地路径一致才能成功下载（例如，更新位置为 `https://your.test/location/of/updates/` 时，文件 `Resources/Binaries/Windows/clientdx.dll` 需要在 `https://your.test/location/of/updates/Resources/Binaries/Windows/clientdx.dll` URL 上可访问）。除更新服务器脚本外，更新器不明确要求更新 Web 服务器上存在或运行任何其他文件或特定软件。

要生成需要上传到服务器的更新信息，请编辑 `VersionConfig.ini`，包含所有要分发的文件（如果为了省带宽且不想允许完整下载，也可以只包含更新过的文件）。每次需要向玩家推送更新时（以及当你更改 `VersionConfig.ini` 中的内容时），都必须更改上述配置文件 `[Version]` 段下的版本键，客户端才会提示更新。如果需要强制用户手动下载更新，可以更改 `[UpdaterVersion]` 段下的键。之后运行 `VersionWriter.exe`，把 `VersionWriter-CopiedFiles` 的内容连同更新脚本一起上传到更新服务器。

版本写入器与更新器的功能与配置文件的更全面说明见下文。

功能
-------

### 版本文件写入器
版本文件写入器是一个写入客户端及其更新器使用的 `version` 文件的程序。它从工作目录读取名为 `VersionConfig.ini` 的文件以获取设置和要包含的文件列表。

客户端仓库中随版本文件写入器附带的示例 `VersionConfig.ini` 包含解释大部分功能与特性的注释。

`VersionWriter.exe` 接受以 `/` 或 `-` 开头的命令行开关参数。支持以下开关：
- `-LOG`：在程序目录生成日志文件。
- `-QUIET`：不生成控制台输出。
- `-SUPRESSINPUTS`：不要求用户输入确认操作。

此外还可以提供一个非开关参数，用于设置程序的工作目录——这允许从模组目录外部运行 VersionWriter。

#### 选项
在 `VersionConfig.ini` 的 `[Options]` 段下设置。
- `EnableExtendedUpdaterFeatures`：启用后，开启压缩归档、更新器版本与手动下载 URL 等附加更新器功能。默认为 `false`。
- `RecursiveDirectorySearch`：启用后，对 `[Include]` 中给出的目录递归遍历每个子目录。默认为 `false`。
- `IncludeOnlyChangedFiles`：启用后，版本文件写入器总是生成两个版本文件——一个包含所有内容（`version_base`）与一个只含变更文件的正式版本文件（`version`）。注意 `version_base` 应保留，下次运行版本文件写入器时用它比较哪些文件发生了变更。默认为 `false`。
- `CopyArchivedOriginalFiles`：启用后，归档文件的原始版本也会被复制到复制文件目录。默认为 `false`。
- `ExcludeHiddenAndSystemFiles`：启用后，任何被标记为隐藏或系统保护的文件与目录（包括其中的所有文件与子目录，无论其他设置如何）都会被排除。此项默认也为 `true`。
- `ApplyTimestampOnVersion`：启用后，模组版本字符串被当作 [.NET 时间戳/日期时间格式字符串](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings)处理，并应用当前本地时间。默认为 `false`。
- `NoCopyMode`：启用后完全不复制任何文件，只生成版本文件。设置此项也会禁用归档文件功能，无论其他设置如何。默认为 `false`。

#### 更新器版本与手动下载 URL
在 `VersionConfig.ini` 中设置 `[UpdaterVersion]` 会把此信息写入 `version` 文件，允许开发者控制客户端允许从版本信息下载文件的版本。本地与服务器版本文件之间的更新器版本不匹配会通过更新器状态消息建议用户手动下载更新。缺失或格式错误的更新器版本（本地与服务器均如此）等价于 `N/A`；如果服务器更新器版本设置为 `N/A` 或缺失，更新器将完全绕过不匹配检查。

另外设置 `[ManualDownloadURL]` 后，除了显示更新器状态消息外，在出现更新器版本不匹配时还会弹出一个通知对话框，以所提供的 URL 作为下载链接。

#### 压缩归档
更新器支持下载并解压 LZMA 压缩数据归档。要压缩的文件应包含在 `VersionConfig.ini` 的 `[ArchiveFiles]` 下。注意它们仍须首先通过 `[Include]` 包含。结果 `version` 文件中会有信息让客户端知道它应该下载归档，而 `VersionWriter-CopiedFiles` 文件夹中放置的是带 `.lzma` 扩展名的压缩文件而非原始文件。

#### 自定义组件
即使是最初的 XNA CnCNet 客户端也支持自定义组件，但由于 ID 与文件名在更新器中是硬编码的，其用途有限。更新器的自定义组件信息可以在 `Resources/UpdaterConfig.ini` 中设置，见下文。对版本文件写入器而言，任何自定义组件都应包含在 `[AddOns]` 下，使用 `ID=filename` 语法，如示例 `VersionConfig.ini` 所示。自定义组件文件名**不应**列在 `[Include]` 下。文件名可以列在 `[ArchiveFiles]` 下以启用压缩归档。

- 自定义组件下载文件路径（在 `Resources/UpdaterConfig.ini` 中）接受绝对 URL 并正确使用它们，因此可以定义必须从别处下载的自定义组件。

### 更新器配置
随客户端文件附带的示例 `Resources/UpdaterConfig.ini` 包含解释大部分功能与特性的注释。

`[Settings]` 下目前唯一支持的全局更新器设置是 `IgnoreMasks`，它允许自定义即使包含在 `version` 文件中也免除文件完整性检查的文件名列表。默认掩码列表为 `.rtf,.txt,Theme.ini,gui_settings.xml`（以空格或逗号分隔）。

#### 下载镜像
可用的下载镜像列表，从中下载版本信息与文件。列在 `[DownloadMirrors]` 下，为逗号分隔的值，包含 URL、UI 显示名与位置。位置可选，可以省略。

如果找不到任何下载镜像，客户端选项中的"更新器"与"更新器和组件"选项将不可用。

#### 自定义组件
更新器可用的自定义组件列表。列在 `[CustomComponents]` 下，为逗号分隔的值，按此顺序包含：**UI 显示名**、`version` 文件中使用的自定义组件 ID、下载路径/URL、本地文件名，以及可选的禁用下载路径/URL 归档扩展名的标志。前四个值必填；少于四个值的条目会被跳过。

下载路径/URL 支持绝对 URL，允许自定义组件从当前更新服务器之外的位置下载，但这也把它限制为单个下载位置，而不是每个下载镜像各一个。

下载路径归档扩展名禁用标志是布尔值（yes/no、true/false），可选，默认为 false。

如果找不到自定义组件信息，自定义组件与客户端选项中的"组件"标签页将不可用。
