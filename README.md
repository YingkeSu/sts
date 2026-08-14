# Seed Search

仓库全名：`sts-searchtheseed`

这是一个《杀戮尖塔 2》游戏内种子搜索 Mod

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

前置要求：本机已安装 .NET 9 SDK（用 `dotnet --version` 确认；如果 `dotnet` 不在 PATH，可使用本机 SDK 的完整路径，例如 `C:\Users\Lenovo\.dotnet\dotnet.exe`）。

```bash
dotnet restore SeedSearchPrototype.csproj
dotnet build SeedSearchPrototype.csproj --no-restore -p:InstallMod=false
dotnet run --project tests/SeedSearchCoreChecks.csproj
```

默认构建会尝试安装 Mod 到本机游戏目录，重启游戏后生效；如果只想编译不安装，使用上面的 `-p:InstallMod=false`。

### Windows

Windows 安装前需要把 `Sts2ModWorkshopId` 设为本项目的 Workshop id，或直接传 `-p:ModsPath=...`：

```bash
dotnet build SeedSearchPrototype.csproj -p:InstallMod=true -p:Sts2ModWorkshopId=<WorkshopId>
```

安装目标是：

```text
<SteamLibrary>\steamapps\workshop\content\2868840\<WorkshopId>\SeedSearchPrototype\
```

`Sts2PathDiscovery.props` 会按 OS 探测 Steam 库：Windows 使用 `data_sts2_windows_x86_64` 下的 `sts2.dll` 与 `0Harmony.dll`，并直接引用已安装的 BaseLib（Workshop item `3737335127`），不再依赖 NuGet 包。可用环境变量 `STS2_GAME_PATH`、`STS2_STEAM_LIBRARY`、`STS2_BASELIB_PATH`，或构建参数 `-p:Sts2GamePath=...`、`-p:BaseLibPath=...`、`-p:ModsPath=...` 覆盖探测结果。

macOS（Apple Silicon）安装目标保持：

```text
SlayTheSpire2.app/Contents/MacOS/mods/SeedSearchPrototype/
```

当前项目是代码型 Mod，不需要 `.pck`。



## 使用

重启游戏后，右下角会出现 `Seed Search` 按钮。打开后可以搜索或检查种子，保存的搜索会写入 Godot 的用户数据目录；搜索接口与 UI 已独立，后续可以在不改页面的情况下替换为按游戏版本校验过的 RNG 实现。
