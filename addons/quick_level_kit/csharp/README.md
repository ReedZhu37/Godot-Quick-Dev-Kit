# Quick Level Kit (C#)

独立原生 C# 实现，命名空间 `QuickDev.Level`。包含关卡目录、定义、流程、阶段、路由、会话、生命周期、进度存储、编辑器 Dock 和示例；不调用 GDScript 核心。

## 环境与安装

验证基线：**Godot 4.7.2 .NET / .NET 9**。普通 Godot 编辑器不能加载 C#。

1. 保留 `addons/quick_level_kit/csharp/` 的完整相对路径，复制到自己的项目。
2. Build 后在 Project Settings → Plugins 启用 **Quick Level Kit (C#)**。
3. 插件注册 `/root/QuickLevelKit`，脚本为 `res://addons/quick_level_kit/csharp/runtime/QuickLevelKitManager.cs`。不要同时启用 GDScript 版本；同名 Autoload 被占用时保留原注册，先停用原插件，再重新启用 C# 版。
4. 编辑器退出或重开不会删除 Autoload；停用插件只移除自己注册的脚本。
5. F6 运行 `examples/BasicUsage.tscn`。示例关卡自动完成准备，停留在 run 阶段。

项目自己用显式 `<Compile Include>` 时包含 `addons/quick_level_kit/csharp/**/*.cs`。插件和 Dock 受 `TOOLS` 条件保护，导出运行时不编译编辑器 UI。测试和示例可在不需要时从安装目录剔除，不要删除 `runtime/`。

## 最小接入

```csharp
using Godot;
using QuickDev.Level;

public partial class Game : Node
{
    public override void _Ready()
    {
        var catalog = new QuickLevelKitCatalog
        {
            DefaultFlow = QuickLevelKitDefaultFlow.Create(),
            Levels = new()
            {
                new QuickLevelKitDefinition
                {
                    LevelId = "level_001",
                    Scene = GD.Load<PackedScene>("res://levels/Level001.tscn")
                }
            }
        };
        var manager = GetNode<QuickLevelKitManager>("/root/QuickLevelKit");
        manager.SetCatalog(catalog);
        manager.LoadFirstLevel();
    }
}
```

关卡根节点必须继承 C# `QuickLevelKitLevel`。可覆写四个钩子：

```csharp
public partial class Level001 : QuickLevelKitLevel
{
    protected override void PrepareLevel() => CompletePreparation();
    protected override void StartLevel() { /* 游戏玩法 */ }
    protected override void FinishLevel(Godot.Collections.Dictionary data) { }
    protected override void TeardownLevel() { /* 清理 */ }
}
```

`Complete(data)`、`Fail(data)`、`Retry(data)` 统一发送 `TransitionRequested`，再转交 Manager。payload 是 `{ "type": StringName, "data": Dictionary }`；无数据时不调用 `FinishLevel`。`Teardown` 幂等，之后不再转发旧关卡事件。`CompletePreparation` 在准备阶段仅生效一次。

## 流程与扩展

`QuickLevelKitFlow` 包含 `StartStageId` 与有序 `Stages`；每个 `QuickLevelKitStage` 包含 `StageId` 与有序 `Routes`。`QuickLevelKitRoute` 的 `ContextType` 留空表示通配，`RequiredPayloadKeys` 要求 `data` 包含指定键，`TargetStageId` 指向目标阶段。首条匹配的 Route 生效；无匹配或无目标时发出 `TransitionRejected` 并保持阶段。

内置默认流程：

- prepare：调用 `BeginPreparation`，准备完成后 ready → run。
- run：调用 `BeginRunning`；completed → advance，failed/retry → reload。
- advance：解锁并加载下一关；最后一关 finished → stop。
- reload：创建当前关卡的新实例。
- stop：结束会话并清理关卡。

自定义资源继承 `QuickLevelKitStage`，覆写 `OnEnter(QuickLevelKitStageRuntime)`、`OnExit(...)` 或 `OnPreparationCompleted(...)`。持久配置写入 Resource；每次进入阶段的可变数据写入 `runtime.State`，不要写入共享 Stage 资源。`runtime.Session.Context` 提供 LevelId、Level、Manager、Progress 和 Data；`SetValue/GetValue` 操作会话字典。

内置 Stage 对原生 `QuickLevelKitLevel` / `QuickLevelKitManager` 使用强类型调用。自定义 `Node` / `GodotObject` 适配器可以提供 PascalCase 的 `BeginPreparation`、`PreparationCompleted`、`BeginRunning`、`AdvanceLevel`、`ReloadLevel`、`FinishFlow` 方法或信号。不要求运行 GDScript。

同步完成准备、连续通关、重载和 Stop 在回调内发生时，会话按队列处理，并防止旧回调覆盖新会话；退出准备阶段会断开未触发的准备完成订阅。

## Manager API 与信号

- `SetCatalog/GetCatalog`、`LoadFirstLevel`、`LoadLevel(id)`、`ContinueGame`。
- `RequestTransition(dictionary)`、`AdvanceLevel`、`ReloadLevel`、`FinishFlow`。
- `GetCurrentLevel/Id/StageId`（完整方法名：`GetCurrentLevel`、`GetCurrentLevelId`、`GetCurrentStageId`）、`IsFlowActive`。
- `GetProgress`、`SetProgressStore/GetProgressStore`、`SaveProgress`、`ResetProgress`、`IsLevelUnlocked`、`UnlockLevel`。
- `ConfigureHosts(active, staging)` 可替换默认 `LevelContainer/ActiveLevelHost` 和 `StagingLevelHost`，两个 Host 必须不同且有效。
- `SessionStarted`、`SessionStopped`、`StageChanged`、`LevelLoaded`、`TransitionRejected`、`ProgressChanged`、`FlowError` 为 C# 事件形式的 Godot signals。`StageChanged` 包含异步准备完成的阶段变化。

`LoadLevel` 校验目录与目标，不成功则返回 false；如果信号回调明确用另一项加载/停止操作取代当前加载，当前加载也返回 false，不覆盖较新的请求。有效流程开始后立即同步通关仍视为成功。

Catalog 提供 `GetDefinition/At`、`FindIndex`、`GetFirstLevelId/GetLastLevelId`、`GetFlowFor`、`ResolveNext`、`GetValidationErrors/Validate`。`ResolveNext` 字典字段 `status` 为 `NextStatus.Available = 0`、`Final = 1`、`Unknown = 2`，还包含 `level_id`、`definition`。错误字典键保持原版 `code/index/level_id/stage_id/target_stage_id`。

## 资源与编辑器

资源导出字段可在 Inspector 编辑，并用 `ResourceSaver.Save` 保存 `.tres`。为允许原 GDScript 源码共存，C# 类型没有 `[GlobalClass]`，不在全局新建资源菜单注册同名项。可以用代码创建并保存 Catalog，或新建 Resource 后绑定相应 C# 脚本，再选中 Catalog 资源使 Dock 显示它。示例：

```csharp
ResourceSaver.Save(catalog, "res://levels/catalog.tres");
var saved = GD.Load<QuickLevelKitCatalog>("res://levels/catalog.tres");
```

Dock 支持目录显示、增删/排序/校验 API、所选关卡默认 Flow 创建。Up/Down/Remove/Flow/Validate 按钮作用于当前 Catalog；新增 Definition 和场景引用可在 Inspector 编辑 `Levels`。Stage/Route 的属性也在 Inspector 中编辑。校验会实例化场景根并释放，构造器应避免游戏副作用。

## 存档

`QuickLevelKitProgress` 保留 `CurrentLevelId`、`HighestUnlockedLevelId`、`LevelStates`。`ToDictionary/FromDictionary`、`DuplicateProgress`、`GetLevelState/SetLevelState` 深拷贝字典。静态 Definition.Metadata 不会写入进度。

默认 `QuickLevelKitConfigFileProgressStore` 路径仍为 `user://quick_level_kit_progress.cfg`，section `quick_level_kit`，key `progress`，与 GDScript schema 兼容。缺失、损坏或字段形状错误时返回空进度。该默认实现不提供加密、多存档槽或事务写入。

```csharp
manager.SetProgressStore(new QuickLevelKitConfigFileProgressStore("user://my_levels.cfg"));
manager.GetProgress().SetLevelState("level_001", new Godot.Collections.Dictionary { ["stars"] = 3 });
Error result = manager.SaveProgress();
```

自定义存储继承 `QuickLevelKitProgressStore`，覆写 `LoadProgress`、`SaveProgress`、`Clear`。检查返回的 `Error`；自动保存失败不会回滚已经发生的关卡切换。

## 范围与验证

仅支持单线顺序关卡。视觉转场、音频、HUD、输入锁、时间缩放与玩法规则由项目或自定义 Stage 负责；不加入章节、分支、多路线、成就、最佳成绩或多存档槽。

[迁移对应表](../docs/MIGRATION.md) · [验证记录](../docs/VALIDATION.md) · [测试入口](../../../tests/quick_level_kit/README.md)
