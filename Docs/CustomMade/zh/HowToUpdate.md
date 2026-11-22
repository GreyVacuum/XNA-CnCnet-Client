# 如何将你的模组更新到最新客户端版本

本指南介绍如何为任何使用 XNA CnCNet 客户端的模组或游戏包更新客户端版本（例如 Tiberian Sun Client、CnCNet YR、YR Mod Base 或任何由它们派生的模组等）。

> [!WARNING]
> 强烈建议同时保持客户端启动器（位于模组文件夹中、用于启动实际客户端的 EXE 文件）为最新版本。更新方法：从[其仓库](https://github.com/CnCNet/xna-cncnet-client-launcher/)下载最新发布版，用 [Resource Hacker](https://www.angusj.com/resourcehacker/) 打开 EXE，替换图标，保存并替换你当前使用的 EXE（例如 `TiberianSun.exe` 或 `CnCNetYRLauncher.exe`）。也不要忘记把压缩包中的其余文件复制到游戏文件夹！

## 为游戏包更新 XNA CnCNet 客户端二进制文件

1. **下载最新的客户端二进制发布版：**
   - 在 [XNA CnCNet Client 仓库发布页](https://github.com/CnCNet/xna-cncnet-client/releases) 找到最新的客户端版本。
   - 下载 `[xna-cncnet-client-X.Y.Z.7z]` 文件，其中应包含更新后的 `Resources` 文件夹。
   - 记录发布说明中提到的迁移步骤。

2. **清理旧的二进制文件夹：**
   - 进入你的本地游戏/模组仓库或工作文件夹。
   - 找到“游戏根目录”中的 `Resources/` 文件夹。
   - **重要：** 删除 `Binaries` 与 `BinariesNET8`，确保不残留过时或已改名的库文件。

3. **将文件粘贴到游戏包仓库：**
   - 进入你的本地游戏/模组仓库或工作文件夹。
   - 将先前下载的 `[xna-cncnet-client-X.Y.Z.7z]` 中的 `Resources` 文件夹解压到“游戏根目录”中。
   - 此时**必须**出现替换 `Resources/` 文件夹及其内部文件的提示。若没有，说明你所在的目录不对。

4. **执行迁移步骤：**
   - 如果是升级到下一个版本：按照第 1 步中提到的发布说明操作。
   - 如果跳过了多个版本：要么
     - 查阅所有跳过的发布说明并执行迁移；
     - 要么参考[客户端迁移文档](Migration.md)。

5. **运行打包后的客户端进行测试：**
   - 启动：Windows 上运行 `YourClientLauncher.exe`，Linux/Mac 上运行 `YourClientLauncher.sh`（名称因模组/游戏客户端包而异）。
   - 在联机大厅中核对版本哈希与客户端版本。
   - 能正常工作吗？如果不能，说明你遗漏了某些迁移步骤或在上述步骤中出了错，请检查修改或从头开始。

之后，如果你使用 Git，可以提交/推送更改；或者发布更新。
