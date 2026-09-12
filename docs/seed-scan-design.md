# 种子扫描链路与边界

## 当前实现

`SeedSearchOverlay.StartSearch()` 读取当前面板状态，生成一个 `SeedQuery`，然后在后台任务中调用 `SeedSearchEngine.Search()`。搜索过程是本地、确定性的候选枚举：

1. `SeedCodec.FromIndex()` 从 `StartOffset` 开始生成候选种子。公开测试分支使用 STS2 public-beta 的 12 位字母表。
   UI 默认启用“随机起点”：每次搜索先在种子空间中间 80% 内随机取一个
   `StartOffset`（解析进 `_lastQuery`，可复现/可保存），候选索引越过空间末尾时
   绕回开头，避免重复枚举同一批种子。
   候选预算默认是整个 public-beta 种子空间（34^12 ≈ 2.4 quintillion），
   UI 只额外提供“自定义候选数”一个入口；由于通常提前命中 `StopAfter`，
   全量预算主要作为覆盖上限而非实际扫描量。
2. `Inspect()` 对候选种子计算 public-beta `XxHash64`，按固定的 RNG stream 顺序生成涅奥、首领、远古、奖励、商店、遗物袋和事件摘要；Act 1 地图由
   `act_1_map` stream 经 `StandardActMap` 的 v0.110.1 移植生成（路径、点类型
   分配、剪枝与后处理全部对齐游戏反编译，见
   `docs/sts2-decompile/v0.110.1/`）。
3. `Matches()` 先检查角色、路线计数、涅奥粗筛，再检查 `HiddenSpec` 中的父子条件和命名条件。
4. 找到 `StopAfter` 个结果、耗尽 `MaxCandidates`、或收到取消信号后结束。

`Search()` 还接受可选的 `onMatch` 回调：每命中一个候选，就在工作线程上按候选索引
顺序回调一次（至多 `StopAfter` 次），命中行的展示由 Overlay 侧用线程安全的并发队列
加上 `CallDeferred` 回到主线程逐行追加，因此结果在搜索进行中就会出现在结果表里，
扫描热循环和命中快照生成路径都不变；`progress` 与 `onMatch` 的顺序、数量仍由同一次
波次合并决定，保持确定性。

批量 `Search` 使用本地 reference-RNG backend，以免在后台线程中反复进入游戏状态。
地图生成只在实际匹配到候选（或查询显式要求精英/商店/休息下限）时才展开；无地图
下限的扫描仍保持轻量并行路径。结果会标出 backend；reference 引擎的地图已经与
游戏运行时计数、节点数和完整布局摘要逐项对齐（见 `tests/SeedLayoutParity.json`），
单种子 Inspect 仍可额外切换到 `game-runtime` backend 复核。

## 搜索性能优化（快速评估路径）

批量搜索对每个候选按查询生成 `SearchPlan`，按需分阶段评估，而不是每个候选都跑一次
完整的 `Inspect`：

1. 种子编码与 `XxHash64` 走无分配的 `Span<byte>` 路径（`HashBetaSeed`），不再为每个
   候选创建种子字符串。
2. 只包含角色/Neow 等廉价条件的查询只做哈希级评估；不约束地图、首领或深锚点时，
   候选直接通过，命中的种子才调用完整 `Inspect` 生成结果快照。
3. 首领/先古/地图计数类约束走紧凑无分配布局链 `ComputeLayoutFast`，并按 spec 需要的
   最深字段分级（`LayoutDepth`：boss1 / boss2 / 全链），不需要整条 550+ 步 RNG 链时
   提前返回。
4. `HiddenSpec` 先解析成 `SpecFragment` 列表（每次搜索一次），逐候选直接用原始 id
   字段比对；只有奖励/商店/遗物袋/事件等“深详情”键才退回完整 `Inspect` 路径。

基准工具（M2 8 核 Release，`dotnet run --project tools/SeedBenchmark/SeedBenchmark.csproj -c Release`）：

| 查询形态 | 本机实测 | 优化前参考 |
| --- | --- | --- |
| 哈希级（如 `char=__never__`） | ≈15.6M runs/s | — |
| Neow 流级（如 `neow=999`） | ≈4.6M runs/s | — |
| boss1 布局级（如 `boss1=__never__`） | ≈2.5M runs/s | ≈0.3M runs/s |
| 深详情（如 `tablet_card=__never__`） | ≈0.64M runs/s | 同左（保留原路径） |

匹配命中后的快照仍会生成完整地图（每个约 40-50ms），这是结果展示成本，不影响扫描
速率；快照后端与 `Inspect` 完全一致。

## “宽松条件却没有结果”的原因

首个中文 UI 版本把显示文本 `Any` 直接写进了 `SeedQuery.AncientFilter` 和 `SeedQuery.BossFilter`。中文显示为 `任意` 后，扫描器仍按内部规范值判断，结果等价于要求远古和首领名称包含“任意”，因此所有候选都会被 `MatchesNamedFilter()` 拒绝。

现在显示文本与查询值分离：UI 的语言只改变按钮和标签，查询始终使用 `Any`、稳定的 slot id 和 `HiddenSpec`。同时对已经保存的旧版 `任意/任何` 查询保留兼容处理。

## 与 SearchTheSpire 对齐的进阶边界

SearchTheSpire 当前页面源码将游戏允许的进阶范围限定为 `none` 与 `A1` 到 `A10`；A7 以上只影响其 `scarcity` 标志，A11 到 A20 对该搜索页面没有可见计算差异。因此本 Mod 不再生成 A15/A20 等不存在于该页面的选项，内部状态也限制到 A10。

## 仍需保持的证据边界

- 候选扫描不是联网服务，也不依赖 SearchTheSpire 页面运行。
- 内容池和 RNG stream 是版本化数据；STS2 更新后必须重新做社区源码/API 对照。
- 只有游戏运行时 Inspect 成功时，结果 backend 才是 `game-runtime`；其余结果应标为 `reference-rng`。
- A1+ 的 SwarmingElites 会把精英房间目标从 5 提到 8，且修复阶段会改变部分休息点
  数量；对比不同进阶的地图时必须显式区分 A0 与 A1+ 夹具。
