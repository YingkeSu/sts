# SearchTheSpire 抽检记录

抽检目标是确认筛选结果仍能被 SearchTheSpire 的 seed preview 接受，并检查版本、种子格式和开局摘要字段；SearchTheSpire 与本 Mod 都按 public beta `v0.110.1` 读取。

本次使用的种子：

- `000000000000`
- `00000000000E`
- `000000000020`
- `000000000027`

使用 SearchTheSpire Inspect seed 页面加载的 `inspect_seed` WASM 接口逐个生成 preview，四个种子均返回结构化结果。其当前 WASM 返回的结构包含 `neow`、`acts[0].map`、`boss`、`ancient` 等字段；因此抽检重点是 seed 能被预览、版本没有混用，以及结果详情中的字段名与页面语义一致。

SearchTheSpire 是独立的社区工具，本项目没有复制它的 WASM 或私有部署文件；站点页面和已部署模块只用于行为参考，具体来源与版本边界见 [community-api-research.md](community-api-research.md)。
