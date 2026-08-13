using System.Globalization;

namespace SeedSearchPrototype;

/// <summary>
/// UI-level localization for the mod. Game entity names come from the
/// official resource pack at runtime; only mod-specific chrome text lives in
/// this class, because the game has no source for those strings.
/// </summary>
public static class Localization
{
    private static bool _isChinese;

    public static bool IsChinese => _isChinese;

    public static string LanguageCode { get; private set; } = "eng";

    public static Func<string, string, string>? NameResolver { get; set; }

    public static Action<string>? LoadLanguage { get; set; }

    private static readonly Dictionary<string, string> ChineseUi =
        new(StringComparer.Ordinal)
        {
            ["Seed Search"] = "种子搜索",
            ["Open the seed search board"] = "打开种子搜索面板",
            ["Search TheSpire"] = "Search TheSpire",
            ["Close"] = "关闭",
            ["Board"] = "开局",
            ["Show search board"] = "显示搜索面板",
            ["Popular"] = "热门",
            ["Show popular searches"] = "显示热门搜索",
            ["Saved"] = "已保存",
            ["Show saved searches"] = "显示已保存搜索",
            ["searching"] = "搜索分支",
            ["share"] = "分享",
            ["Copy a shareable search spec"] = "复制可分享的搜索规格",
            ["clear"] = "清空",
            ["Clear the board"] = "清空面板",
            ["compose the run start you want"] = "组合你想要的初始局面",
            ["empty slots match anything"] = "空槽位匹配任意内容",
            ["bulk search · reference RNG projection · Inspect uses game runtime when available"] =
                "批量搜索 · 参考 RNG 投影 · 可用时 Inspect 使用游戏运行时",
            ["any Neow offers"] = "任意涅奥奖励",
            ["Open Neow offer picker"] = "打开涅奥奖励选择器",
            ["any character"] = "任意角色",
            ["other character"] = "其他角色",
            ["common"] = "普通",
            ["uncommon"] = "罕见",
            ["rare"] = "稀有",
            ["shop"] = "商店",
            ["Shop"] = "商店",
            ["Monster"] = "怪物",
            ["Elite"] = "精英",
            ["Rest site"] = "休息点",
            ["Treasure"] = "宝箱",
            ["Unknown"] = "未知",
            ["Ascension 0"] = "进阶 0",
            ["Ascension 1"] = "进阶 1",
            ["Ascension 2"] = "进阶 2",
            ["Ascension 3"] = "进阶 3",
            ["Ascension 4"] = "进阶 4",
            ["Ascension 5"] = "进阶 5",
            ["Ascension 6"] = "进阶 6",
            ["Ascension 7"] = "进阶 7",
            ["Ascension 8"] = "进阶 8",
            ["Ascension 9"] = "进阶 9",
            ["Ascension 10"] = "进阶 10",
            ["plain run"] = "普通模式",
            ["0+ elites"] = "精英 0+",
            ["1+ elites"] = "精英 1+",
            ["2+ elites"] = "精英 2+",
            ["3+ elites"] = "精英 3+",
            ["0+ shops"] = "商店 0+",
            ["1+ shops"] = "商店 1+",
            ["2+ shops"] = "商店 2+",
            ["0+ rest sites"] = "休息点 0+",
            ["1+ rest sites"] = "休息点 1+",
            ["2+ rest sites"] = "休息点 2+",
            ["3+ rest sites"] = "休息点 3+",
            ["Any"] = "任意",
            ["Ancient A"] = "先古 A",
            ["Ancient B"] = "先古 B",
            ["Ancient C"] = "先古 C",
            ["Ancient D"] = "先古 D",
            ["Boss 1"] = "首领 1",
            ["Boss 2"] = "首领 2",
            ["Boss 3"] = "首领 3",
            ["Neow"] = "涅奥",
            ["Character"] = "角色",
            ["Ascension"] = "进阶",
            ["Run mode"] = "模式",
            ["Act 1 elites"] = "第一幕精英",
            ["Act 1 shops"] = "第一幕商店",
            ["Act 1 rests"] = "第一幕休息点",
            ["Ancient"] = "先古",
            ["Boss"] = "首领",
            ["run layout & drop pins"] = "路线布局与掉落锚点",
            ["bosses, ancients, rewards, shops, relic bags and events use the same nested picker model"] =
                "首领、先古、奖励、商店、遗物袋和事件都使用相同的嵌套选择器模型",
            ["search controls"] = "搜索控制",
            ["5 matches"] = "5 个结果",
            ["10 matches"] = "10 个结果",
            ["20 matches"] = "20 个结果",
            ["50 matches"] = "50 个结果",
            ["10M candidates"] = "1000 万候选",
            ["100M candidates"] = "1 亿候选",
            ["200M candidates"] = "2 亿候选",
            ["custom"] = "自定义",
            ["custom runs"] = "自定义候选数",
            ["e.g. 30000000 or 30M"] = "例如 30000000 或 30M",
            ["stop after"] = "最多结果",
            ["runs to search"] = "搜索候选数",
            ["advanced offset"] = "高级偏移",
            ["random start"] = "随机起点",
            ["start from a random offset away from the seed space edges"] =
                "从远离种子空间首尾的随机位置开始搜索",
            ["Search"] = "搜索",
            ["Search seeds"] = "搜索种子",
            ["Cancel"] = "取消",
            ["Cancel the running search"] = "取消正在进行的搜索",
            ["ready"] = "就绪",
            ["inspect a seed"] = "检查种子",
            ["preview any seed's run start, no search needed"] = "预览任意种子的开局，无需搜索",
            ["paste a seed…"] = "粘贴种子…",
            ["Inspect"] = "检查",
            ["Inspect this seed"] = "检查该种子",
            ["results"] = "结果",
            ["0 matches"] = "0 个结果",
            ["show all spoilers"] = "显示全部剧透",
            ["Toggle result detail"] = "切换结果详情",
            ["hide spoilers"] = "隐藏剧透",
            ["seed"] = "种子",
            ["act 1 map"] = "第一幕地图",
            ["neow offers"] = "涅奥奖励",
            ["ancients"] = "先古",
            ["bosses"] = "首领",
            ["results show up here. select some filters, then hit Search."] =
                "结果会显示在这里。选择筛选条件后点击搜索。",
            ["save this search"] = "保存此搜索",
            ["Save this search in the mod"] = "在本 Mod 中保存此搜索",
            ["copy random seed"] = "复制随机种子",
            ["Copy a random seed from the current results"] = "从当前结果中复制一个随机种子",
            ["Popular this week"] = "本周热门",
            ["popular searches are ranked from searches saved in this game session and can be reopened directly."] =
                "热门搜索按本局游戏会话中保存的搜索排名，可直接重新打开。",
            ["Saved in this mod"] = "本 Mod 中的已保存",
            ["saved searches remain available while this game session is open."] =
                "游戏会话打开期间，保存的搜索保持可用。",
            ["searching…"] = "搜索中…",
            ["search cancelled"] = "搜索已取消",
            ["search failed; check godot.log"] = "搜索失败；请查看 godot.log",
            ["cancelling…"] = "取消中…",
            ["paste a seed first"] = "请先粘贴种子",
            ["search complete"] = "搜索完成",
            ["no seeds matched. remove or loosen a filter and search again."] =
                "没有匹配的种子。移除或放宽一个筛选条件后重新搜索。",
            ["stopped at the match cap"] = "已到达结果上限",
            ["game runtime"] = "游戏运行时",
            ["reference RNG"] = "参考 RNG",
            ["Copy seed"] = "复制种子",
            ["Expand this seed's preview"] = "展开该种子的预览",
            ["details"] = "详情",
            ["hide details"] = "隐藏详情",
            ["seed preview"] = "种子预览",
            ["copy seed"] = "复制种子",
            ["Copy this seed"] = "复制该种子",
            ["start run"] = "一键开局",
            ["Start this seed in custom mode"] = "以该种子和所选角色在自定义模式开局",
            ["already in a run"] = "已在局内",
            ["could not start the run"] = "无法开始本局",
            ["any"] = "任意",
            ["clear this slot"] = "清空此槽位",
            ["Leave this constraint open."] = "保持此约束开放。",
            ["extended Neow options"] = "扩展涅奥选项",
            ["click a slot to narrow what this relic grants; grouped slots share one reward roll"] =
                "点击槽位可缩小该遗物给予的内容；分组槽位共享一次奖励掷骰",
            ["fresh reward rares"] = "新奖励稀有牌",
            ["choose a character first"] = "请先选择角色",
            ["this child slot needs a character-specific card pool"] = "该子槽位需要角色专属卡牌池",
            ["this slot needs a character-specific card pool"] = "该槽位需要角色专属卡牌池",
            ["choose a fresh-reward Neow offer"] = "请选择新奖励涅奥选项",
            ["add a reward card first"] = "请先添加奖励卡牌",
            ["select its parent first"] = "请先选择其父项",
            ["run a search before saving it"] = "保存前请先运行一次搜索",
            ["saved this search"] = "已保存此搜索",
            ["search spec copied"] = "搜索规格已复制",
            ["nothing saved yet. run a search, then hit save this search."] =
                "尚未保存任何内容。运行一次搜索，然后点击“保存此搜索”。",
            ["open"] = "打开",
            ["Open saved results"] = "打开已保存结果",
            ["Open this popular search"] = "打开此热门搜索",
            ["nothing popular yet. save a search first."] = "暂无热门内容。请先保存一次搜索。",
            ["any run start"] = "任意开局",
            ["public beta · v0.110.1"] = "公开测试 · v0.110.1",
            ["English"] = "English",
            ["中文"] = "中文",
            ["Switch language"] = "切换语言",
            ["no matching options"] = "没有匹配的选项",
            ["search…"] = "搜索…",
            ["pick"] = "选择",
            ["Collapse or expand this option group"] = "折叠或展开此选项组",
            ["this option is not available for the current query"] = "该选项在当前查询中不可用",
            ["Close picker"] = "关闭选择器",
            ["has blessing"] = "有祝福",
            ["has curse"] = "有诅咒",
            ["Act 1 map"] = "第一幕地图",
            ["Neow offer"] = "涅奥奖励",
            ["grants"] = "给予",
            ["and grants"] = "并给予",
            ["curse"] = "诅咒",
            ["offers rare"] = "提供稀有牌",
            ["the rare is"] = "稀有牌是",
            ["offers"] = "提供",
            ["offers card"] = "提供卡牌",
            ["potion"] = "药水",
            ["capsule pulls include"] = "扭蛋抽取包含",
            ["and includes"] = "并包含",
            ["and offers card"] = "并提供卡牌",
            ["a bundle contains"] = "卷轴箱包含",
            ["and contains"] = "并包含",
            ["transforming a Basic gives"] = "转化基础牌得到",
            ["poultice gives"] = "药膏给予",
            ["and also gives"] = "并给予",
            ["pulls"] = "抽取",
            ["and pulls"] = "并抽取",
            ["grants potion"] = "给予药水",
            ["and potion"] = "并给予药水",
            ["grants relic"] = "给予遗物",
            ["Act 1 boss"] = "第一幕首领",
            ["Act 2 boss"] = "第二幕首领",
            ["Act 3 boss"] = "第三幕首领",
            ["Act 3 second boss (A10)"] = "第三幕第二首领（A10）",
            ["Act 2 ancient"] = "第二幕先古",
            ["Act 3 ancient"] = "第三幕先古",
            ["reward window"] = "奖励窗口",
            ["reward order"] = "奖励顺序",
            ["rewards have"] = "奖励包含",
            ["and"] = "并",
            ["fight 1 reward has"] = "战斗 1 奖励包含",
            ["fight 2 reward has"] = "战斗 2 奖励包含",
            ["fight 3 reward has"] = "战斗 3 奖励包含",
            ["shop window"] = "商店窗口",
            ["shop relic slot has"] = "商店遗物槽包含",
            ["relic reward window"] = "遗物奖励窗口",
            ["relic rewards have"] = "遗物奖励包含",
            ["event window"] = "事件窗口",
            ["act 1 event"] = "第一幕事件",
            ["cursed offer"] = "诅咒奖励",
            ["bonus offer"] = "额外奖励",
            ["relic grants"] = "遗物给予",
            ["Overgrowth bosses"] = "密林首领",
            ["Underdocks bosses"] = "暗港首领",
            ["Act 2 bosses"] = "第二幕首领",
            ["Act 3 bosses"] = "第三幕首领",
            ["Act 2 ancients"] = "第二幕先古",
            ["Act 3 ancients"] = "第三幕先古",
            ["ancient offers"] = "先古奖励",
            ["Ironclad cards"] = "铁甲战士卡牌",
            ["Silent cards"] = "静默猎手卡牌",
            ["Regent cards"] = "储君卡牌",
            ["Defect cards"] = "故障机器人卡牌",
            ["Necrobinder cards"] = "亡灵契约师卡牌",
            ["cards"] = "卡牌",
            ["potions"] = "药水",
            ["relic rewards"] = "遗物奖励",
            ["shared potions"] = "通用药水",
            ["offers if you qualify"] = "满足条件时提供",
            ["offers on any run"] = "任意路线都会提供",
            ["any order"] = "任意顺序",
            ["in this order"] = "按此顺序",
            ["reward 1 all rare"] = "奖励 1 全稀有",
            ["both all rare"] = "两者全稀有",
            ["the two grants must be distinct"] = "两个给予必须不同",
            ["the two Act 3 bosses must be distinct"] = "两个第三幕首领必须不同",
            ["event picks must be distinct"] = "事件选择必须不同",
            ["repeated relic picks must be distinct"] = "重复遗物选择必须不同",
            ["this boss belongs to the other Act 1 map"] = "该首领属于另一个第一幕地图",
            ["this map contradicts a pinned boss or event"] = "该地图与已固定的首领或事件冲突",
            ["these grouped picks must be distinct"] = "这些分组选择必须不同",
            ["choose a character before narrowing card results"] = "先选择角色再缩小卡牌结果",
            ["Two relic grants with follow-up details."] = "两次遗物给予，并附带后续详情。",
            ["if you still have your starter relic"] = "如果你仍持有初始遗物",
            ["the starter card Archaic Tooth transforms is still in your deck"] =
                "起始牌「远古之牙」转化的牌仍在你的牌组中",
            ["≥3 Goopy-enchantable cards in your deck"] = "牌组中至少 3 张可附魔「黏糊」的牌",
            ["≥5 removable cards in your deck"] = "牌组中至少 5 张可移除的牌",
            ["you have no event pet"] = "你没有事件宠物",
            ["a Basic Strike remains in your deck"] = "牌组中仍有一张基础打击",
            ["≥3 Instinct-enchantable cards in your deck"] = "牌组中至少 3 张可附魔「本能」的牌",
            ["≥4 Swift-enchantable cards in your deck"] = "牌组中至少 4 张可附魔「迅捷」的牌",
            ["if you don't already have a pet"] = "如果你还没有宠物",
            ["if 120+ gold"] = "如果金币不低于 120",
            ["if enough gold and at least one undiscovered relic"] = "如果金币充足且至少有一件未发现遗物",
            ["if 100+ gold and 2+ transformable cards"] = "如果金币不低于 100 且有 2 张以上可转化卡牌",
            ["if floor 6+"] = "如果第 6 层及以上",
            ["if floor 7+ and a removable card"] = "如果第 7 层及以上且有一张可移除卡牌",
            ["if a card that can take the Spiral enchantment"] = "如果有一张可附魔「螺旋」的卡牌",
            ["if 150+ gold"] = "如果金币不低于 150",
            ["if 2+ potions held"] = "如果持有 2 瓶以上药水",
            ["if 10+ HP"] = "如果生命值不低于 10",
            ["if more than 5 HP"] = "如果生命值超过 5",
            ["if HP at or below 70% of max"] = "如果生命值不高于最大值的 70%",
            ["if 55+ gold"] = "如果金币不低于 55",
            ["if 44+ gold"] = "如果金币不低于 44",
            ["if a removable basic card"] = "如果有一张可移除的基础牌",
            ["A{0} only"] = "仅 A{0}",
            ["Open {0} picker"] = "打开 {0} 选择器",
            ["copied {0}"] = "已复制 {0}",
            ["starting {0}"] = "正在以 {0} 开局",
            ["{0} matches"] = "{0} 个结果",
            ["searched {0} candidates"] = "已搜索 {0} 个候选",
            ["searching · {0} candidates checked"] = "搜索中 · 已检查 {0} 个候选",
            ["inspected {0} · game runtime"] = "已检查 {0} · 游戏运行时",
            ["inspected {0} · reference engine"] = "已检查 {0} · 参考引擎",
            ["inspected {0} · {1}"] = "已检查 {0} · {1}",
            ["backend · {0}"] = "后端 · {0}",
            ["Act 1 path · {0}"] = "第一幕路线 · {0}",
            ["Neow detail · {0}"] = "涅奥详情 · {0}",
            ["Ancients · {0}    Bosses · {1}"] = "先古 · {0}    首领 · {1}",
            ["early route counts · {0} elites · {1} shops · {2} rest sites"] =
                "前期路线统计 · {0} 精英 · {1} 商店 · {2} 休息点",
            ["extended pins · {0}"] = "扩展锚点 · {0}",
            ["{0} matches · {1} · offset {2}"] = "{0} 个结果 · {1} · 偏移 {2}",
            ["{0} searches · {1}"] = "{0} 个搜索 · {1}",
            ["{0} search · {1}"] = "{0} 个搜索 · {1}",
            ["first {0} roll rare"] = "前 {0} 次掷骰稀有",
            ["within {0}"] = "在 {0} 内",
            ["{0} cards · {1}"] = "{0} 卡牌 · {1}",
            ["{0} potions"] = "{0} 药水",
            ["{0} — picks the character"] = "{0} — 会锁定角色",
            ["{0} relics · {1}"] = "{0} 遗物 · {1}",
            ["if {0}"] = "如果 {0}",
        };

