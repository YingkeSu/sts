# SearchTheSpire 逐元素对照

对照基准是 `aa92d44` 的已部署源码快照，文件在
[`docs/searchthespire-reference/`](./searchthespire-reference/)，不是运行时依赖。

## 页面与交互键对照

| SearchTheSpire 元素 | 当前 Mod | 状态与差距 |
| --- | --- | --- |
| 游戏外/页面入口 | 主菜单右下角“种子搜索” | 已有。隐藏时 shell 忽略鼠标，只有按钮接收输入。 |
| Board / Popular / Saved | 同名三页（中文为“面板/热门/已保存”） | 已有；切页不丢失当前查询和结果。 |
| 版本/分支选择 | `public beta · v0.110.1` | 已有；当前只暴露已验证分支。 |
| Character art grid | 6 个角色按钮，顺序 Any、Ironclad、Silent、Regent、Necrobinder、Defect；再次点击清空 | 行为一致；当前使用文字按钮，未接入角色美术资源。 |
| Ascension | `none`、A1–A10 | 已有；不再暴露不存在的 A11–A20/A20。 |
| Neow 顶层 picker | 搜索、分组、折叠、Enter 首项、Esc/点击遮罩关闭 | 已有。 |
| Neow 子槽位 | 父项下方递归渲染，只显示当前父项启用的子项 | 已有；已验证 Bones 与大型胶囊不会泄漏无关子项。 |
| Card rewards | 连续多选行；选牌后才显示 within/order；exact fight 独立 disclosure | 已有；角色未选时在当前行显示“请先选择角色”。 |
| Relics | 一个遗物区，先 shop、后 reward；各自独立的多选行与 within | 已有。 |
| Events | 多选行；选事件后显示事件窗口 | 已有；事件选择仍由同一 picker 和地图约束控制。 |
| Ancients | Act 2/3 顶层选择；选中后就地出现 offers | 已有；名称和分组已中文化。 |
| Route | Act 1 map toggle + boss 紧凑选择 | 已有；首领候选的地图冲突项会禁用。当前为文字紧凑按钮，不是图片 art cell。 |
| Picker search | 全部候选的标题/ID/分组搜索 | 已有；显示层支持中英文，ID 仍稳定。 |
| Picker blocked option | 不可用项禁用并保留原因 | 已有。 |
| Result table | seed、map、Neow、ancients、bosses；行首 copy/details | 已有；详情位于行首，避免宽表裁掉动作。 |
| Spoilers | 默认隐藏，按钮切换 | 已有。 |
| Save / Popular / Saved restore | 保存、热门统计、打开后恢复 board 与结果 | 已有；恢复时从 HiddenSpec 重建父子控件。 |
| Share / Clear | 复制稳定查询规格；清空模型和所有可见控件 | 已有。 |
| Search / Cancel / progress | 后台本地扫描、进度、取消 | 已有。 |
| Inspect | 单种子预览，运行时可用时显示 game-runtime，否则 reference-RNG | 已有；批量 reference 地图已移植 `StandardActMap` 并与运行时夹具逐字段对齐，单种子仍可切到 game-runtime 复核。 |
| Language selector | 中文/English，即时重建显示层 | 已有；语言切换不改变稳定 ID、查询或结果。 |

## 已修复的行为差距

1. 中文 `任意` 不再写入 `SeedQuery`。查询使用规范值 `Any`，同时兼容旧保存中的 `任意/任何`；因此空板/宽松查询不再被远古和首领名称过滤掉。
2. 高级区不再按数据目录顺序平铺，固定为 SearchTheSpire 的“奖励 → 遗物 → 事件 → 远古 → 路线”。
3. `within`、order、exact fight 等控件遵循“先选父/包，再显示后续条件”的渐进式规则。
4. 结果和 picker 的显示名经过稳定 ID→显示名映射；中文结果不再直接显示英文内部内容名或 ID。

## 明确保留的差距

- 地图节点使用游戏自身资源（`NNormalMapPoint.IconName` 对应的
  `res://images/atlases/ui_atlas.sprites/map/icons/*.tres`、Ancient 的
  `packed/map/ancients/ancient_node_neow.png`），没有复制网页素材。角色/首领
  picker 仍是文字紧凑按钮。
- SearchTheSpire 的 WASM 搜索实现和完整游戏内容表不是公开稳定 Mod API。批量
  扫描保留本地 reference-RNG backend，其中 Act 1 地图已移植 v0.110.1
  `StandardActMap` 并与游戏运行时夹具逐位对齐；单种子 Inspect 仍尝试游戏运行时 backend。
- SearchTheSpire 的页面是浏览器响应式布局，Godot 面板使用固定的游戏内窗口和滚动列，因此像素级尺寸不会完全一致；交互顺序、父子可见性、分组和键行为已按源码对齐。

## 验证记录

- 纯模型测试：宽松 `Any/Any` 查询返回结果；旧 `任意/任意` 查询兼容；`Ascension=20` 会收敛到 A10；父子可见性和分组顺序有回归断言。
- 游戏内 smoke test：Steam 启动 STS2 后打开右下角入口；截图确认遮罩后仍有游戏主菜单背景；宽松扫描显示 20 个结果；中文/English 切换成功；`涅奥之骨 → 大型胶囊` 的子项紧邻父项，选择角色后角色专属项就地解锁。
