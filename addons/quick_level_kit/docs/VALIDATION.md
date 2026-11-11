# Quick Level Kit C# 验证记录

日期：2026-11-11（UTC）。实施分支：`feat/quick-level-csharp`，基于三模块集成主线 `37f430fd6c77f8cf679f0d7d1c58d3f703890915`。本工作没有直接修改 main，也未覆盖 Input、Audio、Database 或 VFX 的实现。

## 已通过

环境：Linux x86_64，Godot 4.7.2 .NET，.NET 9；保存数据放在隔离 HOME/XDG 中。

- 五个 C# 模块联合 Debug、Release、ExportRelease 构建：**0 warnings、0 errors**，启用 warnings-as-errors。
- C# 主套件：**132 checks，0 failures**。涵盖目录、校验、Route 顺序、默认流、生命周期、会话隔离、拒绝/回退、同步与异步准备、下一关/重载/停止、重入、进度深拷贝及损坏数据、编辑器目录操作和示例资源。
- C# 跨进程保存/重载：**23 checks，0 failures**。包括嵌套进度和 Catalog → Definition → Flow → Stage → Route 的真实 .tres 资源图。检查共享引用、全部五种内置派生 Stage、导出字段、场景类型与独立编辑。
- C# 写入 → GDScript 读取 → GDScript 写入 → C# 读取的 ConfigFile 互操作通过。不暗示脚本/Resource 可以直接跨语言替换。
- 原 GDScript **9 个套件**全部通过：catalog、default_flow、dock、flow、level、manager、package、progress、session。34 个原脚本全部保留。
- **8 项真实 headless 编辑器生命周期阶段**通过：C# 与 GDScript 各自 enable、restart、foreign-autoload conflict、language conflict。C# enable 阶段还覆盖 Inspector 选择实际 .tres → 已安装 Dock → 修改 → 保存重载 → 切换 Catalog。
- C# 纯独立安装通过：只复制 `csharp/`，不含 GDScript 或其他 Quick 模块；Debug/ExportRelease 构建、同一 132 项主套件及真实 C# Autoload 示例启动通过。
- Python 语言标记、源码编译覆盖和根文档链接检查通过；Quick Level 所有本地文档链接及 `git diff --check` 通过。

## 发现并修复的边界

- Stage 的延迟准备订阅必须显式断开；检查新增 Callable 的身份，而不是错误地要求 Godot 自带 C# 事件桥也消失。
- OnExit 内 Stop/Start 或 SessionStopped 回调重启时，旧跳转不能覆盖新 runtime。
- StageChanged 从会话统一发出，延迟准备也会通知 Manager，且不重复。
- 切换 ProgressStore/ResetProgress 后，活动 Context.Progress 与实际保存对象保持一致。
- Godot 4.7 将 Autoload 存为 `*uid://...`；两语言插件均解析已知 UID 后核对路径归属，未知 UID 或外部注册绝不删除。
- C# Dock 绑定 Inspector 选中的 Catalog；编辑器 UI 从 ExportRelease 中排除。

主套件故意对未知 level id 调用 LoadLevel，日志中对应一次 `Unknown level id 'missing'` 是被验证的错误分支；断言与退出码是通过判据，不忽略其他异常。

## 复现

```sh
python3 -m unittest discover -s tests -p 'test_*.py' -v
dotnet build QuickVfxDemo.csproj -c Debug -warnaserror
dotnet build QuickVfxDemo.csproj -c Release -warnaserror
dotnet build QuickVfxDemo.csproj -c ExportRelease -warnaserror
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/run_csharp_tests.sh
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/run_isolated_csharp.sh
GODOT_BIN=/path/to/godot-mono DOTNET_BIN=/path/to/dotnet tests/quick_level_kit/editor_lifecycle_tests.sh
```

如使用本地 NuGet 源，设置 `NUGET_CONFIG` 为其绝对路径。自动 CI 保持联合编译与静态检查，完整 Godot 验证仍由手动工作流执行。

## 未验收的范围

- 没有完成真实图形布局/截图验收。云端图形服务无法建立本地显示 socket；headless 编辑器检查不等于视觉验收。
- 没有运行 Windows/macOS 实机，未生成或验收平台导出包。ExportRelease 编译通过不等于打包验收。
- 没有自动转换已有用户的 GDScript 场景和 Resource。迁移步骤见 MIGRATION.md。
- 本记录描述本地验证；远端 Actions 状态以本次精确提交对应的工作流为准。