    public static void Initialize(string? languageCode)
    {
        LanguageCode = string.IsNullOrWhiteSpace(languageCode)
            ? "eng"
            : languageCode.Trim().ToLowerInvariant();
        SetChinese(LanguageCode is "zhs" or "zht", LanguageCode);
    }

    public static void SetChinese(bool enabled, string? language = null)
    {
        _isChinese = enabled;
        if (enabled)
        {
            var folder = string.IsNullOrWhiteSpace(language)
                ? LanguageCode is "zht" ? "zht" : "zhs"
                : language;
            LoadLanguage?.Invoke(folder);
        }
    }

    public static void ToggleLanguage() => SetChinese(!IsChinese);

    public static string T(string text) =>
        IsChinese && ChineseUi.TryGetValue(text, out var translated) ? translated : text;

    public static string F(string format, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(format), args);

    public static string Game(string id, string fallback) =>
        IsChinese && NameResolver != null ? NameResolver(id, fallback) : fallback;

    public static string OptionTitle(SearchTheSpireOption option)
    {
        if (string.IsNullOrEmpty(option.Id))
        {
            return T(option.Title);
        }

        if (option.Section == "Act 1 map" && option.Id is "0" or "1")
        {
            return Game(option.Id == "0" ? "overgrowth" : "underdocks", option.Title);
        }

        var localized = Game(option.Id, T(option.Title));
        if (IsChinese)
        {
            if (option.Title.StartsWith("within ", StringComparison.Ordinal))
            {
                return F("within {0}", option.Title["within ".Length..]);
            }

            if (option.Title.StartsWith("first ", StringComparison.Ordinal) &&
                option.Title.EndsWith(" roll rare", StringComparison.Ordinal))
            {
                return F(
                    "first {0} roll rare",
                    option.Title["first ".Length..^" roll rare".Length]);
            }

            if (option.Title.EndsWith("(Overgrowth)", StringComparison.OrdinalIgnoreCase))
            {
                return $"{localized}（密林）";
            }

            if (option.Title.EndsWith("(Underdocks)", StringComparison.OrdinalIgnoreCase))
            {
                return $"{localized}（暗港）";
            }
        }

        return localized;
    }

