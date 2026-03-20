# Schedule One Error Analyzer Web Plan

目标是做一个纯前端、可拖拽日志文件的分析工具，部署到 GitHub Pages，避免用户必须安装额外程序。

## 核心方向

- 复用 `ScheduleOne/ErrorAnalyzer/src/ErrorAnalyzer.Core` 的规则模型，而不是在网页里重新发明一套判断逻辑。
- 第一阶段先做“前端规则镜像”，保持规则 ID、标题、置信度、建议动作一致。
- 第二阶段再考虑把核心规则抽成共享 JSON 规则描述，或者用 WebAssembly/源码共享进一步统一实现。

## 前端体验

- 首页只做一件事：拖入 `Latest.log` 或整个日志文本。
- 左侧展示检测到的问题列表，按严重度排序。
- 右侧展示证据片段、推断原因、建议动作。
- 顶部固定显示运行时判断：`Mono`、`IL2CPP` 或 `Unknown`。
- 支持“复制诊断摘要”，方便用户发到 Discord、Nexus 或 GitHub issue。

## 技术方案

- 使用 React + TypeScript + Vite。
- 部署目标为 GitHub Pages，因此保持纯静态站点，不依赖服务端。
- 文件解析使用浏览器 `FileReader`，不上传任何日志内容。
- 状态管理保持轻量，初期直接用 React state；如果页面复杂再考虑 Zustand。

## 目录建议

- `C:\Users\ghost\Desktop\Coding\React\scheduleone-error-analyzer`
- `src/rules/` 存放前端版规则实现。
- `src/lib/` 存放日志解析、运行时判断、去重逻辑。
- `src/components/` 存放上传区、诊断列表、证据面板。

## 里程碑

1. 建立 React 项目骨架和拖拽上传页面。
2. 先实现与当前 C# 核心一致的高置信度规则。
3. 增加示例日志和快照测试，保证网页结论与核心库一致。
4. 加入 GitHub Pages 自动部署。
5. 再评估是否把规则定义抽离成共享格式。

## 测试建议

- 使用 `@ErrorLogs/` 中的代表性日志做前端 fixture。
- 每个 fixture 断言关键规则 ID 存在，例如：
  - `missing_patch_target`
  - `runtime_mismatch_mono_mod_on_il2cpp`
  - `missing_method`
  - `missing_dependency`

## 后续演进

- 增加“按 mod 分组”的结果视图。
- 增加“最近游戏更新导致的集中报错”提示。
- 增加规则命中统计，帮助后续扩展常见错误库。
