# SearchTheSpire 抽检记录

抽检目标是确认筛选结果仍能被 SearchTheSpire 的 seed preview 接受，并检查版本、种子格式和开局摘要字段；SearchTheSpire 与本 Mod 都按 public beta `v0.110.1` 读取。

本次使用的种子：

- `000000000000`
- `00000000000E`
- `000000000020`
- `000000000027`

使用 SearchTheSpire Inspect seed 页面加载的 `inspect_seed` WASM 接口逐个生成 preview，种子均返回结构化结果。当前 WASM 的相关字段位于 `neow`、`acts[0].map`、`acts[0].boss` 和 `acts[0].ancient`。

## 本次筛选结果抽检

在游戏内选择 `Neow's Bones`，角色、Ascension、地图、Boss 和其他开局条件保持任意，设置为最多 5 个结果、最多 10k 候选。Mod 编译出的 SearchTheSpire spec 是 `neow=5`，实际返回以下 5 个种子：

| seed | SearchTheSpire cursed | SearchTheSpire bonuses | SearchTheSpire Bones grants | Act 1 boss | map nodes |
| --- | --- | --- | --- | --- | ---: |
| `000000000003` | Neow's Bones | Pomander / Lead Paperweight | Lava Rock / Nutritious Oyster | Lagavulin Matriarch | 57 |
| `00000000000G` | Neow's Bones | Scroll Boxes / Small Capsule | Lost Coffer / Silken Tress | Soul Fysh | 63 |
| `00000000000L` | Neow's Bones | Phial Holster / Neow's Torment | Golden Pearl / Large Capsule | Soul Fysh | 68 |
| `00000000000N` | Neow's Bones | Precise Scissors / Kaleidoscope | Neow's Sacrifice / Leafy Poultice | Ceremonial Beast | 63 |
| `00000000000X` | Neow's Bones | Lost Coffer / Kaleidoscope | Nutritious Oyster / Phial Holster | Soul Fysh | 63 |

五个结果的 12 位 beta 种子格式、`Neow's Bones` 主筛选、两项 bonus 和两项 Bones grant 均与 SearchTheSpire preview 相符；这次抽检也验证了筛选器使用的是 SearchTheSpire 的 `neow=N` 语法，而不是 Mod 私有别名。

## 运行时交叉检查

Mod 的 Inspect 还用当前游戏进程创建单个 `RunState`，生成 Act 1 房间后读取运行时地图和首领。`StandardActMap.GetAllMapPoints()` 不包含单独的 Ancient/Boss 两个特殊点，因此这里将运行时普通节点数与 SearchTheSpire 总节点数按 `+2` 对齐；普通节点坐标顺序在四个种子上相同。

| seed | SearchTheSpire 节点 | 游戏普通节点 | 普通坐标 | SearchTheSpire 首领 | 游戏运行时首领 |
| --- | ---: | ---: | --- | --- | --- |
| `000000000000` | 64 | 62 | 相同 | Waterfall Giant | Ceremonial Beast |
| `00000000000E` | 64 | 62 | 相同 | Vantom | The Kin |
| `000000000020` | 60 | 58 | 相同 | Soul Fysh | Vantom |
| `000000000027` | 66 | 64 | 相同 | Lagavulin Matriarch | Vantom |

首领差异来自当前本地进程加载的内容/首领池与 SearchTheSpire 固定的 vanilla/public-beta 表不同；因此结果页把单种子检查标记为 `game runtime`，批量搜索仍使用 `reference RNG`，不把这四个样本宣称为完整 vanilla 首领 parity。

从 2026-08-14 起，Inspect/结果详情的 Act 1 地图预览直接采用上述运行时 `StandardActMap`：`StartingMapPoint` 作为 row 0 的 Ancient、`BossMapPoint` 作为最顶行的 Boss，普通点取自 `GetAllMapPoints()`，连线取自 `MapPoint.Children`，节点坐标与 SearchTheSpire 的 `map.nodes` 使用相同的 `col/row` 语义。

SearchTheSpire 首页明确将自己标为 unofficial fan-made tool；目前没有发现它的官方 GitHub 源码仓库。首页只公开链接了社区前身 [tckmn/sts2-seed-search](https://github.com/tckmn/sts2-seed-search)，并说明初始 Neow predicates 来自该仓库。本项目没有复制它的 WASM 或私有部署文件；站点页面和已部署模块只用于行为参考，具体来源与版本边界见 [community-api-research.md](community-api-research.md)。

## 网页源码隐藏设计回归

这次没有只对照首页的字段，而是逐项检查 SearchTheSpire 已部署页面的 `board.js`、`board_ui.js`、`picker.js`、`inspector.js` 和 `trending.js` 行为，并把容易遗漏的交互补进模型与页面：

- 角色切换会清理失效的角色专属 pins；无角色时的 Capsule/Large Capsule picker 同时显示 shared 与角色分组，点击角色专属遗物会推断角色。
- Kaleidoscope、Neow's Bones 胶囊拉取和 Scroll Boxes 共享池约束：不允许重复；Scroll Boxes 只允许 common/uncommon，并限制为 2 common + 1 uncommon。
- Kaleidoscope 六牌面按 SearchTheSpire wasm 的 `inspect_seed` 对照：`niche` 流 `StableShuffle` 其它角色池取前 3，`rewards` 流按 RegularEncounter 稀有度、选牌和升级各抽一次；`kaleido_distinct` 匹配六张牌面中的任意两张。
- beta 种子 codec 与 wasm 的 `display_for_index` 对齐（最高位在前），批量搜索的候选范围与偏移量因此和 SearchTheSpire 一致。
- 搜索默认从种子空间中间 80% 的随机位置开始（可关闭随机起点改为固定偏移）；低索引的
  beta 种子仍会显示成前几位有内容、右侧补 `0` 的形式（例如 `PT0000000000`）。这是
  SearchTheSpire 候选枚举的既定显示，不是生成错误；改动 codec 会破坏候选范围与站点
  偏移量的对齐。
- Neow 扩展项继续按同一 reward roll 分组，fresh-reward rares、ancient 条件 offer、event map lock、reward/shop/bag/event package floor 均保留依赖门槛。
- Saved 页的 Open 会从 `HiddenSpec` 恢复完整 board，而不是只恢复结果列表；冗余的
  Popular 统计页已移除。

对应的 TDD 回归输出包括 `character-switch`、`charless capsule`、`grouped-detail`、`package-floor` 与 `saved-query restore` checks；最新 DLL 在游戏内完成了 6 轮 `search → details → hide → clear` 全流程，且无 UI 输入冻结。

行为参考源码：

- [SearchTheSpire](https://searchthespire.app/)
- [SearchTheSpire board.js](https://searchthespire.app/v/aa92d44/board.js)
- [SearchTheSpire board_ui.js](https://searchthespire.app/v/aa92d44/board_ui.js)
- [SearchTheSpire picker.js](https://searchthespire.app/v/aa92d44/picker.js)
- [SearchTheSpire inspector.js](https://searchthespire.app/v/aa92d44/inspector.js)
- [SearchTheSpire trending.js](https://searchthespire.app/v/aa92d44/trending.js)
