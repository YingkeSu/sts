# 翻译与本地化

本项目没有把游戏内名词的中文译名复制进代码。实体名称在运行时直接读取游戏官方资源包：

- 语言文件夹来自游戏当前语言（`PlatformUtil.GetThreeLetterLanguageCode()`），中文走 `res://localization/zhs/*.json`。
- `OfficialGameLocalization` 用 `FileAccess` 读取官方 JSON 表（`relics.json`、`cards.json`、`potions.json`、`events.json`、`ancients.json`、`encounters.json`、`characters.json`、`acts.json`），按规范化 ID 建立反向索引。
- 因此游戏更新词条后，Mod 会自动跟随官方资源包，不依赖本仓库里维护一份中文副本。

Mod 自己新增的面板文案（按钮、状态、槽位说明等）在官方资源包里不存在，保留在
`Localization.ChineseUi` 中；这类文案不属于游戏本体翻译，无法从官方资源包读取。

## 社区参考

`ptrlrd/spire-codex` 的 `data/zhs/` 目录按语言抽取过同一批官方词条，可用于交叉核对；
本项目以本机游戏 `Slay the Spire 2.pck` 内的官方 JSON 为事实来源，避免引入第二份人工维护的译名表。