    public static string OptionSection(SearchTheSpireOption option)
    {
        var section = option.Section;
        if (string.IsNullOrEmpty(section) || option.OwnerCharacter == null)
        {
            return T(section);
        }

        var owner = CharacterNameFromOwner(option.OwnerCharacter);
        var rarity = option.Rarity;
        if (section.Contains(" cards · ", StringComparison.Ordinal))
        {
            return F("{0} cards · {1}", owner, RarityName(rarity ?? "common"));
        }

        if (section.Contains(" relics · ", StringComparison.Ordinal))
        {
            return F("{0} relics · {1}", owner, RarityName(rarity ?? "common"));
        }

        if (section.Contains(" — picks the character", StringComparison.Ordinal))
        {
            return F("{0} — picks the character", owner);
        }

        if (section.EndsWith(" potions", StringComparison.Ordinal))
        {
            return F("{0} potions", owner);
        }

        return T(section);
    }

    public static string OptionDescription(SearchTheSpireOption option) =>
        T(option.Description ?? option.Section);

    public static string OptionBlockReason(SearchTheSpireOption option) =>
        T(option.BlockReason ?? "this option is not available for the current query");

    public static string CharacterName(RunCharacter character) => character switch
    {
        RunCharacter.Any => T("any character"),
        RunCharacter.Ironclad => Game("ironclad", "Ironclad"),
        RunCharacter.Silent => Game("silent", "Silent"),
        RunCharacter.Regent => Game("regent", "Regent"),
        RunCharacter.Defect => Game("defect", "Defect"),
        RunCharacter.Necrobinder => Game("necrobinder", "Necrobinder"),
        _ => character.ToString(),
    };

    public static string NeowFilterName(NeowFilter filter) => filter switch
    {
        NeowFilter.HasBlessing => T("has blessing"),
        NeowFilter.HasCurse => T("has curse"),
        _ => T("Any"),
    };

    private static string CharacterNameFromOwner(string owner) =>
        Enum.TryParse<RunCharacter>(owner, true, out var character)
            ? CharacterName(character)
            : T(owner);

    private static string RarityName(string rarity) => T(rarity.ToLowerInvariant());
}
