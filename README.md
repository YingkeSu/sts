# Seed Search

仓库全名：`sts-searchtheseed`

这是一个《杀戮尖塔 2》游戏内种子搜索 Mod，界面按 SearchTheSpire 的工作流组织：

- Board / Popular / Saved 标签页；
- 当前 public beta 分支选择；
- Neow、Act 1 精英、商店、休息点筛选；
- 搜索数量、候选范围、偏移量；
- Inspect seed；
- 结果表、保存搜索、复制种子、复制搜索规格、剧透开关。
- 点击具体 Neow 遗物后展开其条件子槽位，例如 Neow's Bones 的两个 grant、curse，以及 grant 遗物继续产生的牌/药水/胶囊约束；子槽支持分组 Picker、搜索和不可用原因提示。
- `run layout & drop pins` 展开 Act 1 地图、三幕首领、A10 第二首领、Ancient、奖励牌包、商店遗物、遗物袋和事件的同类嵌套 Picker；地图/首领会按地图联动，重复遗物与重复首领会被阻止。
- 点击结果行中的 `details` 查看该种子的 Act 1、Neow、Ancient、Boss 与早期路线摘要。

## 本地构建

```bash
/tmp/dotnet9/dotnet restore SeedSearchPrototype.csproj
/tmp/dotnet9/dotnet build SeedSearchPrototype.csproj --no-restore
/tmp/dotnet9/dotnet run --project tests/SeedSearchCoreChecks.csproj
```

默认构建不会安装 Mod，避免开发时意外加载。需要显式安装时使用：

```bash
/tmp/dotnet9/dotnet build SeedSearchPrototype.csproj --no-restore -p:InstallMod=true
```

安装目标是本机游戏的：

```text
SlayTheSpire2.app/Contents/MacOS/mods/SeedSearchPrototype/
```

当前项目是代码型 Mod，不需要 `.pck`。本地路径已按 Apple Silicon 安装目录显式配置在 [Sts2PathDiscovery.props](Sts2PathDiscovery.props) 中。

## 版本边界

- 当前 manifest 钉在 public beta `v0.110.1`；不要把其它分支的 RNG 结果混用。
- SearchTheSpire 的 seed preview 用于版本与字段抽检；它是浏览器端 Rust/WASM 工具，不作为 Mod 的运行时依赖。
- Inspect seed 会优先调用游戏运行时的单种子预览；批量搜索使用本地、可替换的 reference RNG backend。查询模型、Picker 与结果详情已经独立，后续替换版本化 RNG backend 时不需要重做页面。

## 使用

重启游戏后，右下角会出现 `Seed Search` 按钮。打开后可以搜索或检查种子，保存的搜索会写入 Godot 的用户数据目录；搜索接口与 UI 已独立，后续可以在不改页面的情况下替换为按游戏版本校验过的 RNG 实现。
