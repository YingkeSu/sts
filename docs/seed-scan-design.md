# 种子扫描链路与边界

## 当前实现

`SeedSearchOverlay.StartSearch()` 读取当前面板状态，生成一个 `SeedQuery`，然后在后台任务中调用 `SeedSearchEngine.Search()`。搜索过程是本地、确定性的候选枚举：

1. `SeedCodec.FromIndex()` 从 `StartOffset` 开始生成候选种子。公开测试分支使用 STS2 public-beta 的 12 位字母表。
2. `Inspect()` 对候选种子计算 public-beta `XxHash64`，按固定的 RNG stream 顺序生成第一幕地图、路线计数、涅奥、首领、远古、奖励、商店、遗物袋和事件摘要。
3. `Matches()` 先检查角色、路线计数、涅奥粗筛，再检查 `HiddenSpec` 中的父子条件和命名条件。
4. 找到 `StopAfter` 个结果、耗尽 `MaxCandidates`、或收到取消信号后结束。

`Inspect` 还会尝试使用游戏运行时 backend；批量 `Search` 当前使用本地 reference-RNG backend，以免在后台线程中反复进入游戏状态。结果会标出 backend，不能把 reference-RNG projection 当成游戏运行时的逐项证明。

## “宽松条件却没有结果”的原因

首个中文 UI 版本把显示文本 `Any` 直接写进了 `SeedQuery.AncientFilter` 和 `SeedQuery.BossFilter`。中文显示为 `任意` 后，扫描器仍按内部规范值判断，结果等价于要求远古和首领名称包含“任意”，因此所有候选都会被 `MatchesNamedFilter()` 拒绝。

现在显示文本与查询值分离：UI 的语言只改变按钮和标签，查询始终使用 `Any`、稳定的 slot id 和 `HiddenSpec`。同时对已经保存的旧版 `任意/任何` 查询保留兼容处理。

## 与 SearchTheSpire 对齐的进阶边界

SearchTheSpire 当前页面源码将游戏允许的进阶范围限定为 `none` 与 `A1` 到 `A10`；A7 以上只影响其 `scarcity` 标志，A11 到 A20 对该搜索页面没有可见计算差异。因此本 Mod 不再生成 A15/A20 等不存在于该页面的选项，内部状态也限制到 A10。

## 仍需保持的证据边界

- 候选扫描不是联网服务，也不依赖 SearchTheSpire 页面运行。
- 内容池和 RNG stream 是版本化数据；STS2 更新后必须重新做社区源码/API 对照。
- 只有游戏运行时 Inspect 成功时，结果 backend 才是 `game-runtime`；其余结果应标为 `reference-rng`。
