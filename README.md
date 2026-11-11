# Godot Quick Dev Kit

面向 Godot 4.x 的模块化开发工具箱。**Quick Input、Quick Audio Manager、Quick Database 已在 main 提供 GDScript 与原生 C# 两套实现。**

## 先选择语言

- **[C# / .NET 版入口](CSHARP.md)**：需要 **Godot .NET 编辑器** 和匹配的 .NET SDK。目录名为 `csharp/`，编辑器插件显示名明确带 **(C#)**。
- **GDScript 版**：使用下表的 GDScript 链接。目录名为 `gds/`，插件显示名带 **(GDScript)**；Quick Level Kit 目前只有 GDScript。
- 同一模块**二选一安装和启用**。不要把整个 `addons/` 当成纯 C# 包或纯 GDScript 包复制进游戏。

| 模块 | GDScript | C# / .NET | 能力 |
| --- | --- | --- | --- |
| Quick Input | [GDScript](addons/quick_input/gds/README.md) | **[C#](addons/quick_input/csharp/README.md)** | 输入绑定、多槽位、冲突策略、捕获 UI 与存档 |
| Quick Audio Manager | [GDScript](addons/quick_audio_manager/gds/README.md) | **[C#](addons/quick_audio_manager/csharp/README.md)** | Resource 音频资产、轨道、总线、音量及播放管理 |
| Quick Database | [GDScript](addons/quick_database/gds/README.md) | **[C#](addons/quick_database/csharp/README.md)** | 数据文档、事务存储、仓储与多存档槽 |
| Quick VFX Manager | 暂无 | **[C#](addons/quick_vfx_manager/README.md)** | 调用者组件、组合特效生命周期、世界/附着播放；无 Autoload、无池化 |
| Quick Level Kit | [GDScript](addons/quick_level_kit/README.md) | 暂无 | 关卡目录、流程、阶段、状态与进度 |

没有标记为支持的语言版本尚未实现；不能据此表推断五个模块都已有双版本。

## 安装单个模块

保留所选版本的完整相对路径。例如只安装 C# Quick Input：

```text
你的游戏/
├── project.godot
├── 你的游戏.csproj
└── addons/
    └── quick_input/
        └── csharp/        # 从本仓库复制整个目录，保留这一层
```

GDScript Input、Audio、Database 同理复制各自的 `gds/`；Level 复制 `addons/quick_level_kit/`。VFX 复制 `addons/quick_vfx_manager/`，它仅含 C# 实现与文档。

1. 选择模块和语言，复制对应目录，不要把 `csharp/` 或 `gds/` 的内容摊平。
2. C# 先在 Godot .NET 中 **Build**，再到 **Project → Project Settings → Plugins** 启用名称带 **(C#)** 的插件。普通 Godot 不能运行 C#。
3. GDScript 启用名称带 **(GDScript)** 的插件。不要同时启用同模块的两种实现，它们共享 Autoload 名称。
4. 安装或升级前备份项目；切换语言时先停用旧插件并备份，更新或转换 Autoload、脚本路径、场景与 Resource 引用；确认不再依赖旧实现后才移除旧文件。存档兼容不代表脚本 Resource 可直接互换。

旧 main 的 Input、Audio、Database GDScript 文件现已移入各模块 `gds/`。引用旧 `res://addons/quick_*/runtime/...` 路径的项目，需要按对应模块 README 更新到 `.../gds/runtime/...`；原 GDScript 脚本 UID 保留。

## 根目录演示工程

根目录 `project.godot` 是 **C# 工具箱开发/验证工程**，默认运行 VFX Lab。使用 **Godot 4.7.2 .NET + .NET 9** 打开，先 Build 再 F5。

`QuickVfxDemo.csproj` 保留原名称与程序集名，并一起编译 Input、Audio、Database、VFX 的 C# 代码，检查集成冲突。默认仍只启用 VFX 插件，不自动启用其他 Autoload。其他模块的示例路径见 [C# 入口](CSHARP.md)。

根工程不是要复制进游戏的安装包。GDScript 用户不必打开这个 .NET 演示工程。

## Autoload

| 模块 | 名称 |
| --- | --- |
| Quick Input，任一语言 | `QuickInput` |
| Quick Audio Manager，任一语言 | `QuickAudioManager` |
| Quick Database，任一语言 | `QuickDatabase` |
| Quick Level Kit | `QuickLevelKit` |
| Quick VFX Manager | 无 |

Input、Audio、Database 的双版本插件会保护不属于自己的同名 Autoload；遇到占用应先检查配置，不要直接删除项目自己的服务。Level Kit 不提供相同的占用保护，启用或停用前必须先检查 `QuickLevelKit` 名称。

## 验证

自动 CI 检查全部 C# 模块的 Debug / Release 编译，以及语言标记、目录和文档链接。完整 Godot 运行、编辑器生命周期及双语言互操作检查由手动工作流触发，也可在本地执行：

```bash
python3 -m unittest discover -s tests -p 'test_*.py' -v
dotnet build QuickVfxDemo.csproj -c Debug
dotnet build QuickVfxDemo.csproj -c Release
python3 tests/run_quick_input.py --backend all --godot "$GODOT" --sdk-version 4.7.2 --framework net9.0
python3 tests/run_quick_audio.py --backend all --godot "$GODOT"
GODOT="$GODOT" bash tests/quick_database/run.sh
GODOT="$GODOT" bash tests/quick_vfx_manager/run.sh
```

`GODOT` 指向 Godot 4.7.2 .NET，`dotnet` 应可从 PATH 找到。Input 原独立测试默认仍是 Godot 4.6.3 .NET / .NET 8；在 4.7.2 工具箱环境中使用上述显式参数。

- [本次合并验证](docs/CSHARP_INTEGRATION_VALIDATION.md)
- 历史模块验证：[Input](addons/quick_input/VALIDATION.md)、[Audio](addons/quick_audio_manager/VALIDATION.md)、[Database](addons/quick_database/docs/VALIDATION.md)、[VFX](addons/quick_vfx_manager/docs/VALIDATION.md)

各模块的兼容范围、资源转换与行为差异以对应 README 为准。Linux headless 通过不等同于 Windows/macOS 导出、实体输入设备或真人试听验收。

## 仓库与分发

源码在同一个仓库维护，语言通过目录、文档入口和插件名称明确区分。本次只整合三个现有 C# 功能分支，没有合入 `chore/isolate-language-projects` 的整仓目录迁移，也未发布 Release 或 Asset Store 包。

## 许可证

仓库尚未声明正式开源许可证。本次不更改授权范围；使用、修改或再分发前请确认仓库作者授权。

## 作者

- 原文档作者：[@RedisTKey](https://github.com/RedisTKey)
- 当前仓库：[ReedZhu37/Godot-Quick-Dev-Kit](https://github.com/ReedZhu37/Godot-Quick-Dev-Kit)
