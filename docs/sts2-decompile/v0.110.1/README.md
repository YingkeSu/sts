# STS2 v0.110.1 反编译参考快照

本目录保存从本机游戏程序集直接反编译出的类型源码，供移植地图生成算法、
接入游戏自带地图图标和校准种子搜索 RNG 时对照。快照不是项目可编译源码，
保留反编译器的原始输出（含 `You are not using the latest version of the tool`
提示），只作为参考材料。

## 来源

- 程序集：`Slay the Spire 2.app/Contents/Resources/data_sts2_macos_arm64/sts2.dll`
- 版本：`v0.110.1`（与 `SeedSearchEngine.PinnedGameApiVersion`、manifest 一致）
- 工具：`ilspycmd 8.2.0.7535`，因本机只有 .NET 9 运行时，用
  `DOTNET_ROLL_FORWARD=LatestMajor ~/.dotnet/tools/ilspycmd` 执行
- 生成日期：2026-08-14
- 目标平台：macOS Apple Silicon 构建

## 文件清单

| 类型 | 文件 |
| --- | --- |
| `MegaCrit.Sts2.Core.Map.StandardActMap` | `MegaCrit_Sts2_Core_Map_StandardActMap.cs` |
| `MegaCrit.Sts2.Core.Map.ActMap` | `MegaCrit_Sts2_Core_Map_ActMap.cs` |
| `MegaCrit.Sts2.Core.Map.MapPoint` | `MegaCrit_Sts2_Core_Map_MapPoint.cs` |
| `MegaCrit.Sts2.Core.Map.MapCoord` | `MegaCrit_Sts2_Core_Map_MapCoord.cs` |
| `MegaCrit.Sts2.Core.Map.MapPointType` | `MegaCrit_Sts2_Core_Map_MapPointType.cs` |
| `MegaCrit.Sts2.Core.Map.MapPointTypeCounts` | `MegaCrit_Sts2_Core_Map_MapPointTypeCounts.cs` |
| `MegaCrit.Sts2.Core.Map.MapPathPruning` | `MegaCrit_Sts2_Core_Map_MapPathPruning.cs` |
| `MegaCrit.Sts2.Core.Map.MapPostProcessing` | `MegaCrit_Sts2_Core_Map_MapPostProcessing.cs` |
| `MegaCrit.Sts2.Core.Models.ActModel` | `MegaCrit_Sts2_Core_Models_ActModel.cs` |
| `MegaCrit.Sts2.Core.Models.Acts.Overgrowth` | `MegaCrit_Sts2_Core_Models_Acts_Overgrowth.cs` |
| `MegaCrit.Sts2.Core.Models.Acts.Underdocks` | `MegaCrit_Sts2_Core_Models_Acts_Underdocks.cs` |
| `MegaCrit.Sts2.Core.Models.EncounterModel` | `MegaCrit_Sts2_Core_Models_EncounterModel.cs` |
| `MegaCrit.Sts2.Core.Models.AncientEventModel` | `MegaCrit_Sts2_Core_Models_AncientEventModel.cs` |
| `MegaCrit.Sts2.Core.Odds.UnknownMapPointOdds` | `MegaCrit_Sts2_Core_Odds_UnknownMapPointOdds.cs` |
| `MegaCrit.Sts2.Core.Random.Rng` | `MegaCrit_Sts2_Core_Random_Rng.cs` |
| `MegaCrit.Sts2.Core.Random.MegaRandom` | `MegaCrit_Sts2_Core_Random_MegaRandom.cs` |
| `MegaCrit.Sts2.Core.Extensions.ListExtensions` | `MegaCrit_Sts2_Core_Extensions_ListExtensions.cs` |
| `MegaCrit.Sts2.Core.Helpers.StringHelper` | `MegaCrit_Sts2_Core_Helpers_StringHelper.cs` |
| `MegaCrit.Sts2.Core.Helpers.ImageHelper` | `MegaCrit_Sts2_Core_Helpers_ImageHelper.cs` |
| `MegaCrit.Sts2.Core.Helpers.AscensionHelper` | `MegaCrit_Sts2_Core_Helpers_AscensionHelper.cs` |
| `MegaCrit.Sts2.Core.Entities.Ascension.AscensionLevel` | `MegaCrit_Sts2_Core_Entities_Ascension_AscensionLevel.cs` |
| `MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager` | `MegaCrit_Sts2_Core_Entities_Ascension_AscensionManager.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapScreen` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NMapScreen.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NMapPoint.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NNormalMapPoint` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NNormalMapPoint.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NBossMapPoint` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NBossMapPoint.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NAncientMapPoint` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NAncientMapPoint.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapBg` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NMapBg.cs` |
| `MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapLegendItem` | `MegaCrit_Sts2_Core_Nodes_Screens_Map_NMapLegendItem.cs` |
| `MegaCrit.Sts2.Core.Rooms.RoomType` | `MegaCrit_Sts2_Core_Rooms_RoomType.cs` |
| `MegaCrit.Sts2.Core.Models.ModelId` | `MegaCrit_Sts2_Core_Models_ModelId.cs` |

## 本任务最关心的结论

- `StandardActMap` 的地图宽度固定 7，`_mapLength = GetNumberOfRooms(false) + 1`；
  Act 1 的 Overgrowth/Underdocks 都是 `BaseNumberOfRooms = 15`，所以普通路线点
  在 row 1..15。
- 点类型数量来自 `ActModel.GetMapPointTypes(mapRng)`：休息点在
  `NextGaussianInt(7, 1, 6, 7)`，未知点在 `NextGaussianInt(12, 1, 10, 14)`；
  `MapPointTypeCounts` 的精英默认 5，SwarmingElites（A1+）为 8；商店固定 3。
- 地图 RNG 是 `new Rng(runState.Rng.Seed, $"act_{actIndex + 1}_map")`，即
  `seed + XxHash64("act_1_map")`；本工程 reference 引擎应使用相同的 stream。
- 剪枝与后处理必须完整移植：`MapPathPruning.PruneAndRepair`、
  `MapPostProcessing.CenterGrid/SpreadAdjacentMapPoints/StraightenPaths`，
  SearchTheSpire 的 map 输出与游戏是 oracle 位级一致。
- 游戏地图图标路径来自 `NNormalMapPoint.IconName/IconPath`：
  `res://images/atlases/ui_atlas.sprites/map/icons/map_monster.tres` 等；
  Boss 用 Boss 节点资源（`EncounterModel.BossNodePath`），Ancient 用
  `AncientEventModel.MapIcon`（`packed/map/ancients/ancient_node_*.png`）。

## 再生命令

```sh
DOTNET_ROLL_FORWARD=LatestMajor ~/.dotnet/tools/ilspycmd -t \
  MegaCrit.Sts2.Core.Map.StandardActMap \
  '/Users/suyingke/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/Resources/data_sts2_macos_arm64/sts2.dll'
```
