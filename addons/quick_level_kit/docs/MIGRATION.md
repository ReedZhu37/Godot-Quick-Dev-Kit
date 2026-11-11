# Quick Level Kit GDScript → C# 对应与迁移

全部原 GDScript 生产脚本保留在 `../gds/`。C# 位于 `../csharp/`，命名空间 `QuickDev.Level`，不调用 GDScript 核心。两套实现的进度 schema 一致，脚本资源类型不共享。

| 原脚本（gds 下） | C#（csharp 下） |
| --- | --- |
| runtime/definitions/quick_level_kit_catalog.gd | runtime/definitions/QuickLevelKitCatalog.cs |
| runtime/definitions/quick_level_kit_definition.gd | runtime/definitions/QuickLevelKitDefinition.cs |
| runtime/definitions/quick_level_kit_flow.gd | runtime/definitions/QuickLevelKitFlow.cs |
| runtime/definitions/quick_level_kit_route.gd | runtime/definitions/QuickLevelKitRoute.cs |
| runtime/definitions/quick_level_kit_stage.gd | runtime/definitions/QuickLevelKitStage.cs |
| runtime/progress/quick_level_kit_config_file_progress_store.gd | runtime/progress/QuickLevelKitConfigFileProgressStore.cs |
| runtime/progress/quick_level_kit_progress.gd | runtime/progress/QuickLevelKitProgress.cs |
| runtime/progress/quick_level_kit_progress_store.gd | runtime/progress/QuickLevelKitProgressStore.cs |
| runtime/quick_level_kit_context.gd | runtime/QuickLevelKitContext.cs |
| runtime/quick_level_kit_level.gd | runtime/QuickLevelKitLevel.cs |
| runtime/quick_level_kit_manager.gd | runtime/QuickLevelKitManager.cs |
| runtime/quick_level_kit_session.gd | runtime/QuickLevelKitSession.cs |
| runtime/quick_level_kit_stage_runtime.gd | runtime/QuickLevelKitStageRuntime.cs |
| runtime/stages/quick_level_kit_advance_stage.gd | runtime/stages/QuickLevelKitAdvanceStage.cs |
| runtime/stages/quick_level_kit_default_flow.gd | runtime/stages/QuickLevelKitDefaultFlow.cs |
| runtime/stages/quick_level_kit_prepare_stage.gd | runtime/stages/QuickLevelKitPrepareStage.cs |
| runtime/stages/quick_level_kit_reload_stage.gd | runtime/stages/QuickLevelKitReloadStage.cs |
| runtime/stages/quick_level_kit_run_stage.gd | runtime/stages/QuickLevelKitRunStage.cs |
| runtime/stages/quick_level_kit_stop_stage.gd | runtime/stages/QuickLevelKitStopStage.cs |
| editor/quick_level_kit_dock.gd | editor/QuickLevelKitDock.cs |
| quick_level_kit_plugin.gd | QuickLevelKitPlugin.cs |
| examples/basic_usage.gd + .tscn | examples/BasicUsage.cs + .tscn，独立 DemoLevel.cs + .tscn |

API 采用 PascalCase，如 `load_level` → `LoadLevel`、`level_id` → `LevelId`。扩展钩子 `_prepare_level` → `protected PrepareLevel`、`on_enter` → `public OnEnter`。常量 `ERROR_*` → `Error*`，枚举 `AVAILABLE/FINAL/UNKNOWN` → `Available/Final/Unknown`，整数值保持 0/1/2。

## 原有工程升级

1. 先备份工程与进度文件，停用旧插件。
2. 继续用 GDScript：旧资源脚本路径改为 `res://addons/quick_level_kit/gds/...`，启用 GDScript 插件。原脚本 UID 保留，显式路径仍需同步更新。
3. 改用 C#：用 Godot .NET、保留 csharp 完整目录并 Build，将关卡根脚本迁移为 C# QuickLevelKitLevel 子类。
4. 重建或逐字段迁移 Catalog、Definition、Flow、Stage、Route 的 .tres，绑定 C# 脚本和 PascalCase 导出属性。不能只把 .gd 改成 .cs，也不能直接将 GDScript 资源强转成 C# 类型。
5. 核对 id、顺序、场景、metadata、默认与覆盖 Flow，运行 Catalog/Flow.Validate 与真实关卡。
6. 确认旧引用已迁移后再删除原 GDScript 文件，最后启用 C# 插件。两个实现共用 Autoload，只能启用其一。

**没有自动资源转换器。** 默认 ConfigFile 进度格式无需转换；这不等于场景/Resource 可跨语言直接互换。

## 功能对等范围

- 有序目录、结构化校验、未知/最后关区分、默认与逐关 Flow。
- 有序 Route、context type、required payload keys 与拒绝信号。
- 会话 Context 与每次 Stage 进入的 Runtime.State 隔离，共享配置 Resource。
- 准备、运行、完成/失败/重试、下一关、重载、停止、进度解锁/继续/重置。
- ConfigFile 路径、section/key、字段名与深拷贝语义。
- 编辑器目录操作、校验、默认 Flow 创建及独立示例。

## 明确加固的边界

- 退出阶段断开未触发的准备订阅，延迟完成不影响新会话。
- OnExit 内 Stop/Start、信号回调内重新加载/停止不让旧操作覆盖新状态。
- StageChanged 由会话统一发送，异步准备完成也通知 Manager。
- ResetProgress/SetProgressStore 同步活动 Context.Progress。
- 进度字段类型错误时安全返回空值。
- 两语言插件只在启停管理自己的 Autoload，编辑器退出保留注册。
- C# Dock 使用 Godot 4.7 EditorDock 并接入 Inspector 选择，导出时排除 UI。

仍仅支持单线顺序关卡；视觉转场、HUD、音频、输入锁和玩法由项目或自定义 Stage 负责。
