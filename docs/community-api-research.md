# 《杀戮尖塔 2》Mod 社区规范与社区 API 研究

研究范围：
- 优先一手来源：`Alchyr/ModTemplate-StS2`、`Alchyr/BaseLib-StS2`、`BAKAOLC/STS2-RitsuLib`、`ptrlrd/spire-codex`
- 辅助来源：社区教程/文档页、GitHub README、源码文件

结论先行：
- 社区里已经形成了两条明显路线：
  - `BaseLib` 路线偏“轻基础库 + 模板 + 自动化打包”
  - `RitsuLib` 路线偏“显式框架 + 兼容分支 + 运行时/设置/本地化/诊断一体化”
- 对大多数新 Mod，最稳妥的共识不是“直接补游戏源码”，而是：
  - 先选一个基础库
  - 用 manifest 声明运行时依赖
  - 用 `dll` 负责逻辑，用 `pck` 承载 Godot 资源
  - 兼容分支按游戏 API 版本切分，而不是按“看起来差不多”的游戏版本切分

## 1. 工程结构与构建/打包格式

### 社区共识

- STS2 模组工程常见三件套是：
  - `*.dll`
  - `mod_manifest.json`
  - 可选 `*.pck`
- `BaseLib` README 直接写明：需要把发布产物里的 `.dll`、`.pck`、`.json` 放进 `Slay the Spire 2/mods`。
- `RitsuLib` 明确是 `dll only`：`has_pck: false`，正常验证就是 DLL 构建。

来源：
- [BaseLib README](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/README.md)
- [BaseLib manifest](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/BaseLib.json)
- [RitsuLib README](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/README.md)
- [RitsuLib manifest](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/mod_manifest.json)

### 框架约定

- `Alchyr/ModTemplate-StS2` 采用 Godot .NET 工程模板，模板 README 说明：
  - 提供 3 个模板：`Mod`、`Content`、`Character`
  - 创建 solution 时要勾选 “Put solution and project in same directory”，否则不能按模板原样工作
- 模板工程的 `.csproj` 里自动做了：
  - 从游戏安装目录探测 `Sts2Path`
  - 构建后把 `dll`、`json`、`pdb` 复制到 `mods/<项目名>/`
  - 如果配置了 Godot 路径，则在 `Publish` 后 `--export-pack` 生成同名 `.pck`
- `RitsuLib` 的 `.csproj` 则把“兼容分支”做成一等公民：
  - 通过 `Sts2ApiCompat` 决定输出主包还是 `STS2.RitsuLib.Compat.<api>` 包
  - 通过 `RitsuLibManifestGenerated` 生成最终 `mod_manifest.json`
  - 通过 `Copy Mod` 目标把产物直接同步到游戏 `mods` 目录

