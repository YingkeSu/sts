# Seed Search

这是一个《杀戮尖塔 2》游戏内种子搜索 Mod，界面按 SearchTheSpire 的工作流组织：

- Board / Popular / Saved 标签页；
- 当前 public beta 分支选择；
- Neow、Act 1 精英、商店、休息点筛选；
- 搜索数量、候选范围、偏移量；
- Inspect seed；
- 结果表、保存搜索、复制种子、复制搜索规格、剧透开关。

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

## 使用

重启游戏后，右下角会出现 `Seed Search` 按钮。打开后可以搜索或检查种子，保存的搜索会写入 Godot 的用户数据目录；搜索接口与 UI 已独立，后续可以在不改页面的情况下替换为按游戏版本校验过的 RNG 实现。
