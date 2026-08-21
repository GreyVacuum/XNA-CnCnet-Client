using System.Linq;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// 游戏条目包装：在 DefaultCnCNetGame 之上附加排序与开关控制。
    /// Wraps a DefaultCnCNetGame with ordering / toggle controls.
    ///
    /// Priority — 越小越靠前（在同一集合内排序有效）。
    /// Enabled  — false 时不加入游戏集合（代码保留，仅运行时禁用）。
    ///
    /// 公开与私有游戏统一使用此类，公开版本即使不编译 Private/ 文件夹也能正常构建。
    /// </summary>
    internal sealed class GameEntry
    {
        public GameEntry(int priority, bool enabled, DefaultCnCNetGame game)
        {
            Priority = priority;
            Enabled = enabled;
            Game = game;
        }

        public int Priority { get; }
        public bool Enabled { get; }
        public DefaultCnCNetGame Game { get; }

        /// <summary>
        /// 将一组 GameEntry 按 Priority 升序、且仅保留 Enabled 的项，展开为 DefaultCnCNetGame[]。
        /// Orders entries by Priority (ascending) and returns only Enabled ones as DefaultCnCNetGame[].
        /// </summary>
        public static DefaultCnCNetGame[] Order(params GameEntry[] entries) =>
            entries
                .Where(e => e.Enabled)
                .OrderBy(e => e.Priority)
                .Select(e => e.Game)
                .ToArray();
    }
}
