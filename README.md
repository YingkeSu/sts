# Seed Search

仓库全名：`sts-searchtheseed`

这是一个《杀戮尖塔 2》游戏内种子搜索 Mod，界面按 SearchTheSpire 的工作流组织：

- Board / Saved 标签页（Popular 冗余模块已移除）；
- 当前 public beta 分支选择；
- Neow、Act 1 精英、商店、休息点筛选；
- 进阶覆盖 A0-A10（SearchTheSpire board 的 `none` + A1..A10），模型、UI 与恢复路径统一以 A10 为上限；
- 搜索数量、候选范围、偏移量；
- Inspect seed；
- 结果表、保存搜索、复制种子、复制搜索规格、剧透开关、清空已有候选。
- 点击具体 Neow 遗物后展开其条件子槽位，例如 Neow's Bones 的两个 grant、curse，以及 grant 遗物继续产生的牌/药水/胶囊约束；子槽支持分组 Picker、搜索和不可用原因提示。
- `run layout & drop pins` 展开 Act 1 地图、三幕首领、A10 第二首领、Ancient、奖励牌包、商店遗物、遗物袋和事件的同类嵌套 Picker；地图/首领会按地图联动，重复遗物与重复首领会被阻止。
- 点击结果行中的 `details` 查看该种子的 Act 1、Neow、Ancient、Boss 与早期路线摘要；有游戏运行时可用时，会按 SearchTheSpire 预览的样式绘制 Act 1 节点地图（类型着色、虚线路线、Boss/Ancient 收尾）。
- 卡牌、遗物与首领选择器以游戏官方资源图为主体：卡牌直接显示卡图，遗物和
  首领显示游戏内图标；角色下拉框带角色图标，并随选中角色展示角色肖像。
- 批量搜索按候选索引分块并行，占满本机全部 CPU 核心；版本化卡牌/遗物/药水池
  按角色预计算并缓存，候选热循环不再重复构建池。
- 批量搜索带查询计划：无分配种子编码/哈希、Neow/boss1/布局按需分级评估、`HiddenSpec`
  预先解析；只有命中种子才生成完整快照。可用
  `dotnet run --project tools/SeedBenchmark/SeedBenchmark.csproj -c Release`
  查看本机各查询形态的 runs/s（详细对比见 [docs/seed-scan-design.md](docs/seed-scan-design.md)）。
- Act 1 地图由 v0.110.1 `StandardActMap` 移植生成，搜索结果的路线、精英/商店/
  休息点计数与游戏内地图一致；预览地图直接使用游戏自带的房间图标。

## 本地构建

前置要求：本机已全局安装 .NET 9 SDK（用 `dotnet --version` 确认）。

```bash
dotnet restore SeedSearchPrototype.csproj
dotnet build SeedSearchPrototype.csproj --no-restore
dotnet run --project tests/SeedSearchCoreChecks.csproj
```

默认构建会安装 Mod 到游戏 Mods 目录，重启游戏后生效。如果只想编译不安装，使用：

```bash
dotnet build SeedSearchPrototype.csproj --no-restore -p:InstallMod=false
```

安装目标是本机游戏的：

```text
SlayTheSpire2.app/Contents/MacOS/mods/SeedSearchPrototype/
```

当前项目是代码型 Mod，不需要 `.pck`。本地路径已按 Apple Silicon 安装目录显式配置在 [Sts2PathDiscovery.props](Sts2PathDiscovery.props) 中。

## 版本边界

- 当前 manifest 与引擎都钉在 public beta `v0.110.1`，唯一来源是 [SeedSearchEngine.cs](SeedSearchPrototypeCode/SeedSearchEngine.cs) 的 `PinnedGameApiVersion`；测试断言 manifest 与常量一致，不要把其它分支的 RNG 结果混用。
- 版本边界与中文旧保存兼容的根因记录在 [docs/seed-scan-design.md](docs/seed-scan-design.md)；与 SearchTheSpire 的逐元素对照见 [docs/searchthespire-gap-report.md](docs/searchthespire-gap-report.md)，页面源码快照在 [docs/searchthespire-reference/](docs/searchthespire-reference/)。
- SearchTheSpire 的 seed preview 用于版本与字段抽检；它是浏览器端 Rust/WASM 工具，不作为 Mod 的运行时依赖。
- Inspect seed 会优先调用游戏运行时的单种子预览；批量搜索使用本地、可替换的 reference RNG backend。查询模型、Picker 与结果详情已经独立，后续替换版本化 RNG backend 时不需要重做页面。
- 已验证的游戏 API 用法、线程边界与 RNG 细节记录在 [docs/sts2-api-notes.md](docs/sts2-api-notes.md)；踩坑清单沉淀在 `sts2-mod` 技能的 `references/pitfalls.md`。
- 界面翻译直接读取游戏官方资源包中的本地化表，说明见 [docs/localization.md](docs/localization.md)。

## 使用

重启游戏后，右下角会出现 `Seed Search` 按钮。打开后可以搜索或检查种子，保存的搜索会写入 Godot 的用户数据目录；搜索接口与 UI 已独立，后续可以在不改页面的情况下替换为按游戏版本校验过的 RNG 实现。
