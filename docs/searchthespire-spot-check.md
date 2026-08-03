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

SearchTheSpire 是独立的社区工具，本项目没有复制它的 WASM 或私有部署文件；站点页面和已部署模块只用于行为参考，具体来源与版本边界见 [community-api-research.md](community-api-research.md)。
