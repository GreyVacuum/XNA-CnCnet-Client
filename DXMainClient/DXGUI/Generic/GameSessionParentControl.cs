using System;
using System.Collections.Generic;
using System.Linq;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// 锁定适配器：由受父控件约束的子控件实现。
/// 引擎（GameSessionParentControl）仅在锁定状态发生跳变时回调，
/// 适配器负责把状态落到具体控件的视觉与交互上
/// （CheckBox：AllowChanges/AllowChecking + 禁用纹理；DropDown：AllowDropDown + 输入框入口等）。
/// 新的子控件类型（如未来的 GameSessionTextBox）只需实现本接口即可接入，引擎零改动。
/// </summary>
public interface IGameSessionParentLockAdapter
{
    /// <summary>
    /// 约束满足（子项可编辑）。含首次绑定成功后的初始应用。
    /// </summary>
    void OnUnlocked();

    /// <summary>
    /// 约束不满足（子项锁定）。
    /// 锁定显示值（CheckBox 选中态 / DropDown 选中项 / 纹理）可从 engine 按需解析。
    /// </summary>
    void OnLocked(GameSessionParentControl engine, int firstLockedIndex);
}

/// <summary>
/// 通用"父控件约束"引擎（组模型）。
/// 子控件（GameSessionCheckBox、GameSessionDropDown 等任意 IGameSessionParentLockAdapter 实现）
/// 由一个或多个"父控件组"决定是否可编辑。
///
/// 组模型（索引即组号，组间互不重叠）：
///   - 每个 ParentControlNameN（含逗号列表）定义一个独立的组 N，
///     逗号列表的每一项是组 N 的一个父控件成员；
///   - ParentControlRequiredValueN / ParentControlLockedValueN /
///     ParentCheckBoxTextureN / ParentControlMatchModeN 作用于组 N 的全部成员（键序无关）；
///   - 组间 AND（各管各的）：任一组不满足 → 本控件禁用；全部组满足 → 可编辑；
///   - 组内方向：All（默认）= 组内任一成员满足 → 组满足；Any = 组内全部成员满足 → 组满足；
///   - 无后缀键（ParentControlName 等）等价于组 0。
///
/// 约束满足 → OnUnlocked；不满足 → OnLocked；父控件尚未绑定成功时不干预。
/// 单父控件/单组时所有模式等价。
///
/// 职责划分：
///   - 引擎：INI 解析（ParentControl* 键）、父控件查找/订阅、纯函数判定、跳变检测；
///   - 适配器：锁定/解锁的具体视觉与交互动作。
///
/// 刷新模型为"跳变驱动"：仅当 可编辑⇄锁定 状态变化、或 INI 配置被重新解析时，
/// 才调用一次适配器——不与宿主代码（如 MultiplayerGameLobby 对 AllowChanges
/// 的房主锁定）发生每帧覆写对抗。锁定态每帧重申以对抗宿主刷新。
/// </summary>
public sealed class GameSessionParentControl
{
    /// <summary>
    /// 单个父控件成员的约束信息。
    /// </summary>
    private sealed class ParentBinding
    {
        /// <summary>父控件名（在 UI 层级中查找）。</summary>
        public string Name;

        /// <summary>运行时解析到的父控件（XNACheckBox 或 XNADropDown）。</summary>
        public XNAControl Control;

        /// <summary>成员级 Required 覆盖（仅普通式逗号值映射写入；
        /// 通常为 null，判定回退到组级 → 通用值）。</summary>
        public string RequiredValueRaw;

        /// <summary>成员级 LockedValue 覆盖（同上）。</summary>
        public string LockedValueRaw;

        /// <summary>成员级锁定纹理覆盖（同上）。</summary>
        public string TextureCheckedRaw;
        public string TextureUncheckedRaw;
    }

    /// <summary>
    /// 父控件组：一个 ParentControlNameN 键定义的全部父控件成员 + 该组的值与方向。
    /// 组级值作用于全部成员（键序无关），成员级覆盖优先。
    /// </summary>
    private sealed class ParentGroup
    {
        /// <summary>组内父控件成员（逗号列表展开，顺序保持 INI 书写顺序）。</summary>
        public List<ParentBinding> Members = new();

        /// <summary>组内匹配方向：true = All 方向（任一成员满足 → 组满足）；
        /// false = Any 方向（全部成员满足 → 组满足）。</summary>
        public bool DirectionAll = true;

        /// <summary>是否已由 ParentControlMatchModeN 显式指定方向
        /// （显式指定后，全局 ParentControlMatchMode 不再覆盖该组）。</summary>
        public bool DirectionExplicit;

        /// <summary>组级 Required 值（原始字符串；CheckBox: True/False，DropDown: 索引或项 Tag）。</summary>
        public string RequiredValueRaw;

        /// <summary>组级锁定显示值（原始字符串）。</summary>
        public string LockedValueRaw;

