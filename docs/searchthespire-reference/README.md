# SearchTheSpire 网页源码与设计档案

这份目录保存 SearchTheSpire 已部署页面的可复核快照，作为本 Mod 的交互与视觉参考，不作为运行时依赖，也不复制其 WASM、素材或私有部署数据。

## 快照信息

- 站点：[https://searchthespire.app/](https://searchthespire.app/)
- 部署版本：`aa92d44`
- 抓取日期：`2026-08-04`
- 页面声明：SearchTheSpire 是 unofficial、fan-made tool；其首页公开指向的社区前身是 [tckmn/sts2-seed-search](https://github.com/tckmn/sts2-seed-search)。
- 源码文件：`board.js`、`board_ui.js`、`picker.js`、`inspector.js`、`trending.js`

## 文件校验

| 文件 | 字节数 | SHA-256 |
| --- | ---: | --- |
| `board.js` | 75637 | `fd1757a384012a3e680e815962158c556b738d00a8a379470d2f72cf46299aa2` |
| `board_ui.js` | 29530 | `43c97c19a115b94863dfb5c3e205629513e381bd2a06eb1c19fab74e64f0ac18` |
| `picker.js` | 6510 | `02ef9680523b4ed09e35d4ed471011fb42eb5d91f22af57afc8a41a3af444503` |
| `inspector.js` | 25680 | `8c7915af37f87c1b0a1659285766c7ef6d16001791c7880b85bd678c98f06be3` |
| `trending.js` | 1599 | `651e705429c4ca3f0fd985b6cf9d3d78bf887c2282d1521e04b09aaf18f22e3b` |

## 设计要点

### 页面层级

- 顶层只有 `Board`、`Popular`、`Saved` 三个页面；页面切换不销毁查询状态。
- Board 的主要流程是：分支选择 → 查询板 → 搜索控制 → 搜索结果 → 保存/复制/Inspect。
- 查询板不是一张把所有字段平铺的表，而是由“父槽位 + 就地扩展的子槽位”组成。

### 父子交互

- 选择 Neow 遗物后，只有该遗物的扩展区出现；扩展区紧跟父槽位，并按 reward roll / grant cluster 分组。
- 子槽位显示当前值、清除入口、搜索入口和不可用原因；不相关的父项不会展示其子项。
- 多次抽取类槽位使用同一 picker 模型，重复项会在当前组内被禁用，而不是把约束挪到页面另一端。
- 角色专属池在没有选择角色时仍可浏览，但会显示角色归属；选择角色专属项可以反向设置角色。

### 信息密度与可读性

- 结果行先放种子与 `details` 动作，宽字段使用换行，不让末尾动作被裁切。
- Spoiler 信息默认收起，只有用户明确展开时才显示。
- Picker 内有统一的搜索框、分组标题、折叠状态和 blocked reason。
- 文案使用稳定的显示名映射；内部 ID 不直接暴露给用户。

### 本项目的对齐结果

逐元素差距、已修复项、保留差距和验证记录见
[`docs/searchthespire-gap-report.md`](../searchthespire-gap-report.md)。当前实现保留可测试的
`SearchTheSpireBoardState`，在 Godot 展示层实现父子就地扩展、渐进式多选行、exact-fight disclosure、
稳定显示名和中英文切换。

## 在线对应文件

- [board.js](https://searchthespire.app/v/aa92d44/board.js)
- [board_ui.js](https://searchthespire.app/v/aa92d44/board_ui.js)
- [picker.js](https://searchthespire.app/v/aa92d44/picker.js)
- [inspector.js](https://searchthespire.app/v/aa92d44/inspector.js)
- [trending.js](https://searchthespire.app/v/aa92d44/trending.js)
