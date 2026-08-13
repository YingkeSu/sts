# STS2 已验证 API 笔记

适用版本：public beta `v0.110.1`、Apple Silicon macOS。游戏更新后必须重新对照
已安装的 `sts2.dll` 与社区源码，API 名称和行为在不同分支间会变化。本文只记录
本仓库中实际调用过并验证过的用法，详细踩坑清单见技能
`sts2-mod/references/pitfalls.md`。

## Mod 入口与日志

- 使用 `MegaCrit.Sts2.Core.Modding.ModInitializer`：静态类上标注
  `[ModInitializer(nameof(Initialize))]`。
- 通过 `Engine.GetMainLoop() as SceneTree` 拿到主循环，再用
  `tree.Root.CallDeferred("add_child", overlay)` 挂载全局 UI；初始化时不能假设
  任何游戏画面已经打开。
- `MegaCrit.Sts2.Core.Logging.Logger` 与 `Godot.Logger` 同名冲突，必须别名：
  `using StsLogger = MegaCrit.Sts2.Core.Logging.Logger;`。日志写入
  `logs/godot.log`，统一带 Mod ID 前缀。

参考实现：[`MainFile.cs`](../SeedSearchPrototypeCode/MainFile.cs)

## manifest 字段

本项目 `SeedSearchPrototype.json` 已验证的字段：`id`、`name`、`author`、
`description`、`version`、`min_game_version`、`has_pck`、`has_dll`、
`dependencies`、`affects_gameplay`。

- 当前构建使用对象形式依赖：
  `{"id": "BaseLib", "min_version": "3.3.0"}`；旧分支可能需要字符串形式。
- 纯代码 Mod 固定 `has_pck: false`；输出目录、manifest `id` 与程序集名必须一致。
- `min_game_version` 是加载器兼容边界：UI 只能暴露 manifest 声明过的分支，每
  个分支单独测试和适配。

## Overlay UI 约定

- `CanvasLayer` 挂到场景树根，`Layer = 1000`，
  `ProcessMode = ProcessModeEnum.Always`。
- 全屏 shell `Control` 必须设置
  `MouseFilter = Control.MouseFilterEnum.Ignore`；只有可见遮罩
  `ColorRect` 和启动按钮接收输入。隐藏的 shell 若保持默认 `Stop` 会冻结整个游戏 UI。
- `Task.Run` 搜索完成后先检查 `IsCanceled`/`IsFaulted`，再
  `GetAwaiter().GetResult()`，并且每条终止路径都要恢复 Search/Cancel 按钮状态。

参考实现：[`SeedSearchOverlay.cs`](../SeedSearchPrototypeCode/SeedSearchOverlay.cs)

## 官方资源库：卡图、遗物与首领图

图片预览直接走游戏自身的模型与资源 API，不把游戏资产复制进 Mod：

- `CardModel.Portrait` 返回游戏资源包内的卡牌立绘，`CardModel.PortraitPath`
  通过 `ImageHelper.GetImagePath("atlases/card_atlas.sprites/...")` 计算；
  `CardModel.Pool.Title` 决定角色卡池目录，`ModelDb.AllCards` 可枚举全部卡牌。
- `CharacterModel.CharacterSelectIcon` 是角色选择肖像，
  `CharacterModel.IconTexture` 是顶部栏/下拉图标；
  `ModelDb.AllCharacters` 可枚举五个角色。
- `RelicModel.BigIcon` / `RelicModel.Icon` 直接读遗物图；`ModelDb.AllRelics`
  枚举全部遗物，`Id.Entry` 归一化后与 SearchTheSpire 槽位 id 对齐。
- Boss 使用 `ModelDb.AllEncounters` 中 `RoomType == Boss` 的
  `EncounterModel`，再经
  `ImageHelper.GetRoomIconPath(MapPointType.Boss, RoomType.Boss, encounter.Id)`
  加载 `res://images/ui/run_history/...` 首领图；SearchTheSpire 的 boss id
  通常等于“类型名去掉末尾 Boss”的归一化结果。