        /// <summary>组级 CheckBox 子项锁定纹理（选中态/未选态）。</summary>
        public string TextureCheckedRaw;
        public string TextureUncheckedRaw;

        // ---- DropDown 专用键族（ParentDropDownMode/Value/Compare，作用于组内 DropDown 成员）----
        // 组配置了 DropDownValueRaw 时，DropDown 成员优先用本键族判定；
        // 未配置时回退到 ParentControlRequiredValue（索引/Tag 匹配）

        /// <summary>匹配模式：Index（默认，按 SelectedIndex）/ Tag（按项 Tag）/ Text（按显示文本）。</summary>
        public string DropDownModeRaw;

        /// <summary>比较目标值（按 Mode 解释；配置后启用本键族判定）。</summary>
        public string DropDownValueRaw;

        /// <summary>比较方式：==（默认）/ != / > / >= / < / <= / *；
        /// 英文单词保留为兼容别名。区间比较仅 Index 模式支持。</summary>
        public string DropDownCompareRaw;
    }

    /// <summary>
    /// 父控件约束的当前判定结果（纯数据，Evaluate 无副作用）。
    /// </summary>
    public struct ParentControlState
    {
        /// <summary>是否存在任何父控件配置。</summary>
        public bool HasConfiguration { get; set; }

        /// <summary>是否至少有一个父控件绑定成功。</summary>
        public bool AnyBound { get; set; }

        /// <summary>
        /// 约束是否满足（可编辑）。
        /// 组间 AND：全部组满足才可编辑；任一组不满足即禁用。
        /// </summary>
        public bool AllSatisfied { get; set; }

        /// <summary>首个已绑定成员所在组的组号（用于锁定显示取值）。</summary>
        public int FirstLockedIndex { get; set; }
    }

    // 组号 → 父控件组（索引即组号，组间互不重叠）
    private readonly Dictionary<int, ParentGroup> groups = new();

    // 各组显式指定的匹配方向（ParentControlMatchModeN）；
    // 未指定的组回退到全局 ParentControlMatchMode
    private readonly Dictionary<int, bool> groupModeOverrides = new();

    // 无索引通用值（普通式键写入；组级未配置时回退到这些值）
    private string commonRequiredValueRaw;
    private string commonLockedValueRaw;
    private string commonTextureCheckedRaw;
    private string commonTextureUncheckedRaw;

    // 全局匹配模式（按"何时禁用"命名）：
    // All（默认）= 组内任一成员满足即可用；Any = 组内全部成员满足才可用
    private bool matchModeAll = true;

    // 锁定适配器（子控件在 Initialize 时通过 Attach 提供）
    private IGameSessionParentLockAdapter adapter;

    // 跳变驱动状态：INI 重新解析或尚未应用过 → 需要应用一次
    private bool appliedStateDirty = true;
    private bool lastAppliedSatisfied;

    // 最近一次 Refresh 传入的子控件（用于订阅回调与查找）
    private XNAControl owner;

    /// <summary>是否存在任何父控件配置（任一组含至少一个成员）。</summary>
    public bool HasConfiguration => groups.Values.Any(g => g.Members.Count > 0);

    /// <summary>
    /// 是否已有任一父控件绑定成功（之后不再每帧重试查找；
    /// INI 重解析/成员重建时复位）。
    /// </summary>
    public bool BindingsResolved { get; private set; }

    /// <summary>
    /// 挂接锁定适配器（通常为子控件自身，Initialize 中调用一次）。
    /// </summary>
    public void Attach(IGameSessionParentLockAdapter adapter)
        => this.adapter = adapter;

    // ------------------------------------------------------------------
    // INI 键解析（仅 ParentControl* 键族）
    // ------------------------------------------------------------------