来源：
- [ModTemplate README](https://github.com/Alchyr/ModTemplate-StS2/blob/8ed1955ce61ceec0f421322cbc1553100895ea28/README.md)
- [ModTemplate 模板工程 csproj](https://github.com/Alchyr/ModTemplate-StS2/blob/8ed1955ce61ceec0f421322cbc1553100895ea28/content/ModTemplate/ModTemplate.csproj)
- [RitsuLib csproj](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/STS2-RitsuLib.csproj)
- [RitsuLib 兼容定义](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/build/RitsuLib.CompatDefines.targets)
- [RitsuLib manifest 生成任务](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/build/RitsuLib.ModManifest.targets)

### 个人项目做法

- `ModTemplate-StS2` 更像“开箱即用脚手架”，适合：
  - 新项目
  - 需要 Godot 资源的 Mod
  - 想直接把产物复制进 `mods` 目录调试的人
- `RitsuLib` 更像“框架发行包”，适合：
  - 共享库
  - 需要长期维护多 API 分支的 Mod
  - 依赖较重的项目

## 2. manifest 字段与版本/依赖规范

### 社区共识

从三份 manifest 可以归纳出稳定字段：
- `id`
- `name`
- `author`
- `description`
- `version`
- `has_pck`
- `has_dll`
- `dependencies`
- `affects_gameplay`
- `min_game_version`

观察到的实际差异：
- `BaseLib` 和模板工程的 manifest 使用 `min_game_version`
- `RitsuLib` 的 manifest 当前没有写 `min_game_version`，而是依赖构建系统按 `Sts2ApiCompat` 生成最终 manifest
- `RitsuLib` README 说明：
  - 新一点的游戏 API 使用 object form：`{ "id": "STS2-RitsuLib" }`
  - 老版本 branch 还可能需要 legacy string form：`"STS2-RitsuLib"`

来源：
- [BaseLib manifest](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/BaseLib.json)
- [ModTemplate manifest](https://github.com/Alchyr/ModTemplate-StS2/blob/8ed1955ce61ceec0f421322cbc1553100895ea28/content/ModTemplate/ModTemplate.json)
- [RitsuLib manifest](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/mod_manifest.json)
- [RitsuLib README 依赖说明](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/README.md)

### 框架约定

- `ModTemplate-StS2` 默认把 `BaseLib` 作为依赖：
  - 模板 manifest 里写了 `{"id": "BaseLib", "min_version": "3.3.0"}`
  - csproj 里也引用了 `Alchyr.Sts2.BaseLib`
- `BaseLib` README 建议“已有 Mod 接入时”在 csproj 里加：
  - `<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />`
- `RitsuLib` 的构建过程会自动把 manifest 的 `name` 改成显示名，并在兼容分支下自动填 `min_game_version`
- `RitsuLib` 的兼容包命名规则：
  - 最新 API：`STS2.RitsuLib`
  - 兼容包：`STS2.RitsuLib.Compat.<api-version>`

来源：
- [ModTemplate README](https://github.com/Alchyr/ModTemplate-StS2/blob/8ed1955ce61ceec0f421322cbc1553100895ea28/README.md)
- [ModTemplate manifest](https://github.com/Alchyr/ModTemplate-StS2/blob/8ed1955ce61ceec0f421322cbc1553100895ea28/content/ModTemplate/ModTemplate.json)
- [BaseLib README](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/README.md)
- [RitsuLib manifest 生成任务](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/build/RitsuLib.ModManifest.targets)
- [RitsuLib csproj 兼容/包名规则](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/STS2-RitsuLib.csproj)

### 个人项目做法

- `BaseLib` 路线通常保留 `min_game_version` 在 source manifest 里显式写死。
- `RitsuLib` 路线更强调“单一源码、多兼容分支输出”，因此版本信息会从构建参数和目标 API 推导。

## 3. Harmony / patch / UI / 配置 / 定位 / 多语言 API

### Harmony / patch

`RitsuLib`：
- 以 `ModPatcher` 作为补丁管理核心
- 一个 `ModPatcher` 持有一个 Harmony 实例
- 支持静态补丁与动态补丁
- 重复 ID 会跳过
- 补丁失败时可选择 rollback

`BaseLib`：
- 有自己的 `HarmonyExtensions`、`InstructionPatcher`、`CallMatcher`、`AsyncMethod*` 等辅助，说明它不仅是“注册库”，也带了 Harmony/IL 层的工具箱。

来源：
- [RitsuLib `ModPatcher`](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/src/Patching/Core/ModPatcher.cs)
- [RitsuLib patch 模型](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/src/Patching/Models/ModPatchInfo.cs)
- [BaseLib `HarmonyExtensions`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/Extensions/HarmonyExtensions.cs)
- [BaseLib `InstructionPatcher`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/Utils/Patching/InstructionPatcher.cs)

### UI / settings

`BaseLib`：
- `Config/ModConfig.cs` + `ConfigAttributes.cs` 提供自动设置页
- 支持：
  - section
  - slider
  - tooltip
  - hide in UI
  - ignore
  - button
  - conditional visibility
  - text input 约束
- `ModConfig` 会把静态属性写到用户数据目录下的 `mod_configs`
- `NGame.Quit()` 时会保存所有 ModConfig

`RitsuLib`：
- `README` 直接把“settings UI”列成核心能力之一
- `csproj` 里有 `components/viewer`，说明它还内置调试日志 viewer 的构建和打包

来源：
- [BaseLib `ConfigAttributes`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/Config/ConfigAttributes.cs)
- [BaseLib `ModConfig`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/Config/ModConfig.cs)
- [BaseLib README](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/README.md)
- [RitsuLib README](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/README.md)

### 定位 / 资源路径 / 多语言

`BaseLib` 的关键约定：
- 场景和资源路径以 `res://` 形式注册
- 自动转换机制要求场景路径精确匹配，不支持通配符
- 配置 UI 的文案依赖 `settings_ui.json` 一类的本地化表

`RitsuLib` 的关键约定：
- 本地化、SmartFormat、诊断和 settings UI 都是主功能
- 运行时依赖 `SmartFormat.dll`
- 构建时会验证 `sts2.dll`、`0Harmony.dll`、`SmartFormat.dll` 是否存在于签名目录

来源：
- [BaseLib `docs/auto_conversion.md`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/docs/auto_conversion.md)
- [BaseLib `ConfigAttributes`](https://github.com/Alchyr/BaseLib-StS2/blob/45c85493349db6e6097ff96a09bd485a7b93c0ae/Config/ConfigAttributes.cs)
- [RitsuLib csproj 依赖校验](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/STS2-RitsuLib.csproj)
- [RitsuLib `Localization/I18NLocTable.cs`](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/src/Localization/I18NLocTable.cs)

### 个人项目做法

- `BaseLib` 更像“把常见 UI 和配置习惯做成默认行为”。
- `RitsuLib` 更像“把 UI、i18n、诊断、运行时输入一起纳入框架层”。
- 两者都不是游戏原生 API 的替代品，都是在游戏 API 上再包一层约定。

## 4. 种子、地图、RNG、数据读取/解析 API

### 结论

- 在这批一手来源里，`BaseLib` 和 `RitsuLib` 更偏“Mod 框架/API 层”，而不是“地图/种子解析器”。
- 真正适合做种子筛选、地图复现、卡牌/怪物/掉落数据库的，可复用项目是 `spire-codex`。

### Spire Codex 的可复用点

- `spire-codex` README 说明它是：
  - 反编译 `sts2.dll`
  - 通过 22 个 Python 正则解析器抽取结构化数据
  - 输出按语言分目录的 JSON
- README 还明确提到它覆盖的内容包含：
  - card odds
  - relic distribution
  - potion drops
  - map generation
  - boss pools
  - combat
- README 还写明它支持游戏自带的 15 种语言

来源：
- [Spire Codex README](https://github.com/ptrlrd/spire-codex/blob/main/README.md)

### 风险

- `spire-codex` 属于“逆向数据管线”，非常适合读数据，不适合直接当运行时 Mod API 依赖。
- 它的解析逻辑依赖：
  - 反编译结果结构
  - 当前游戏版本的代码形状
  - 具体正则和提取规则
- 因此它对“种子筛选器”最有价值的部分是：
  - 数据字典
  - 地图/掉落/池子模型
  - 语言文本映射
  - 解析方法本身
- 但它不保证可以直接拿来做游戏内实时 hook。

### 对“种子筛选器”最值得复用的库

优先级建议：
1. `spire-codex`
   - 用于构建 seed -> map / reward / pool 的离线知识库
2. `RitsuLib`
   - 用于做 Mod 运行时 UI、设置页、patch、数据持久化、诊断
3. `BaseLib`
   - 用于做轻量 UI、配置、自动转换、常见补丁辅助

不建议把 `BaseLib` 或 `RitsuLib` 当作“地图/种子算法真相源”：
- 它们更像运行时框架
- 真正的 seed / map / rng 事实应来自游戏源码解析或数据索引项目

### 风险点

- 最大风险不是 API 本身，而是版本漂移：
  - 地图/掉落/池子数据一旦随版本变化，离线数据库就会失真
  - 公开 beta 常常领先稳定分支，兼容包未必覆盖所有公开分支
- 如果要做“种子筛选器”，必须把：
  - 数据版本
  - 游戏分支
  - 解析器版本
 绑定在一起

## 5. 当前 v0.110.x / public-beta 的兼容性分支规则

### 社区共识

- `RitsuLib` README 明确写了：
  - 主包跟随“仓库支持的最高游戏 API”
  - 而“最高 API 经常在 beta 分支”
  - 因此面向其它 public branch 时要用 compat package
- `RitsuLib` 的 `RitsuLib.CompatDefines.targets` 当前支持的 API 列表是：
  - `0.103.2`
  - `0.106.1`
  - `0.107.0`
  - `0.107.1`
  - `0.108.0`
  - `0.109.0`
  - `0.110.0`
- 这说明截至当前仓库状态，`0.110.0` 是它支持的最新兼容目标

来源：
- [RitsuLib README](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/README.md)
- [RitsuLib 兼容定义](https://github.com/BAKAOLC/STS2-RitsuLib/blob/9155a410e23c8945c204714cf4e59eec0a63a2ce/build/RitsuLib.CompatDefines.targets)

### 解释

- 如果你面向的是 `v0.110.x` 稳定/公开分支：
  - 代码上应使用 `STS2.RitsuLib.Compat.0.110.0` 这类兼容包思路
  - 还是要按仓库实际发布资产确认最终包名
- 如果你面向的是“public-beta 领先于稳定分支”的场景：
  - 先看仓库是否已经把该 beta API 纳入最新兼容列表
  - 如果没有，就不要默认主包和 compat 包能直接兼容

这是基于仓库当前状态的推断，不是游戏官方承诺。

## 6. 对“种子筛选器”最值得复用的库和风险

### 最值得复用

1. `spire-codex`
   - 适合做离线知识库和预计算数据源
   - 适合 seed 相关的地图、池子、掉落、怪物池、语言文本索引
2. `RitsuLib`
   - 适合做筛选器的 Mod 前端、设置、持久化、调试和补丁接入
3. `BaseLib`
   - 适合快速做配置页、自动 UI、资源转换、轻量 patch 工具

### 主要风险

- 版本漂移：
  - 种子筛选器最怕把某个版本的地图/池子规律当成长期规则
- 分支漂移：
  - beta / public branch / compat branch 之间不能混用
- 数据来源漂移：
  - 逆向解析的结果是“当前版本快照”，不是永久真理
- 集成风险：
  - 运行时 Mod API 和离线数据管线不要混在同一层

## 附：本次研究中最有用的源码路径

- `Alchyr/ModTemplate-StS2`
  - `README.md`
  - `content/ModTemplate/ModTemplate.csproj`
  - `content/ModTemplate/ModTemplate.json`
- `Alchyr/BaseLib-StS2`
  - `README.md`
  - `BaseLib.json`
  - `docs/auto_conversion.md`
  - `docs/FmodAudio.md`
  - `Config/ModConfig.cs`

## 7. 与“种子筛选器”直接相关的社区实践

### 已经存在的种子搜索工具

- [SearchTheSpire](https://searchthespire.app/) 是目前最接近本项目目标的社区工具：它支持当前 public-beta `v0.110.1` 和主分支 `v0.107.1`，可以按角色、Neow 开局奖励、Ancient、Act 1 Boss、前期战斗/事件和商店遗物等条件搜索种子。
- 它的实现不是游戏内 Mod API，而是把部分 RNG 逻辑用 Rust 重写并编译为 WebAssembly，在浏览器 Worker 中并行搜索。
- 该工具明确把“种子固定内容”和“路线消耗随机数后的内容”分开：地图布局、Boss、部分开局信息适合筛选；战斗奖励、药水、金币、卡牌稀有度和 Boss 卡牌会随路线和此前的随机调用变化。
- 因此它是筛选语义和测试样例的重要参考，但当前不能直接作为本 Mod 的代码依赖。

来源：
- [SearchTheSpire 使用说明与 RNG 说明](https://searchthespire.app/)

### 游戏内 Mod 可复用的真实类型和事件

社区 Mod 源码已经验证了以下游戏内类型可以被直接引用或通过 Harmony 接入：

- `MegaCrit.Sts2.Core.Map.MapPoint`
  - `PointType`
  - `parents`
  - `children`
  - `coord`
- `MegaCrit.Sts2.Core.Map.ActMap`
  - `GetAllMapPoints()`
  - `StartingMapPoint`
  - `BossMapPoint`
  - `SecondBossMapPoint`
- `MegaCrit.Sts2.Core.Runs.RunState`
  - `Map`
  - `CurrentMapPoint`
  - `CurrentActIndex`
  - `ActFloor`
- `MegaCrit.Sts2.Core.Runs.RunManager`
  - `RunStarted`
  - `ActEntered`
  - `RoomEntered`
  - `RoomExited`
- `MegaCrit.Sts2.Core.Random.Rng`
  - 地图构造和随机逻辑的输入类型
- `MegaCrit.Sts2.Core.Map.StandardActMap`
  - 社区源码对其构造函数做过 Harmony postfix，用于在原始地图生成完成后读取或修改地图节点。
- `MegaCrit.Sts2.Core.Odds.UnknownMapPointOdds`
  - 社区源码对 `Roll` 做过 Harmony prefix，使用其内部 `_rng` 保持确定性和多人同步。

来源：
- [RouteSuggest 源码](https://github.com/jiegec/STS2RouteSuggest/blob/main/RouteSuggest.cs)
- [STS2 Random Map 的 StandardActMap patch](https://github.com/ing-gom/sts2-random-map/blob/main/Sts2RandomMapCode/Patches/StandardActMapPatch.cs)
- [STS2 Random Map 的 UnknownMapPointOdds patch](https://github.com/ing-gom/sts2-random-map/blob/main/Sts2RandomMapCode/Patches/UnknownOddsPatch.cs)
- [STS2 Random Map 的地图读取与 RNG 使用](https://github.com/ing-gom/sts2-random-map/blob/main/Sts2RandomMapCode/RandomMapService.cs)

### 对本项目架构的直接影响

1. 运行时前端可以采用标准 C# Mod + Harmony + `MapPoint`/`ActMap` 数据结构。
2. 候选种子扫描不能依赖 `RunManager` 当前正在运行的一局；需要独立构造候选的 RNG/地图，或嵌入一个经过版本测试的轻量 RNG/地图引擎。
3. 第一版应优先实现“开局条件和地图条件”的严格过滤；不能把任意卡牌奖励都承诺成种子固定结果。
4. 搜索引擎必须带上 `game_api_version`、分支、角色、进阶和自定义模式修饰器等上下文。官方在 `v0.107.1` 做过 RNG 重构，改用了 `xoshiro256**`，旧版搜索结果不能无条件复用到新版本。
5. 本机是 Apple Silicon，游戏实际使用 `data_sts2_macos_arm64`；社区模板的部分旧路径仍写成 `data_sts2_macos_x86_64`，构建工程需要显式覆盖 `Sts2DataDir`，不能盲信自动探测。

来源：
- [MegaCrit 官方 v0.107.1 更新说明：RNG 重构与 Mod 改进](https://steamcommunity.com/ogg/2868840/announcements/detail/710026912607505281)
- [ModTemplate 的 macOS 路径和构建约定](https://github.com/Alchyr/ModTemplate-StS2/blob/master/content/ModTemplate/Sts2PathDiscovery.props)
  - `Config/ConfigAttributes.cs`
  - `Patches/Localization/*`
  - `Patches/UI/*`
- `BAKAOLC/STS2-RitsuLib`
  - `README.md`
  - `mod_manifest.json`
  - `STS2-RitsuLib.csproj`
  - `build/RitsuLib.CompatDefines.targets`
  - `build/RitsuLib.ModManifest.targets`
  - `src/Patching/Core/ModPatcher.cs`
  - `src/Localization/*`
  - `src/Data/*`
  - `src/RitsuLibFramework.ModRunRng.cs`
- `ptrlrd/spire-codex`
  - `README.md`

## 6. 卡图、遗物与首领图加载

社区 Mod 不需要自己打包或拆包 `Slay the Spire 2.pck`，官方运行时已经暴露了
资源读取入口：

- `CardModel.Portrait` / `CardModel.PortraitPath` 直接读游戏资源包内的卡图；
  `ModelDb.AllCards` 枚举卡牌，`CardModel.Pool.Title` 定位角色卡池。
- `CharacterModel.CharacterSelectIcon` / `CharacterModel.IconTexture` 提供角色
  肖像与图标；`ModelDb.AllCharacters` 枚举角色。
- `RelicModel.BigIcon` / `RelicModel.Icon` 提供遗物图，`ModelDb.AllRelics`
  枚举遗物。
- Boss 走 `EncounterModel` 的 `RoomType == Boss` 条目，`ImageHelper`
  `GetRoomIconPath(MapPointType.Boss, RoomType.Boss, modelId)` 给出首领图路径。
- 反编译源码确认路径由 `ImageHelper.GetImagePath("atlases/...")` 统一换算，
  因此悬停预览与游戏内图鉴走的是同一套 `res://` 资产，版本更新后会自动跟随
  资源包。

来源：
- [zhiyue/sts2-rl-agent 反编译 `CardModel.cs`](https://github.com/zhiyue/sts2-rl-agent/blob/main/decompiled/MegaCrit.Sts2.Core.Models/CardModel.cs)
- [zhiyue/sts2-rl-agent 反编译 `CharacterModel.cs`](https://github.com/zhiyue/sts2-rl-agent/blob/main/decompiled/MegaCrit.Sts2.Core.Models/CharacterModel.cs)
- [zhiyue/sts2-rl-agent 反编译 `ImageHelper.cs`](https://github.com/zhiyue/sts2-rl-agent/blob/main/decompiled/MegaCrit.Sts2.Core.Helpers/ImageHelper.cs)

结论：官方模型 API + `ResourceLoader.Load<Texture2D>` 是可用且无需额外资源
依赖的社区方案；本项目在 `GameArtPreview.cs` 中封装了该适配层。