- 游戏 `ModelId.Entry` 由 `StringHelper.Slugify(类型名)` 生成（如
  `StrikeIronclad` -> `STRIKE_IRONCLAD`）；SearchTheSpire 的槽位 id 是
  去掉分隔符的小写形式，比对时先对两侧做“去分隔符 + 小写”归一化。
- 同名的 Basic 牌（Strike/Defend）会出现在每个角色池，优先用
  `OwnerCharacter` + `CardModel.Pool.Title` 挑出对应角色立绘。

参考实现：[`GameArtPreview.cs`](../SeedSearchPrototypeCode/GameArtPreview.cs)
社区反编译源码确认：
`zhiyue/sts2-rl-agent` 的 `CardModel.cs`、`CharacterModel.cs` 与
`ImageHelper.cs`；BaseLib 的 `CustomCardModel` 同样落在官方资源包路径上。

## 运行时单种子预览

以下类型全部属于主线程；禁止在 `Task.Run` 中构造。

1. 从 `ModelDb.AllCharacters` 选角色模型，调用
   `Player.CreateForNewRun(characterModel, unlocks, 1)`。
2. 可移动 act 列表：
   `ActModel.GetDefaultList().Select(act => act.ToMutable()).ToList()`；
   直接传规范 act 会抛 `CanonicalModelException`。
3. 创建运行：`RunState.CreateForNewRun(players, acts,
   Array.Empty<ModifierModel>(), GameMode.Standard, ascension, seed)`。
4. 先 `run.Act.GenerateRooms(run.Rng.UpFront, unlocks, false)`，再
   `StandardActMap.CreateFor(run, false)`；否则地图/首领为空。
5. `map.GetAllMapPoints()` 只含普通路线点；Ancient/Boss 是独立点，与
   SearchTheSpire 总数对比时要显式 `+2`。
6. 首领用 `run.Act.BossEncounter` 取模型 id。
7. `UnlockState` 没有公开的 all 构造；本构建通过反射读取非公开静态字段
   `all`。
8. 隔离运行下 `ActModel.Ancient`/`PullAncient()` 可能抛异常；adapter 应给出
   安全回退值，不能吞异常当成有效结果。
9. 地图预览按 SearchTheSpire 的 `map.nodes/edges` 模型还原：`StandardActMap`
   的 `StartingMapPoint`（row 0、Ancient）、`GetAllMapPoints()`（普通路线点）
   与 `BossMapPoint`（最顶行）共同构成节点，`MapPoint.Children` 提供连线；
   `MapPoint.coord` 的 `col/row` 直接用于渲染。

参考实现：[`GameSeedRuntime.cs`](../SeedSearchPrototypeCode/GameSeedRuntime.cs)

## 种子编码与参考 RNG

- 主分支：种子是索引的十进制字符串，使用旧 32 位 `HashCode`。
- public beta：12 位字母表 `0123456789ABCDEFGHJKLMNPQRSTUVWXYZ`（无 I/O），
  对 UTF-8 种子字节做 XxHash64。
- 命名 stream：`streamSeed = baseSeed + HashCode64(streamName)`（beta；
  主分支用旧 `HashCode`）。已验证名称：`act_selection`（Act 1 地图选择）、
  `up_front`（事件/战斗/精英/首领/Ancient 的完整消耗序列）、`NEOW`
  （Neow 提供项与 bonus）、`rewards`（Neow's Bones grant）。首领/Ancient
  不是 `up_front` 的前几个数；必须按 `RunManager.GenerateRooms` 的顺序消费
  共享/玩家遗物、共享 Ancient、每幕事件与战斗队列后再读取。
- 内容池与 RNG 消耗顺序都是版本化数据；游戏或 SearchTheSpire 更新后必须重新
  对照，测试固定值见 `tests/Program.cs`。

参考实现：[`SeedSearchEngine.cs`](../SeedSearchPrototypeCode/SeedSearchEngine.cs)

## 2026-08-13：布局 parity 修复记录

### 症状

本地 reference-RNG 引擎对 public beta 种子生成 Act 1 地图、三个首领、A10
第二首领与 Ancient 时，与 searchthespire.app 的 `inspect_seed` 输出
15/15 全不一致。

### 根因

- Act 1 地图 id 从 `baseSeed` 直接创建的 RNG 读取；正确来源是独立的
  `act_selection` stream（`baseSeed + HashCode64("act_selection")`）。