    /// <summary>
    /// 解析 ParentControl 系列 INI 键。
    /// 每次解析到本键族键时会使已应用状态失效，
    /// 下一次 Refresh 会按当前配置重新应用一次（支持 INI 重读场景）。
    /// 组级值直接存于组对象，键序无关。
    /// </summary>
    /// <returns>true 表示该键属于父控件系列并已处理。</returns>
    public bool TryParseAttribute(string key, string value)
    {
        switch (key)
        {
            // 组 0：单一父控件名 或 逗号分隔多个父控件名（逗号列表 = 全量重置）
            case "ParentControlName":
                ParseParentNames(value);
                return true;

            case "ParentControlRequiredValue":
                ParseParentRequiredValues(value);
                return true;

            case "ParentControlLockedValue":
                ParseParentLockedValues(value);
                return true;

            case "ParentCheckBoxTexture":
                // 仅 CheckBox 子项使用的锁定纹理（DropDown 子项的禁用视觉
                // 由控件侧自动置灰，无需纹理）。支持 "checkedTex,uncheckedTex"
                // 或单一纹理（两个状态共用）
                if (!string.IsNullOrEmpty(value))
                {
                    ParseTexturePair(value, out var c, out var u);
                    commonTextureCheckedRaw = c;
                    commonTextureUncheckedRaw = u;
                    appliedStateDirty = true;
                }
                return true;

            case "ParentControlMatchMode":
                // 全局默认方向：All（默认）= 组内任一成员满足即可用；
                // Any = 组内全部成员满足才可用。
                // 仅作用于未用 MatchModeN 显式指定的组
                if (string.Equals(value?.Trim(), "Any", StringComparison.OrdinalIgnoreCase))
                    matchModeAll = false;
                else if (string.Equals(value?.Trim(), "All", StringComparison.OrdinalIgnoreCase))
                    matchModeAll = true;

                foreach (var g in groups.Values)
                {
                    if (g.DirectionExplicit)
                        continue;
                    g.DirectionAll = matchModeAll;
                }
                appliedStateDirty = true;
                return true;

            // ---- DropDown 专用键族（无后缀 = 组 0；与 N 式共用组级字段）----
            // 取值支持 "…" 引号包裹（如 ParentDropDownCompare="=="），解析时剥离引号与空白
            case "ParentDropDownMode":
                GetOrCreateGroup(0).DropDownModeRaw = StripQuotes(value);
                appliedStateDirty = true;
                return true;

            case "ParentDropDownValue":
                GetOrCreateGroup(0).DropDownValueRaw = StripQuotes(value);
                appliedStateDirty = true;
                return true;

            case "ParentDropDownCompare":
                GetOrCreateGroup(0).DropDownCompareRaw = StripQuotes(value);
                appliedStateDirty = true;
                return true;
        }

        // 组级匹配方向：ParentControlMatchModeN（作用于组 N 的全部成员）
        int mIdx = ParseSuffix(key, "ParentControlMatchMode");
        if (mIdx >= 0)
        {
            if (string.Equals(value?.Trim(), "All", StringComparison.OrdinalIgnoreCase))
            {
                groupModeOverrides[mIdx] = true;
                if (groups.TryGetValue(mIdx, out var g))
                {
                    g.DirectionAll = true;
                    g.DirectionExplicit = true;
                }
            }
            else if (string.Equals(value?.Trim(), "Any", StringComparison.OrdinalIgnoreCase))
            {
                groupModeOverrides[mIdx] = false;
                if (groups.TryGetValue(mIdx, out var g))
                {
                    g.DirectionAll = false;
                    g.DirectionExplicit = true;
                }
            }
            // 无效取值忽略，保持该组当前方向
            appliedStateDirty = true;
            return true;
        }

        // 组 N 的父控件名（单名或逗号列表）：NameN 定义独立的组 N
        int idx = ParseSuffix(key, "ParentControlName");
        if (idx >= 0)
        {
            ParseIndexedNames(idx, value);
            return true;
        }

        // 组级 Required：作用于组 N 全部成员（键序无关）
        idx = ParseSuffix(key, "ParentControlRequiredValue");
        if (idx >= 0)
        {
            GetOrCreateGroup(idx).RequiredValueRaw = value;
            appliedStateDirty = true;
            return true;
        }

        // 组级 Locked：作用于组 N 全部成员（键序无关）
        idx = ParseSuffix(key, "ParentControlLockedValue");
        if (idx >= 0)
        {
            GetOrCreateGroup(idx).LockedValueRaw = value;
            appliedStateDirty = true;
            return true;
        }

        // 组级 CheckBox 锁定纹理：作用于组 N 全部成员（键序无关）
        idx = ParseSuffix(key, "ParentCheckBoxTexture");
        if (idx >= 0)
        {
            ParseTexturePair(value, out var c, out var u);
            var g = GetOrCreateGroup(idx);
            g.TextureCheckedRaw = c;
            g.TextureUncheckedRaw = u;
            appliedStateDirty = true;
            return true;
        }

        // ---- DropDown 专用键族 N 式（作用于组 N；取值同样支持 "…" 引号包裹）----

        idx = ParseSuffix(key, "ParentDropDownMode");
        if (idx >= 0)
        {
            GetOrCreateGroup(idx).DropDownModeRaw = StripQuotes(value);
            appliedStateDirty = true;
            return true;
        }

        idx = ParseSuffix(key, "ParentDropDownValue");
        if (idx >= 0)
        {
            GetOrCreateGroup(idx).DropDownValueRaw = StripQuotes(value);
            appliedStateDirty = true;
            return true;
        }

        idx = ParseSuffix(key, "ParentDropDownCompare");
        if (idx >= 0)
        {
            GetOrCreateGroup(idx).DropDownCompareRaw = StripQuotes(value);
            appliedStateDirty = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 剥离 INI 取值两侧的空白与成对双引号（用于值含 =、> 等符号时的书写形式，
    /// 如 ParentDropDownCompare="==" 或 =" 内容 "）。无引号时原样返回（仅 Trim）。
    /// </summary>
    private static string StripQuotes(string raw)
    {
        if (raw == null)
            return null;

        string v = raw.Trim();
        if (v.Length >= 2 && v[0] == '"' && v[v.Length - 1] == '"')
            v = v.Substring(1, v.Length - 2).Trim();

        return v;
    }

    /// <summary>
    /// 解析 ParentControlNameN：值支持逗号列表，每一项是组 N 的一个成员。
    /// 索引即组号，组间互不重叠（组 N 的展开不占用其它组的索引）。
    /// 组成员全量重建：旧成员一律解绑（INI 重解析可能伴随控件树重建，
    /// 名字相同不代表缓存的控件实例仍有效），由 Bind 按当前层级重新解析。
    /// </summary>
    private void ParseIndexedNames(int idx, string value)
    {
        var parts = string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

        appliedStateDirty = true;

        if (!groups.TryGetValue(idx, out var g))
        {
            if (parts.Length == 0)
                return; // 组不存在且新列表为空：无事可做

            g = new ParentGroup();
            groups[idx] = g;
        }

        // 全量重建成员：解绑全部旧成员（健壮性优先，不做名字比较）
        bool hadBound = false;
        foreach (var old in g.Members)
        {
            if (old.Control != null)
            {
                Unsubscribe(old.Control);
                hadBound = true;
            }
        }
        g.Members.Clear();

        if (parts.Length == 0)
        {
            // 空列表：移除整组
            groups.Remove(idx);
            BindingsResolved = false;
            return;
        }

        foreach (var name in parts)
            g.Members.Add(new ParentBinding { Name = name });

        // 组方向：显式 MatchModeN 优先，否则回退全局 MatchMode
        g.DirectionAll = groupModeOverrides.TryGetValue(idx, out var ov) ? ov : matchModeAll;
        g.DirectionExplicit = groupModeOverrides.ContainsKey(idx);

        if (hadBound)
            BindingsResolved = false;
    }

    /// <summary>
    /// 解析无后缀 ParentControlName（等价于组 0）。
    /// 逗号列表式：全量重置（清空全部组）后重建组 0——保留历史行为，
    /// 作为 INI 重解析后幽灵绑定的自愈入口；
    /// 单一名字：仅重建组 0，不影响其它组。
    /// </summary>
    private void ParseParentNames(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        appliedStateDirty = true;

        if (value.Contains(','))
        {
            // 逗号列表式：清空全部组（解绑事件）后按顺序重建组 0；
            UnsubscribeAll();
            groups.Clear();
            BindingsResolved = false;

            var parts = value.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();

            var g = new ParentGroup();
            g.DirectionAll = groupModeOverrides.TryGetValue(0, out var ov) ? ov : matchModeAll;
            g.DirectionExplicit = groupModeOverrides.ContainsKey(0);
            foreach (var name in parts)
                g.Members.Add(new ParentBinding { Name = name });
            groups[0] = g;
        }
        else
        {
            // 单一名字：仅重建组 0
            ParseIndexedNames(0, value.Trim());
        }
    }

    /// <summary>
    /// 解析无后缀 ParentControlRequiredValue。
    /// 逗号值列表：按组序/成员序扁平映射到各成员（值不足时用最后一个值补齐）；
    /// 单一值：写入全部已存在成员；无任何成员时记为通用值（键序无关兜底）。
    /// </summary>
    private void ParseParentRequiredValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        appliedStateDirty = true;

        var members = FlattenMembers();
        if (members.Count == 0)
        {
            commonRequiredValueRaw = value.Trim();
            return;
        }

        if (value.Contains(','))
        {
            var parts = value.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
            if (parts.Length == 0)
                return;

            for (int i = 0; i < members.Count; i++)
                members[i].RequiredValueRaw = parts[Math.Min(i, parts.Length - 1)];
        }
        else
        {
            string raw = value.Trim();
            foreach (var m in members)
                m.RequiredValueRaw = raw;
        }
    }

    /// <summary>
    /// 解析无后缀 ParentControlLockedValue（映射规则同 Required）。
    /// </summary>
    private void ParseParentLockedValues(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        appliedStateDirty = true;

        var members = FlattenMembers();
        if (members.Count == 0)
        {
            commonLockedValueRaw = value.Trim();
            return;
        }

        if (value.Contains(','))
        {
            var parts = value.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
            if (parts.Length == 0)
                return;

            for (int i = 0; i < members.Count; i++)
                members[i].LockedValueRaw = parts[Math.Min(i, parts.Length - 1)];
        }
        else
        {
            string raw = value.Trim();
            foreach (var m in members)
                m.LockedValueRaw = raw;
        }
    }

    /// <summary>
    /// 按组序、组内成员序扁平列出全部成员（普通式值映射用）。
    /// </summary>
    private List<ParentBinding> FlattenMembers()
        => groups.OrderBy(k => k.Key)
            .SelectMany(k => k.Value.Members)
            .ToList();

    private ParentGroup GetOrCreateGroup(int index)
    {
        if (!groups.TryGetValue(index, out var g))
        {
            g = new ParentGroup();
            g.DirectionAll = groupModeOverrides.TryGetValue(index, out var ov) ? ov : matchModeAll;
            g.DirectionExplicit = groupModeOverrides.ContainsKey(index);
            groups[index] = g;
        }
        return g;
    }

    private static void ParseTexturePair(string value, out string checkedTex, out string uncheckedTex)
    {
        checkedTex = uncheckedTex = null;
        if (string.IsNullOrEmpty(value))
            return;

        if (value.Contains(','))
        {
            var parts = value.Split(',').Select(s => s.Trim()).ToArray();
            if (parts.Length >= 2)
            {
                checkedTex = parts[0];
                uncheckedTex = parts[1];
            }
            else if (parts.Length == 1)
            {
                checkedTex = parts[0];
                uncheckedTex = parts[0];
            }
        }
        else
        {
            checkedTex = value.Trim();
            uncheckedTex = value.Trim();
        }
    }

    private static int ParseSuffix(string key, string prefix)
    {
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return -1;
        string suffix = key.Substring(prefix.Length);
        if (suffix.Length == 0)
            return -1;
        if (int.TryParse(suffix, out int idx))
            return idx;
        return -1;
    }

    // ------------------------------------------------------------------
    // 绑定
    // ------------------------------------------------------------------

    /// <summary>
    /// 尝试解析并绑定所有尚未绑定的父控件。
    /// 由 Refresh 驱动（Initialize 首次 + 之后每帧，直到全部绑定成功）。
    /// </summary>
    /// <param name="owner">子控件自身（查找从其所在容器开始）。</param>
    public void Bind(XNAControl owner)
    {
        if (owner == null)
            return;

        this.owner = owner;

        bool anyBound = false;
        foreach (var g in groups.Values)
        {
            foreach (var m in g.Members)
            {
                if (m.Control != null)
                {
                    anyBound = true;
                    continue;
                }

                if (string.IsNullOrEmpty(m.Name))
                    continue;

                var found = FindControlByName(owner, m.Name);
                if (found == null)
                    continue;

                m.Control = found;
                Subscribe(found);
                anyBound = true;
            }
        }

        if (anyBound)
            BindingsResolved = true;
    }

    /// <summary>
    /// 在 UI 层级中按名查找父控件，查找优先级：
    /// ① 从 owner 的直接容器开始，沿父层级逐级向上（就近容器优先）；
    /// ② 每级先查该容器的直接子级，再广度优先查其全部后代容器
    ///    （因此父控件即使位于兄弟子面板内也能找到）；
    /// ③ 同级同名取遍历顺序第一个（即控件添加顺序）；跳过 owner 自身。
    /// </summary>
    private XNAControl FindControlByName(XNAControl owner, string name)
    {
        var container = owner.Parent;
        while (container != null)
        {
            var match = FindInContainer(container, owner, name);
            if (match != null)
                return match;
            container = container.Parent;
        }
        return null;
    }

    private static XNAControl FindInContainer(XNAControl container, XNAControl owner, string name)
    {
        var queue = new Queue<XNAControl>(container.Children);
        while (queue.Count > 0)
        {
            var control = queue.Dequeue();
            if (control == owner)
                continue;
            if (control.Name == name)
                return control;
            foreach (var child in control.Children)
                queue.Enqueue(child);
        }
        return null;
    }

    private void Subscribe(XNAControl control)
    {
        switch (control)
        {
            case XNACheckBox checkBox:
                checkBox.CheckedChanged += OnParentStateChanged;
                break;
            case XNADropDown dropDown:
                dropDown.SelectedIndexChanged += OnParentStateChanged;
                break;
        }
    }

    private void Unsubscribe(XNAControl control)
    {
        switch (control)
        {
            case XNACheckBox checkBox:
                checkBox.CheckedChanged -= OnParentStateChanged;
                break;
            case XNADropDown dropDown:
                dropDown.SelectedIndexChanged -= OnParentStateChanged;
                break;
        }
    }

    private void UnsubscribeAll()
    {
        foreach (var g in groups.Values)
        {
            foreach (var m in g.Members)
            {
                if (m.Control == null)
                    continue;

                Unsubscribe(m.Control);
            }
        }
    }

    // ------------------------------------------------------------------
    // 判定与应用（跳变驱动）
    // ------------------------------------------------------------------

    /// <summary>
    /// 计算当前约束状态（纯函数，无副作用）。
    /// 组间 AND（各管各的）：任一组不满足 → 本控件禁用；全部组满足 → 可编辑。
    /// 组内方向由该组的 MatchMode 决定：
    ///   All（默认）= 任一绑定成员满足 → 组满足；
    ///   Any        = 全部绑定成员满足 → 组满足（任一不满足即组不满足）。
    /// 组内全部成员未绑定 → 组不参与锁定倾向（可编辑）。
    /// </summary>
    public ParentControlState Evaluate()
    {
        var state = new ParentControlState
        {
            HasConfiguration = HasConfiguration,
            FirstLockedIndex = -1
        };

        if (!HasConfiguration)
            return state;

        bool anyBound = false;
        bool allGroupsSatisfied = true;

        foreach (var kvp in groups.OrderBy(k => k.Key))
        {
            var g = kvp.Value;
            if (g.Members.Count == 0)
                continue;

            bool anyMemberBound = false;
            bool groupSatisfied;

            if (g.DirectionAll)
            {
                // All 方向（默认）：任一绑定成员满足 → 组满足
                groupSatisfied = false;
                foreach (var m in g.Members)
                {
                    if (m.Control == null)
                        continue;

                    anyMemberBound = true;

                    if (IsSatisfied(m, g))
                    {
                        groupSatisfied = true;
                        break;
                    }
                }
            }
            else
            {
                // Any 方向：全部绑定成员满足 → 组满足（任一不满足即组不满足）
                groupSatisfied = true;
                foreach (var m in g.Members)
                {
                    if (m.Control == null)
                        continue;

                    anyMemberBound = true;

                    if (!IsSatisfied(m, g))
                    {
                        groupSatisfied = false;
                        break;
                    }
                }
            }

            // 组内无可判定成员（全部未绑定）：不锁定倾向
            if (!anyMemberBound)
                groupSatisfied = true;

            if (anyMemberBound)
            {
                anyBound = true;
                if (state.FirstLockedIndex < 0)
                    state.FirstLockedIndex = kvp.Key;
            }

            if (!groupSatisfied)
                allGroupsSatisfied = false;
        }

        state.AnyBound = anyBound;
        state.AllSatisfied = allGroupsSatisfied;
        return state;
    }

    private bool IsSatisfied(ParentBinding m, ParentGroup g)
    {
        switch (m.Control)
        {
            case XNACheckBox checkBox:
            {
                // CheckBox 父：Required 默认 True
                bool required = Conversions.BooleanFromString(GetRequiredRaw(m, g), true);
                return checkBox.Checked == required;
            }

            case XNADropDown dropDown:
            {
                // DropDown 父：组配置了 ParentDropDownValue 时优先用专用键族判定；
                // 未配置时回退 ParentControlRequiredValue（整数=索引、非整数=Tag 匹配）
                if (g.DropDownValueRaw != null)
                    return IsDropDownSatisfiedByKeys(dropDown, g);

                int requiredIndex = ResolveDropDownIndex(dropDown, GetRequiredRaw(m, g));
                return dropDown.SelectedIndex == requiredIndex;
            }
        }

        return true;
    }

    /// <summary>
    /// DropDown 专用键族判定（ParentDropDownMode/Value/Compare）：
    /// <list type="bullet">
    /// <item><c>*</c> 比较方式 → 任意选择即满足</item>
    /// <item><c>Mode=Index</c>（默认）→ Value 解析为整数，按 Compare 与 SelectedIndex 数值比较</item>
    /// <item><c>Mode=Tag</c> / <c>Mode=Text</c> → 选中项的 Tag / 显示文本与 Value 精确比较
    /// （仅支持 == / !=；区间比较对字符串无意义，视为不满足）</item>
    /// </list>
    /// 非法取值/非法组合一律不满足（不静默放行）。
    /// </summary>
    private static bool IsDropDownSatisfiedByKeys(XNADropDown dropDown, ParentGroup g)
    {
        string value = g.DropDownValueRaw?.Trim();
        if (string.IsNullOrEmpty(value))
            return dropDown.SelectedIndex == 0;

        // 比较方式归一化（符号为主，英文单词为兼容别名）；非法 → 不满足
        string op = NormalizeDropDownCompare(g.DropDownCompareRaw);
        if (op == null)
            return false;

        if (op == "*")
            return true;

        // Text 模式：按显示文本比较（仅 == / !=，忽略大小写）
        // Tag 模式：值语义——取选中项的 Tag 字符串（写入 spawn.ini 的值；自定义槽位 →
        // 玩家输入的值）。两侧均可解析为整数时按数值比较（支持全部符号），
        // 否则字符串精确比较（仅 == / !=，区间视为不满足）
        string mode = g.DropDownModeRaw?.Trim();
        bool byTag = string.Equals(mode, "Tag", StringComparison.OrdinalIgnoreCase);
        bool byText = string.Equals(mode, "Text", StringComparison.OrdinalIgnoreCase);

        if (byTag || byText)
        {
            if (byText)
            {
                if (op != "==" && op != "!=")
                    return false; // 区间比较对文本无意义

                bool textEqual = dropDown.SelectedIndex >= 0 && dropDown.SelectedIndex < dropDown.Items.Count &&
                    string.Equals(dropDown.Items[dropDown.SelectedIndex]?.Text, value,
                        StringComparison.OrdinalIgnoreCase);
                return op == "==" ? textEqual : !textEqual;
            }

            string current = dropDown is GameSessionDropDown sessionDropDown
                ? sessionDropDown.GetSelectedValueString()
                : (dropDown.SelectedIndex >= 0 && dropDown.SelectedIndex < dropDown.Items.Count
                    ? dropDown.Items[dropDown.SelectedIndex]?.Tag?.ToString()
                    : null);

            if (int.TryParse(current, out int currentNum) && int.TryParse(value, out int valueNum))
            {
                return op switch
                {
                    "!=" => currentNum != valueNum,
                    ">=" => currentNum >= valueNum,
                    "<=" => currentNum <= valueNum,
                    ">" => currentNum > valueNum,
                    "<" => currentNum < valueNum,
                    _ => currentNum == valueNum, // "=="
                };
            }

            if (op != "==" && op != "!=")
                return false; // 非数字 Tag 不支持区间比较

            bool equal = string.Equals(current, value, StringComparison.Ordinal);
            return op == "==" ? equal : !equal;
        }

        // Index 模式（默认）：按 SelectedIndex（槽位索引）比较
        if (!int.TryParse(value, out int target))
            return false; // 非数字目标值：非法 → 不满足

        int current2 = dropDown.SelectedIndex;
        return op switch
        {
            "!=" => current2 != target,
            ">=" => current2 >= target,
            "<=" => current2 <= target,
            ">" => current2 > target,
            "<" => current2 < target,
            _ => current2 == target, // "=="
        };
    }

    /// <summary>
    /// 归一化 ParentDropDownCompare 取值：
    /// 符号为主：<c>==</c>（默认，<c>=</c> 等价）/ <c>!=</c> / <c>&gt;</c> / <c>&gt;=</c> /
    /// <c>&lt;</c> / <c>&lt;=</c> / <c>*</c>（任意选择即满足）；
    /// 英文单词保留为兼容别名（Equals/NotEquals/Greater/GreaterOrEqual/Less/LessOrEqual/Any）。
    /// 非法取值返回 null（调用方视为不满足）。
    /// </summary>
    private static string NormalizeDropDownCompare(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "==";

        switch (raw.Trim())
        {
            case "==":
            case "=":
                return "==";
            case "!=":
                return "!=";
            case ">":
                return ">";
            case ">=":
                return ">=";
            case "<":
                return "<";
            case "<=":
                return "<=";
            case "*":
                return "*";
        }

        string v = raw.Trim();
        if (string.Equals(v, "Any", StringComparison.OrdinalIgnoreCase)) return "*";
        if (string.Equals(v, "Equals", StringComparison.OrdinalIgnoreCase)) return "==";
        if (string.Equals(v, "NotEquals", StringComparison.OrdinalIgnoreCase)) return "!=";
        if (string.Equals(v, "Greater", StringComparison.OrdinalIgnoreCase)) return ">";
        if (string.Equals(v, "GreaterOrEqual", StringComparison.OrdinalIgnoreCase)) return ">=";
        if (string.Equals(v, "Less", StringComparison.OrdinalIgnoreCase)) return "<";
        if (string.Equals(v, "LessOrEqual", StringComparison.OrdinalIgnoreCase)) return "<=";

        return null;
    }

    private string GetRequiredRaw(ParentBinding m, ParentGroup g)
        => m.RequiredValueRaw ?? g.RequiredValueRaw ?? commonRequiredValueRaw;

    /// <summary>
    /// 将 DropDown 的原始字符串解析为项索引：
    /// 整数 → 直接作为索引；非整数 → 按项 Tag 精确匹配；未配置/未匹配 → 0。
    /// </summary>
    private static int ResolveDropDownIndex(XNADropDown dropDown, string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        string trimmed = raw.Trim();
        if (int.TryParse(trimmed, out int idx))
            return idx;

        for (int i = 0; i < dropDown.Items.Count; i++)
        {
            string tag = dropDown.Items[i]?.Tag?.ToString();
            if (string.Equals(tag, trimmed, StringComparison.Ordinal))
                return i;
        }

        return 0;
    }

    /// <summary>
    /// 每帧驱动（子控件 Update 中调用）：
    /// 重试绑定（只要仍有父控件未解析就持续尝试，直到全部绑定成功）+
    /// 检测锁定状态跳变。无父控件配置时为廉价空操作。
    /// </summary>
    public void Refresh(XNAControl owner)
    {
        if (!HasConfiguration)
            return;

        if (owner != null)
            this.owner = owner;

        // 仍有未解析的父控件：持续重试（INI 重解析清空绑定后也会走到这里）
        if (this.owner?.Parent != null &&
            groups.Values.SelectMany(g => g.Members)
                .Any(m => m.Control == null && !string.IsNullOrEmpty(m.Name)))
            Bind(this.owner);

        ApplyIfTransitioned();
    }

    /// <summary>
    /// 跳变检测与应用：仅当 可编辑⇄锁定 状态变化，
    /// 或 INI 配置被重新解析（appliedStateDirty）时，调用适配器一次。
    /// 此外，锁定态每帧重申一次：游戏结束返回大厅等宿主刷新会重置
    /// 子控件的禁用外观/交互（不经过引擎），持续重申以保持锁定显示；
    /// 可编辑态仅在跳变时应用一次，不与宿主代码（如非房主 AllowChanges=false）
    /// 发生每帧对抗。
    /// </summary>
    private void ApplyIfTransitioned()
    {
        if (adapter == null || !HasConfiguration)
            return;

        var state = Evaluate();
        if (!state.AnyBound)
            return;

        if (appliedStateDirty || state.AllSatisfied != lastAppliedSatisfied)
        {
            appliedStateDirty = false;
            lastAppliedSatisfied = state.AllSatisfied;

            if (state.AllSatisfied)
                adapter.OnUnlocked();
            else
                adapter.OnLocked(this, state.FirstLockedIndex);
            return;
        }

        // 锁定态每帧重申（对抗宿主刷新导致的禁用显示丢失）
        if (!state.AllSatisfied)
            adapter.OnLocked(this, state.FirstLockedIndex);
    }

    private void OnParentStateChanged(object sender, EventArgs e)
        => ApplyIfTransitioned();

    // ------------------------------------------------------------------
    // 锁定显示值解析（适配器 OnLocked 中按需调用；索引参数为组号）
    // ------------------------------------------------------------------

    /// <summary>
    /// CheckBox 子项锁定时应显示的选中状态。
    /// 取值优先级：组内首个成员覆盖 → 组级值 → 通用值，默认 false。
    /// </summary>
    public bool GetLockedCheckedDisplay(int firstLockedIndex)
    {
        string raw = ResolveGroupValue(firstLockedIndex, g => g.LockedValueRaw, m => m.LockedValueRaw);
        raw ??= commonLockedValueRaw;
        return Conversions.BooleanFromString(raw, false);
    }

    /// <summary>
    /// DropDown 子项锁定时应显示的选中索引。
    /// 未配置 LockedValue 时返回 false（保持当前选择）。
    /// </summary>
    public bool TryGetLockedDropDownIndex(XNADropDown child, int firstLockedIndex, out int index)
    {
        index = -1;
        if (child == null)
            return false;

        string raw = ResolveGroupValue(firstLockedIndex, g => g.LockedValueRaw, m => m.LockedValueRaw);
        raw ??= commonLockedValueRaw;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        index = ResolveDropDownIndex(child, raw);
        return true;
    }

    /// <summary>
    /// CheckBox 子项锁定时应使用的禁用纹理名（按锁定显示状态选择选中/未选纹理）。
    /// 无任何纹理配置时返回 null（适配器应还原默认禁用纹理）。
    /// 取值优先级：组内首个成员覆盖 → 组级值 → 通用纹理。
    /// </summary>
    public string GetLockedTextureName(int firstLockedIndex)
    {
        string checkedTex = ResolveGroupValue(firstLockedIndex,
            g => g.TextureCheckedRaw, m => m.TextureCheckedRaw) ?? commonTextureCheckedRaw;
        string uncheckedTex = ResolveGroupValue(firstLockedIndex,
            g => g.TextureUncheckedRaw, m => m.TextureUncheckedRaw) ?? commonTextureUncheckedRaw;

        if (string.IsNullOrEmpty(checkedTex) && string.IsNullOrEmpty(uncheckedTex))
            return null;

        bool displayChecked = GetLockedCheckedDisplay(firstLockedIndex);
        return displayChecked
            ? (checkedTex ?? uncheckedTex)
            : (uncheckedTex ?? checkedTex);
    }

    /// <summary>
    /// 解析指定组的显示值：组内首个成员的覆盖优先，其次组级值。
    /// 组不存在或无配置时返回 null。
    /// </summary>
    private string ResolveGroupValue(
        int groupIndex,
        Func<ParentGroup, string> groupValue,
        Func<ParentBinding, string> memberValue)
    {
        if (!groups.TryGetValue(groupIndex, out var g))
            return null;

        // 组内首个绑定成员的覆盖，其次首个成员，最后组级值
        var member = g.Members.FirstOrDefault(m => m.Control != null)
                     ?? g.Members.FirstOrDefault();
        string raw = member != null ? memberValue(member) : null;
        return raw ?? groupValue(g);
    }
}
