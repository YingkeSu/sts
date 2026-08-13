# 并行 subagent 工作区隔离 Runbook

## 要求

并行 subagent 修复同一仓库的多个独立问题时，每个 subagent 必须运行在
**独立的 git worktree** 中，互相看不到也不应共享未提交改动。

## 2026-08-14 事故记录

### 现象

本次并行分派 5 个 subagent 后，工作区出现了并发写入：

- `git worktree list` 只有主 checkout，没有任何 subagent worktree；
- 同一仓库路径下同时出现两个 agent 的未提交改动（例如 `[issue-3]` 与
  `[issue-5]` 都写进了同一个 `tests/Program.cs`，`SeedSearchEngine.cs` 同时被
  两个 agent 编辑）；
- 其中一个 subagent 在最终回答里明确提到“工作区有并发写入，`[issue-5]` 块
  不是我改的”。

这违背了用户要求的 worktree 隔离，并引入了合并/互相覆盖风险。

### 根因

当前运行环境里的 `multi_agent_v1.spawn_agent` 工具**没有 worktree 参数**：
它只接受 `agent_type`、`fork_context`、`model`、`reasoning_effort`、
`service_tier`、`items`、`message`。工具描述中的 “forked workspace” 指
对话/上下文 fork，不是文件系统 worktree。因此按默认方式 spawn 的 subagent
共享同一个仓库 checkout，所有未提交改动都落在同一工作树里。

支持 worktree 的工具只有线程级 API：

- `codex_app.fork_thread`（`environment: {type: "worktree"}`）；
- `codex_app.create_thread`（project + worktree environment）。

它们创建的是独立 Codex 线程，而不是本线程的 subagent。在没有提前用
`git worktree add` 手动建树的前提下，subagent 无法获得真实隔离。

### 证据

```sh
git worktree list
# 只有一个主 checkout：/Users/suyingke/Documents/sts  e52c4e7 [master]

git status --short
# 多个无关 issue 的改动同时出现在同一工作树
```

## 预防流程

1. **分派前建树**。为每个 subagent 创建独立 worktree 和分支：

   ```sh
   git worktree add ../sts-subagent-issue-1 -b codex/issue-1
   git worktree add ../sts-subagent-issue-2 -b codex/issue-2
   # ...
   git worktree list   # 必须能看到每个新 worktree
   ```

2. **把 worktree 路径写进任务**。给每个 subagent 的 prompt 中明确：
   “你的工作目录是 `<绝对路径>`，所有命令都以该目录为 cwd，只允许修改该
   worktree 内的文件；不要访问/修改其它 worktree 或主 checkout。”
   同时保持文件所有权分区，作为第二道防线。

3. **只读核验**。spawn 后主线程抽查一次：

   ```sh
   git worktree list
   git -C <worktree> status --short
   ```

   若 subagent 开始写入主 checkout（例如主 `git status` 出现该 agent 的
   改动），立即中断并改为 worktree 模式。

4. **收口**。subagent 完成后，在主 checkout 用
   `git diff`/`git merge` 逐份审查并合并，禁止让多个 agent 同时写同一文件。

## 替代方案

- 如果任务本身适合线程级隔离，使用 `codex_app.create_thread` /
  `codex_app.fork_thread` 的 worktree environment；这些是用户可见线程，不
  是 subagent，适合需要长期保留、分别跟进的任务。
- 如果必须用 subagent 且不能建 worktree，则至少保证写集完全不相交，并在
  最终由主线程统一合并验证；本次即采用此降级方案。

## 检查清单

- [ ] `git worktree list` 显示每个 subagent 一个 worktree
- [ ] 每个 subagent prompt 含绝对 worktree 路径与“禁止跨树访问”
- [ ] 主 checkout 在 subagent 运行期间没有出现它们的未提交改动
- [ ] 最终合并前逐份 diff 审查，合并后全量测试