- 首领/Ancient 被当作 `up_front` stream 的前几个数读取。实际上
  `RunManager.GenerateRooms` 与 `ActModel.GenerateRooms` 会在首领/Ancient
  之前按版本化顺序消费共享/玩家遗物洗牌、共享 Ancient 判定、每幕事件与
  战斗队列。

### 正确的消耗顺序（v0.110.1 反编译）

1. `actSelectionRng = baseSeed + HashCode64("act_selection")`，
   Act 1 地图 id = `NextInt(2)`。
2. `layoutRng = baseSeed + HashCode64("up_front")`。
3. 共享/玩家遗物洗牌：`Advance(29 + 24 + 34 + 24 + 1)`，再
   `Advance(31 + 25 + 37 + 25)`。
4. 共享 Ancient 判定：`darv2 = NextInt(2)`；
   `darv3 = NextInt(2) != 0 && darv2 == 0`。
5. 逐幕消费：
   - Act 1：事件洗牌 `Advance((map == 1 ? 10 : 13) + 18 - 1)`；easy 战斗
     去重三抽（`NextInt(4)`/`NextInt(3)`/`NextInt(2)` 后调整）；hard 池
     12 抽，按战斗 tag 拒绝与上一场同 tag；5 个精英；首领从该地图池
     `NextInt(3)`；Act 1 Ancient 固定 Neow，消耗 1 个数。
   - Act 2：事件洗牌 `Advance(10 + 18 - 1)`；easy 两抽去重；Hive hard 池
     12 抽；5 个精英；首领 `Act2Bosses[NextInt(3)]`；Ancient
     `Act2Ancients[NextInt(3 + (darv2 ? 1 : 0))]`。
   - Act 3：事件洗牌 `Advance(7 + 18 - 1)`；easy 两抽去重；Glory hard 池
     11 抽；5 个精英；首领 `Act3Bosses[NextInt(3)]`；Ancient
     `Act3Ancients[NextInt(3 + (darv3 ? 1 : 0))]`；A10 第二首领为
     `NextItem(Act3Bosses 去掉已选首领)`，即对剩余 2 个做 `NextInt(2)`。

### 验证方法

- 用 Playwright 调用站点 WASM
  `inspect_seed(seed, charIdx, ascension, cursedIdx, bonus, bonesFirst, ver)`，
  入口 `https://searchthespire.app/v/5327a91/pkg/sts2_core.js`，站点版本
  `"0.110.1"`。
- 用 `ilspycmd`（`DOTNET_ROLL_FORWARD=LatestMajor`）反编译本地
  `sts2.dll`，核对 `RunManager.GenerateRooms` 与 `ActModel.GenerateRooms`
  的 stream 消耗；战斗 tag 常量来自社区 v0.107.1 反编译。
- 回归夹具 `tests/SeedLayoutParity.json` 固定 15 个种子，`tests/Program.cs`
  归一化后比对 `act1_map`、`boss1`、`boss2`、`boss3`、`boss3b`、
  `ancient2`、`ancient3`。

回归命令：

```sh
/usr/local/share/dotnet/dotnet run --project tests/SeedSearchCoreChecks.csproj
/usr/local/share/dotnet/dotnet build SeedSearchPrototype.csproj --no-restore
```

### 证据边界

当前只断言地图 id、首领与 Ancient；elite/shop/rest 数量仍是近似值（尚未移植
完整 `StandardActMap`），夹具中虽有记录但刻意不参与 parity 断言，不能宣称
与 SearchTheSpire 在这些字段上完全一致。

## 证据边界

- 批量搜索使用本地 reference-RNG backend，结果标记 `reference-rng`。
- 单种子 Inspect 可走游戏运行时，结果标记 `game-runtime`；本地装有内容 Mod 或
  池子不同时，不得宣称与 SearchTheSpire vanilla 完全一致。

## 相关研究

社区规范、工程模板、基础库与真实类型清单见
[`community-api-research.md`](community-api-research.md)；与 SearchTheSpire
逐元素对照见 [`searchthespire-gap-report.md`](searchthespire-gap-report.md)。
