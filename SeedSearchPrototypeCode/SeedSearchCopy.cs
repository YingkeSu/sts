namespace SeedSearchPrototype;

public enum SeedSearchLanguage
{
    Chinese,
    English,
}

/// <summary>
/// Display-boundary copy for the overlay and picker.  The search model keeps
/// stable English ids and spec keys; only the rendered copy is translated.
/// That prevents a language switch from changing a saved query or the seed
/// search semantics.
/// </summary>
public static class SeedSearchCopy
{
    private static readonly IReadOnlyDictionary<string, string> English =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["launcher.title"] = "Seed Search",
            ["launcher.tooltip"] = "Open the seed search board",
            ["app.title"] = "Search TheSpire",
            ["beta"] = "public beta · v0.110.1",
            ["close"] = "Close",
            ["board"] = "Board",
            ["board.tooltip"] = "Show search board",
            ["popular"] = "Popular",
            ["popular.tooltip"] = "Show popular searches",
            ["saved"] = "Saved",
            ["saved.tooltip"] = "Show saved searches",
            ["language"] = "Language",
            ["language.chinese"] = "中文",
            ["language.english"] = "English",
            ["searching"] = "searching",
            ["branch.publicBeta"] = "public beta · v0.110.1",
            ["share"] = "share",
            ["share.tooltip"] = "Copy a shareable search spec",
            ["clear"] = "clear",
            ["clear.tooltip"] = "Clear the board",
            ["compose"] = "compose the run start you want",
            ["emptySlots"] = "empty slots match anything",
            ["dataNote"] = "bulk search · reference RNG projection · Inspect uses game runtime when available",
            ["neow"] = "Neow",
            ["neow.any"] = "any Neow offers",
            ["neow.tooltip"] = "Open Neow offer picker",
            ["character"] = "Character",
            ["character.any"] = "any character",
            ["character.tooltip"] = "Choose a character; click it again to clear",
            ["setup"] = "run setup",
            ["ascension"] = "Ascension",
            ["ascension.none"] = "none",
            ["ascension.level"] = "A{0}",
            ["runMode"] = "Run mode",
            ["runMode.plain"] = "plain run",
            ["act1.elites"] = "Act 1 elites",
            ["act1.shops"] = "Act 1 shops",
            ["act1.rests"] = "Act 1 rests",
            ["minimum.elites"] = "{0}+ elites",
            ["minimum.shops"] = "{0}+ shops",
            ["minimum.rests"] = "{0}+ rest sites",
            ["ancient"] = "Ancient",
            ["ancient.a"] = "Ancient A",
            ["ancient.b"] = "Ancient B",
            ["ancient.c"] = "Ancient C",
            ["ancient.d"] = "Ancient D",
            ["boss"] = "Boss",
            ["boss.1"] = "Boss 1",
            ["boss.2"] = "Boss 2",
            ["boss.3"] = "Boss 3",
            ["any"] = "Any",
            ["runLayout"] = "run layout & drop pins",
            ["runLayout.note"] = "bosses, ancients, rewards, shops, relic bags and events use the same nested picker model",
            ["relics.shop"] = "from a shop",
            ["relics.reward"] = "as a reward",
            ["rewards.caption"] = "cards that must all appear within the first N fight rewards",
            ["relics.shop.caption"] = "the multi-pick shop relic slots",
            ["relics.reward.caption"] = "the top of each rarity's relic reward deque",
            ["events.caption"] = "Act 1 ? rooms use the seed-determined event queue",
            ["rewards.exact"] = "exact fight",
            ["advanced.show"] = "show details",
            ["advanced.hide"] = "hide details",
            ["advanced.empty"] = "choose a pin to reveal its follow-up options",
            ["neow.details"] = "extended Neow options",
            ["neow.details.note"] = "click a slot to narrow what this offer grants; grouped slots share one reward roll",
            ["neow.freshRares"] = "fresh reward rares",
            ["picker.pick"] = "pick",
            ["picker.search"] = "search…",
            ["picker.close"] = "Close picker",
            ["picker.clear"] = "any",
            ["picker.noMatch"] = "no matching options",
            ["picker.collapse"] = "Collapse or expand this option group",
            ["searchControls"] = "search controls",
            ["stopAfter"] = "stop after",
            ["runsToSearch"] = "runs to search",
            ["advancedOffset"] = "advanced offset",
            ["matches5"] = "5 matches",
            ["matches10"] = "10 matches",
            ["matches20"] = "20 matches",
            ["matches50"] = "50 matches",
            ["candidates10k"] = "10k candidates",
            ["candidates50k"] = "50k candidates",
            ["candidates250k"] = "250k candidates",
            ["candidates1m"] = "1m candidates",
            ["search"] = "Search",
            ["search.tooltip"] = "Search seeds",
            ["cancel"] = "Cancel",
            ["cancel.tooltip"] = "Cancel the running search",
            ["ready"] = "ready",
            ["search.complete"] = "search complete",
            ["search.cancelling"] = "cancelling…",
            ["inspect.title"] = "inspect a seed",
            ["inspect.note"] = "preview any seed's run start, no search needed",
            ["inspect.placeholder"] = "paste a seed…",
            ["inspect"] = "Inspect",
            ["inspect.tooltip"] = "Inspect this seed",
            ["results"] = "results",
            ["matches.zero"] = "0 matches",
            ["spoilers.show"] = "show all spoilers",
            ["spoilers.hide"] = "hide spoilers",
            ["spoilers.tooltip"] = "Toggle result detail",
            ["header.seed"] = "seed",
            ["header.map"] = "act 1 map",
            ["header.neow"] = "neow offers",
            ["header.ancients"] = "ancients",
            ["header.bosses"] = "bosses",
            ["status.initial"] = "results show up here. select some filters, then hit Search.",
            ["status.searching"] = "searching…",
            ["status.cancelled"] = "search cancelled",
            ["status.failed"] = "search failed; check godot.log",
            ["status.noMatches"] = "no seeds matched. remove or loosen a filter and search again.",
            ["status.matchCap"] = "stopped at the match cap",
            ["status.saved"] = "saved this search",
            ["status.saveFirst"] = "run a search before saving it",
            ["status.specCopied"] = "search spec copied",
            ["status.seedCopied"] = "copied {0}",
            ["status.seedInspected"] = "inspected {0} · {1}",
            ["status.pasteSeed"] = "paste a seed first",
            ["save"] = "save this search",
            ["save.tooltip"] = "Save this search in the mod",
            ["random"] = "copy random seed",
            ["random.tooltip"] = "Copy a seed from the current branch",
            ["popular.title"] = "Popular this week",
            ["popular.note"] = "popular searches are ranked from searches saved in this game session and can be reopened directly.",
            ["saved.title"] = "Saved in this mod",
            ["saved.note"] = "saved searches remain available while this game session is open.",
            ["saved.empty"] = "nothing saved yet. run a search, then hit save this search.",
            ["popular.empty"] = "nothing popular yet. save a search first.",
            ["open"] = "open",
            ["open.saved.tooltip"] = "Open saved results",
            ["open.popular.tooltip"] = "Open this popular search",
            ["result.preview"] = "seed preview",
            ["result.backend"] = "backend · {0}",
            ["result.map"] = "Act 1 path · {0}",
            ["result.neow"] = "Neow detail · {0}",
            ["result.ancientsBosses"] = "Ancients · {0}    Bosses · {1}",
            ["result.route"] = "early route counts · {0} elites · {1} shops · {2} rest sites",
            ["result.pins"] = "extended pins · {0}",
            ["result.details"] = "details",
            ["result.details.hide"] = "hide details",
            ["result.details.tooltip"] = "Expand this seed's preview",
            ["result.copy"] = "copy seed",
            ["result.copy.tooltip"] = "Copy this seed",
            ["saved.row"] = "{0} matches · {1} · offset {2}",
            ["popular.row.single"] = "search",
            ["popular.row.plural"] = "searches",
            ["any.option"] = "any",
            ["blocked.character"] = "choose a character first",
            ["blocked.character.tooltip"] = "this slot needs a character-specific card pool",
            ["blocked.ascension"] = "A{0} only",
            ["blocked.neow"] = "choose a fresh-reward Neow offer",
            ["blocked.reward"] = "add a reward card first",
            ["blocked.parent"] = "select its parent first",
            ["backend.game"] = "game runtime",
            ["backend.reference"] = "reference RNG",
            ["backend.unknown"] = "{0}",
            ["common.none"] = "none",
            ["common.anyRunStart"] = "any run start",
            ["common.within"] = "within {0}",
            ["common.anyOrder"] = "any order",
            ["common.inOrder"] = "in this order",
        };

    private static readonly IReadOnlyDictionary<string, string> Chinese =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["launcher.title"] = "种子搜索",
            ["launcher.tooltip"] = "打开种子搜索面板",
            ["app.title"] = "Search TheSpire",
            ["beta"] = "公开测试 · v0.110.1",
            ["close"] = "关闭",
            ["board"] = "面板",
            ["board.tooltip"] = "显示搜索面板",
            ["popular"] = "热门",
            ["popular.tooltip"] = "显示热门搜索",
            ["saved"] = "已保存",
            ["saved.tooltip"] = "显示已保存的搜索",
            ["language"] = "语言",
            ["language.chinese"] = "中文",
            ["language.english"] = "English",
            ["searching"] = "搜索",
            ["branch.publicBeta"] = "公开测试 · v0.110.1",
            ["share"] = "分享",
            ["share.tooltip"] = "复制可分享的搜索规格",
            ["clear"] = "清空",
            ["clear.tooltip"] = "清空面板",
            ["compose"] = "组合你想要的开局",
            ["emptySlots"] = "留空的条件表示任意",
            ["dataNote"] = "批量搜索 · 参考 RNG 投影 · 若可用，Inspect 会使用游戏运行时",
            ["neow"] = "涅奥",
            ["neow.any"] = "任意涅奥奖励",
            ["neow.tooltip"] = "打开涅奥奖励选择器",
            ["character"] = "角色",
            ["character.any"] = "任意角色",
            ["character.tooltip"] = "选择角色；再次点击当前角色可清空",
            ["setup"] = "开局设置",
            ["ascension"] = "进阶",
            ["ascension.none"] = "无",
            ["ascension.level"] = "A{0}",
            ["runMode"] = "模式",
            ["runMode.plain"] = "普通模式",
            ["act1.elites"] = "第一幕精英",
            ["act1.shops"] = "第一幕商店",
            ["act1.rests"] = "第一幕休息处",
            ["minimum.elites"] = "{0}+ 个精英",
            ["minimum.shops"] = "{0}+ 个商店",
            ["minimum.rests"] = "{0}+ 个休息处",
            ["ancient"] = "远古",
            ["ancient.a"] = "远古 A",
            ["ancient.b"] = "远古 B",
            ["ancient.c"] = "远古 C",
            ["ancient.d"] = "远古 D",
            ["boss"] = "首领",
            ["boss.1"] = "首领 1",
            ["boss.2"] = "首领 2",
            ["boss.3"] = "首领 3",
            ["any"] = "任意",
            ["runLayout"] = "路线布局与掉落条件",
            ["runLayout.note"] = "首领、远古、奖励、商店、遗物袋和事件使用同一套嵌套选择器",
            ["relics.shop"] = "来自商店",
            ["relics.reward"] = "作为奖励",
            ["rewards.caption"] = "前 N 场战斗奖励中必须全部出现的卡牌",
            ["relics.shop.caption"] = "可多选的商店遗物栏",
            ["relics.reward.caption"] = "按稀有度排列的遗物奖励队列",
            ["events.caption"] = "第一幕问号房使用种子决定的事件队列",
            ["rewards.exact"] = "指定战斗奖励",
            ["advanced.show"] = "展开详情",
            ["advanced.hide"] = "收起详情",
            ["advanced.empty"] = "选择一个条件后，这里会显示它的后续选项",
            ["neow.details"] = "涅奥奖励详情",
            ["neow.details.note"] = "点击条件来缩小该奖励的内容；同组条件共享一次奖励抽取",
            ["neow.freshRares"] = "新鲜奖励中的稀有牌",
            ["picker.pick"] = "选择",
            ["picker.search"] = "搜索…",
            ["picker.close"] = "关闭选择器",
            ["picker.clear"] = "任意",
            ["picker.noMatch"] = "没有匹配的选项",
            ["picker.collapse"] = "展开或折叠此选项组",
            ["searchControls"] = "搜索控制",
            ["stopAfter"] = "找到多少个后停止",
            ["runsToSearch"] = "搜索候选数",
            ["advancedOffset"] = "高级偏移",
            ["matches5"] = "5 个结果",
            ["matches10"] = "10 个结果",
            ["matches20"] = "20 个结果",
            ["matches50"] = "50 个结果",
            ["candidates10k"] = "1 万候选",
            ["candidates50k"] = "5 万候选",
            ["candidates250k"] = "25 万候选",
            ["candidates1m"] = "100 万候选",
            ["search"] = "搜索",
            ["search.tooltip"] = "搜索种子",
            ["cancel"] = "取消",
            ["cancel.tooltip"] = "取消正在运行的搜索",
            ["ready"] = "就绪",
            ["search.complete"] = "搜索完成",
            ["search.cancelling"] = "正在取消…",
            ["inspect.title"] = "检查种子",
            ["inspect.note"] = "无需搜索即可预览任意种子的开局",
            ["inspect.placeholder"] = "粘贴种子…",
            ["inspect"] = "检查",
            ["inspect.tooltip"] = "检查这个种子",
            ["results"] = "结果",
            ["matches.zero"] = "0 个结果",
            ["spoilers.show"] = "显示全部剧透",
            ["spoilers.hide"] = "隐藏剧透",
            ["spoilers.tooltip"] = "切换结果详情",
            ["header.seed"] = "种子",
            ["header.map"] = "第一幕地图",
            ["header.neow"] = "涅奥奖励",
            ["header.ancients"] = "远古",
            ["header.bosses"] = "首领",
            ["status.initial"] = "结果会显示在这里。选择条件后点击“搜索”。",
            ["status.searching"] = "正在搜索…",
            ["status.cancelled"] = "搜索已取消",
            ["status.failed"] = "搜索失败；请检查 godot.log",
            ["status.noMatches"] = "没有找到符合条件的种子。请移除或放宽条件后重试。",
            ["status.matchCap"] = "已达到结果上限",
            ["status.saved"] = "搜索已保存",
            ["status.saveFirst"] = "请先搜索，再保存搜索条件",
            ["status.specCopied"] = "搜索规格已复制",
            ["status.seedCopied"] = "已复制 {0}",
            ["status.seedInspected"] = "已检查 {0} · {1}",
            ["status.pasteSeed"] = "请先粘贴种子",
            ["save"] = "保存搜索",
            ["save.tooltip"] = "将此搜索保存到 Mod",
            ["random"] = "复制随机种子",
            ["random.tooltip"] = "从当前分支复制一个种子",
            ["popular.title"] = "本周热门",
            ["popular.note"] = "热门搜索按本次游戏中保存的次数排序，可以直接重新打开。",
            ["saved.title"] = "本 Mod 中已保存",
            ["saved.note"] = "只要本次游戏仍在运行，已保存的搜索就会保留。",
            ["saved.empty"] = "还没有保存的搜索。先搜索，然后点击“保存搜索”。",
            ["popular.empty"] = "还没有热门搜索。请先保存一次搜索。",
            ["open"] = "打开",
            ["open.saved.tooltip"] = "打开已保存的结果",
            ["open.popular.tooltip"] = "打开这个热门搜索",
            ["result.preview"] = "种子预览",
            ["result.backend"] = "后端 · {0}",
            ["result.map"] = "第一幕路线 · {0}",
            ["result.neow"] = "涅奥详情 · {0}",
            ["result.ancientsBosses"] = "远古 · {0}    首领 · {1}",
            ["result.route"] = "前期路线统计 · {0} 个精英 · {1} 个商店 · {2} 个休息处",
            ["result.pins"] = "扩展条件 · {0}",
            ["result.details"] = "详情",
            ["result.details.hide"] = "收起详情",
            ["result.details.tooltip"] = "展开这个种子的预览",
            ["result.copy"] = "复制种子",
            ["result.copy.tooltip"] = "复制这个种子",
            ["saved.row"] = "{0} 个结果 · {1} · 偏移 {2}",
            ["popular.row.single"] = "次搜索",
            ["popular.row.plural"] = "次搜索",
            ["any.option"] = "任意",
            ["blocked.character"] = "请先选择角色",
            ["blocked.character.tooltip"] = "该条件需要确定角色专属卡池",
            ["blocked.ascension"] = "仅限 A{0}",
            ["blocked.neow"] = "请先选择会产生新鲜奖励的涅奥选项",
            ["blocked.reward"] = "请先添加奖励卡",
            ["blocked.parent"] = "请先选择它的父条件",
            ["backend.game"] = "游戏运行时",
            ["backend.reference"] = "参考 RNG",
            ["backend.unknown"] = "{0}",
            ["common.none"] = "无",
            ["common.anyRunStart"] = "任意开局",
            ["common.within"] = "前 {0} 层内",
            ["common.anyOrder"] = "任意顺序",
            ["common.inOrder"] = "按此顺序",
        };

    private static readonly IReadOnlyDictionary<string, string> ChineseNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["neowsbones"] = "涅奥之骨",
            ["neowssacrifice"] = "涅奥的献祭",
            ["neowstorment"] = "涅奥的折磨",
            ["neowstalisman"] = "涅奥的护符",
            ["largecapsule"] = "大型胶囊",
            ["smallcapsule"] = "小型胶囊",
            ["leafypoultice"] = "叶脉药膏",
            ["heftytablet"] = "沉重石板",
            ["precariousshears"] = "危险剪刀",
            ["silvercrucible"] = "白银坩埚",
            ["cursedpearl"] = "诅咒珍珠",
            ["dowsingrod"] = "占卜杖",
            ["arcanescroll"] = "奥术卷轴",
            ["boomingconch"] = "轰鸣海螺",
            ["fishingrod"] = "钓鱼竿",
            ["goldenpearl"] = "黄金珍珠",
            ["kaleidoscope"] = "万花筒",
            ["leadpaperweight"] = "铅镇纸",
            ["lostcoffer"] = "遗失的宝箱",
            ["newleaf"] = "新叶",
            ["phialholster"] = "药瓶套",
            ["precisescissors"] = "精密剪刀",
            ["scrollboxes"] = "卷轴盒",
            ["wingedboots"] = "翼靴",
            ["lavarock"] = "熔岩石",
            ["nutritiousoyster"] = "营养牡蛎",
            ["stonehumidifier"] = "石制加湿器",
            ["pomander"] = "香球",
            ["ruinedhelmet"] = "残破头盔",
            ["beatingremnant"] = "搏动残骸",
            ["bagofpreparation"] = "准备袋",
            ["unsettlinglamp"] = "不安之灯",
            ["anchor"] = "锚",
            ["lantern"] = "灯笼",
            ["kunai"] = "苦无",
            ["mango"] = "芒果",
            ["oldcoin"] = "古钱币",
            ["silkentress"] = "丝绸发辫",
            ["bloodvial"] = "血瓶",
            ["akabeko"] = "赤贝可",
            ["ceremonialbeast"] = "仪式巨兽",
            ["thekin"] = "同族",
            ["vantom"] = "梵托姆",
            ["lagavulinmatriarch"] = "拉格瓦林女王",
            ["soulfysh"] = "魂鱼",
            ["waterfallgiant"] = "瀑布巨人",
            ["kaisercrab"] = "凯撒蟹",
            ["knowledgedemon"] = "知识恶魔",
            ["theinsatiable"] = "贪得无厌者",
            ["aeonglass"] = "永恒玻璃",
            ["queen"] = "女王",
            ["testsubject"] = "实验体",
            ["orobas"] = "奥罗巴斯",
            ["pael"] = "佩尔",
            ["tezcatara"] = "特兹卡塔拉",
            ["darv"] = "达尔夫",
            ["nonupeipe"] = "诺努佩佩",
            ["tanx"] = "坦克斯",
            ["vakuu"] = "瓦库",
            ["strike"] = "打击",
            ["defend"] = "防御",
            ["bash"] = "猛击",
            ["ironwave"] = "铁波",
            ["neutralize"] = "中和",
            ["backflip"] = "后空翻",
            ["survivor"] = "生存者",
            ["seer"] = "预见",
            ["orbit"] = "轨道",
            ["dismantle"] = "拆解",
            ["zap"] = "电击",
            ["deadly_disease"] = "致命疾病",
            ["soul_fire"] = "灵魂之火",
            ["colorless_insight"] = "无色洞见",
            ["dominate"] = "支配",
            ["tearasunder"] = "撕裂",
            ["pyre"] = "火葬",
            ["bludgeon"] = "钝击",
            ["pommelstrike"] = "柄击",
            ["automation"] = "自动机",
            ["equilibrium"] = "平衡",
            ["thinkingahead"] = "未雨绸缪",
            ["darkshackles"] = "黑暗枷锁",
            ["fire_potion"] = "火焰药水",
            ["swift_potion"] = "迅捷药水",
            ["energy_potion"] = "能量药水",
            ["focus_potion"] = "专注药水",
            ["steroid_potion"] = "力量药水",
            ["attackpotion"] = "攻击药水",
            ["bloodpotion"] = "血液药水",
            ["clumsy"] = "笨拙",
            ["debt"] = "债务",
            ["decay"] = "衰败",
            ["doubt"] = "怀疑",
            ["guilty"] = "罪恶感",
            ["injury"] = "伤口",
            ["normality"] = "平庸",
            ["regret"] = "悔恨",
            ["shame"] = "羞愧",
            ["writhe"] = "痉挛",
            ["aromaofchaos"] = "混沌芬芳",
            ["byrdonisnest"] = "伯德的巢穴",
            ["densevegetation"] = "茂密植被",
            ["junglemazeadventure"] = "丛林迷宫冒险",
            ["luminouschoir"] = "辉光合唱团",
            ["morphicgrove"] = "变形林地",
            ["sapphireseed"] = "蓝宝石种子",
            ["tabletoftruth"] = "真理石板",
            ["unrestsite"] = "不安之地",
            ["wellspring"] = "源泉",
            ["whisperinghollow"] = "低语空洞",
            ["woodcarvings"] = "木雕",
            ["abyssalbaths"] = "深渊浴场",
            ["doorsoflightanddark"] = "光暗之门",
            ["drowningbeacon"] = "溺亡灯塔",
            ["endlessconveyor"] = "无尽传送带",
            ["punchoff"] = "冲拳",
            ["spiralingwhirlpool"] = "螺旋漩涡",
            ["sunkentreasury"] = "沉没宝库",
            ["trashheap"] = "垃圾堆",
            ["waterloggedscriptorium"] = "水淹抄经室",
            ["sunkenstatue"] = "沉没雕像",
            ["brainleech"] = "脑蛭",
            ["roomfullofcheese"] = "满屋奶酪",
            ["selfhelpbook"] = "自助书",
            ["slipperybridge"] = "湿滑桥",
            ["teamaster"] = "茶艺师",
            ["thefutureofpotions"] = "药水的未来",
            ["thelegendsweretrue"] = "传说是真的",
            ["thisorthat"] = "这个或那个",
        };

    private static readonly IReadOnlyDictionary<string, string> ChineseSlots =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["act"] = "第一幕地图",
            ["neowOffer"] = "涅奥奖励",
            ["bonesGrantA"] = "获得",
            ["bonesGrantB"] = "并获得",
            ["bonesCurse"] = "诅咒",
            ["bonesTabletCard"] = "稀有牌为",
            ["bonesArcaneCard"] = "稀有牌是",
            ["bonesPaperweightCard"] = "提供",
            ["bonesCofferCard"] = "提供卡牌",
            ["bonesCofferPotion"] = "药水",
            ["bonesCapsuleSet1"] = "胶囊内容包括",
            ["bonesCapsuleSet2"] = "并包括",
            ["bonesCapsuleSet3"] = "并包括",
            ["bonesKaleidoCard1"] = "提供卡牌",
            ["bonesKaleidoCard2"] = "并提供卡牌",
            ["bonesScrollboxCard1"] = "卷轴盒包含",
            ["bonesScrollboxCard2"] = "并包含",
            ["bonesScrollboxCard3"] = "并包含",
            ["bonesNewleafCard"] = "基础牌转化后得到",
            ["bonesPoulticeCard1"] = "药膏提供",
            ["bonesPoulticeCard2"] = "并额外提供",
            ["tabletCard"] = "稀有牌为",
            ["poulticeCard1"] = "药膏提供",
            ["poulticeCard2"] = "并额外提供",
            ["largeRelicA"] = "抽取",
            ["largeRelicB"] = "并抽取",
            ["paperweightCard"] = "提供",
            ["arcaneCard"] = "稀有牌是",
            ["cofferCard"] = "提供卡牌",
            ["cofferPotion"] = "药水",
            ["kaleidoCard1"] = "提供卡牌",
            ["kaleidoCard2"] = "并提供卡牌",
            ["newleafCard"] = "基础牌转化后得到",
            ["scrollboxCard1"] = "卷轴盒包含",
            ["scrollboxCard2"] = "并包含",
            ["scrollboxCard3"] = "并包含",
            ["phialPotionA"] = "提供药水",
            ["phialPotionB"] = "并提供药水",
            ["capsuleRelic"] = "提供遗物",
            ["boss1"] = "第一幕首领",
            ["boss2"] = "第二幕首领",
            ["boss3"] = "第三幕首领",
            ["boss3b"] = "第三幕第二首领（A10）",
            ["ancient2"] = "第二幕远古",
            ["ancient2Offers"] = "提供",
            ["ancient3"] = "第三幕远古",
            ["ancient3Offers"] = "提供",
            ["rewardWithin"] = "奖励窗口",
            ["rewardOrdered"] = "奖励顺序",
            ["rares"] = "新鲜奖励稀有牌",
            ["rewardPick1"] = "奖励包含",
            ["rewardPick2"] = "并包含",
            ["rewardPick3"] = "并包含",
            ["rewardPick4"] = "并包含",
            ["rewardPick5"] = "并包含",
            ["rewardPick6"] = "并包含",
            ["reward1"] = "第 1 场战斗奖励包含",
            ["reward2"] = "第 2 场战斗奖励包含",
            ["reward3"] = "第 3 场战斗奖励包含",
            ["shopWithin"] = "商店窗口",
            ["shopPick1"] = "商店遗物栏包含",
            ["shopPick2"] = "商店遗物栏包含",
            ["shopPick3"] = "商店遗物栏包含",
            ["shopPick4"] = "商店遗物栏包含",
            ["shopPick5"] = "商店遗物栏包含",
            ["shopPick6"] = "商店遗物栏包含",
            ["bagWithin"] = "遗物奖励窗口",
            ["bagPick1"] = "遗物奖励包含",
            ["bagPick2"] = "遗物奖励包含",
            ["bagPick3"] = "遗物奖励包含",
            ["eventWithin"] = "事件窗口",
            ["eventPick1"] = "第一幕事件",
            ["eventPick2"] = "第一幕事件",
            ["eventPick3"] = "第一幕事件",
            ["eventPick4"] = "第一幕事件",
            ["eventPick5"] = "第一幕事件",
        };

    private static readonly IReadOnlyDictionary<string, string> ChineseClusters =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["act"] = "地图",
            ["neow"] = "涅奥",
            ["Neow's Bones"] = "涅奥之骨",
            ["Hefty Tablet"] = "沉重石板",
            ["Arcane Scroll"] = "奥术卷轴",
            ["Lead Paperweight"] = "铅镇纸",
            ["Lost Coffer"] = "遗失的宝箱",
            ["Capsules"] = "胶囊",
            ["Kaleidoscope"] = "万花筒",
            ["Scroll Boxes"] = "卷轴盒",
            ["New Leaf"] = "新叶",
            ["Leafy Poultice"] = "叶脉药膏",
            ["Large Capsule"] = "大型胶囊",
            ["Phial Holster"] = "药瓶套",
            ["bosses"] = "首领",
            ["route"] = "路线",
            ["ancients"] = "远古",
            ["rewards"] = "奖励",
            ["relics"] = "遗物",
            ["shops"] = "商店",
            ["bag"] = "遗物袋",
            ["events"] = "事件",
        };

    private static readonly IReadOnlyDictionary<string, string> ChineseSections =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Act 1 map"] = "第一幕地图",
            ["cursed offer"] = "诅咒选项",
            ["bonus offer"] = "奖励选项",
            ["cards"] = "卡牌",
            ["potions"] = "药水",
            ["relic rewards"] = "遗物奖励",
            ["relic grants"] = "遗物给予",
            ["curse"] = "诅咒",
            ["ancient offers"] = "远古提供",
            ["offers if you qualify"] = "满足条件时提供",
            ["offers on any run"] = "任意开局提供",
            ["act 1 events"] = "第一幕事件",
            ["reward order"] = "奖励顺序",
            ["fresh reward rares"] = "新鲜奖励稀有牌",
            ["shared potions"] = "通用药水",
            ["any character"] = "任意角色",
            ["Ironclad cards"] = "铁甲战士卡牌",
            ["Silent cards"] = "静默猎手卡牌",
            ["Regent cards"] = "君权卡牌",
            ["Defect cards"] = "故障机器人卡牌",
            ["Necrobinder cards"] = "死灵契约者卡牌",
        };

    private static readonly IReadOnlyDictionary<string, string> ChineseReasons =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["the two grants must be distinct"] = "两次遗物给予必须不同",
            ["this boss belongs to the other Act 1 map"] = "该首领属于另一张第一幕地图",
            ["this map contradicts a pinned boss or event"] = "该地图与已固定的首领或事件冲突",
            ["event picks must be distinct"] = "事件条件不能重复",
            ["repeated relic picks must be distinct"] = "重复遗物条件必须不同",
            ["these grouped picks must be distinct"] = "同组条件必须不同",
            ["choose a character before narrowing card results"] = "请先选择角色，再缩小卡牌范围",
        };

    public static string Get(string key, SeedSearchLanguage language) =>
        (language == SeedSearchLanguage.Chinese ? Chinese : English).TryGetValue(key, out var value)
            ? value
            : English.GetValueOrDefault(key, key);

    public static string Format(string key, SeedSearchLanguage language, params object[] args) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, Get(key, language), args);

    public static string DisplayName(string id, SeedSearchLanguage language)
    {
        if (language == SeedSearchLanguage.Chinese && ChineseNames.TryGetValue(id, out var chinese))
        {
            return chinese;
        }

        return SearchTheSpireCatalog.DisplayName(id);
    }

    public static string SlotLabel(SearchTheSpireSlot slot, SeedSearchLanguage language) =>
        language == SeedSearchLanguage.Chinese && ChineseSlots.TryGetValue(slot.Id, out var label)
            ? label
            : slot.Label;

    public static string ClusterLabel(string cluster, SeedSearchLanguage language) =>
        language == SeedSearchLanguage.Chinese && ChineseClusters.TryGetValue(cluster, out var label)
            ? label
            : cluster;

    public static string SectionLabel(string section, SeedSearchLanguage language)
    {
        if (language != SeedSearchLanguage.Chinese)
        {
            return section;
        }

        foreach (var (prefix, translated) in ChineseSections)
        {
            if (section.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return translated + section[prefix.Length..];
            }
        }

        return ChineseClusters.GetValueOrDefault(section, section);
    }

    public static string OptionTitle(SearchTheSpireOption option, SeedSearchLanguage language)
    {
        if (language != SeedSearchLanguage.Chinese)
        {
            return option.Title;
        }

        if (option.Id.Length == 0)
        {
            return Get("picker.clear", language);
        }

        if (option.Title.StartsWith("within ", StringComparison.OrdinalIgnoreCase) &&
            option.Title[7..].Length > 0)
        {
            return Format("common.within", language, option.Title[7..]);
        }

        return option.Id switch
        {
            "0" => "茂盛林地",
            "1" => "地下码头",
            "false" => Get("common.anyOrder", language),
            "true" => Get("common.inOrder", language),
            _ => ChineseNames.TryGetValue(option.Id, out var translated)
                ? translated
                : option.Title,
        };
    }

    public static string OptionDescription(SearchTheSpireOption option, SeedSearchLanguage language) =>
        language == SeedSearchLanguage.Chinese && option.Description is { } description
            ? description switch
            {
                "Two relic grants with follow-up details." => "两次遗物给予，并可继续限定详情。",
                "event picks must be distinct" => "事件条件必须不同。",
                _ => description,
            }
            : option.Description ?? SectionLabel(option.Section, language);

    public static string BlockReason(string? reason, SeedSearchLanguage language) =>
        reason is { } value && language == SeedSearchLanguage.Chinese
            ? ChineseReasons.GetValueOrDefault(value, value)
            : reason ?? Get("blocked.parent", language);

    public static SearchTheSpireOption LocalizeOption(SearchTheSpireOption option, SeedSearchLanguage language) =>
        option with
        {
            Title = OptionTitle(option, language),
            Section = SectionLabel(option.Section, language),
            Description = OptionDescription(option, language),
            BlockReason = option.Blocked ? BlockReason(option.BlockReason, language) : option.BlockReason,
        };

    public static IReadOnlyList<SearchTheSpireOption> LocalizeOptions(
        IEnumerable<SearchTheSpireOption> options,
        SeedSearchLanguage language) => options.Select(option => LocalizeOption(option, language)).ToArray();

    public static string Character(RunCharacter character, SeedSearchLanguage language) =>
        language == SeedSearchLanguage.Chinese
            ? character switch
            {
                RunCharacter.Ironclad => "铁甲战士",
                RunCharacter.Silent => "静默猎手",
                RunCharacter.Regent => "君权",
                RunCharacter.Defect => "故障机器人",
                RunCharacter.Necrobinder => "死灵契约者",
                _ => Get("character.any", language),
            }
            : character == RunCharacter.Any ? Get("character.any", language) : character.ToString();

    public static string LocalizeText(string value, SeedSearchLanguage language)
    {
        if (language != SeedSearchLanguage.Chinese || string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Result rows are assembled by the reference engine from stable ids
        // before the UI language is known. Translate only the display names;
        // map symbols, seed text, and spec keys remain untouched.
        foreach (var pair in ChineseNames.OrderByDescending(pair => pair.Key.Length))
        {
            var english = SearchTheSpireCatalog.DisplayName(pair.Key);
            value = value.Replace(english, pair.Value, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    public static string Backend(string backend, SeedSearchLanguage language) => backend switch
    {
        "game-runtime" => Get("backend.game", language),
        "reference-rng" => Get("backend.reference", language),
        _ => Format("backend.unknown", language, backend),
    };
}
