# Godot Quick Dev Kit

面向 Godot 4.x 的模块化工具箱。每个模块独立安装；仓库按语言隔离开发工程，仓库根目录不再是 VFX .NET 示例工程。

## 当前实现

以 main 中已有代码为准，没有把未合并功能分支当作已交付版本，也没有进行语言转码。

| 模块 | GDScript | C# |
| --- | --- | --- |
| Quick Audio Manager | [已有](implementations/gdscript/addons/quick_audio_manager/README.md) | 未纳入 main |
| Quick Database | [已有](implementations/gdscript/addons/quick_database/README.md) | 未纳入 main |
| Quick Input | [已有](implementations/gdscript/addons/quick_input/README.md) | 未纳入 main |
| Quick Level Kit | [已有](implementations/gdscript/addons/quick_level_kit/README.md) | 尚无实现 |
| Quick VFX Manager | 尚无实现 | [已有](implementations/csharp/addons/quick_vfx_manager/README.md) |

现有 C# 开发分支：[Audio](https://github.com/ReedZhu37/Godot-Quick-Dev-Kit/tree/feat/quick-audio-csharp)、[Database](https://github.com/ReedZhu37/Godot-Quick-Dev-Kit/tree/feat/quick-database-csharp)、[Input](https://github.com/ReedZhu37/Godot-Quick-Dev-Kit/tree/feat/quick-input-csharp)。这些分支没有混入本次目录迁移；这不是对其质量或验收结果的判断。

## 仓库布局

```text
implementations/
  gdscript/
    project.godot
    addons/quick_*/        # 可安装的插件与运行资源
    demos/quick_*/         # 独立于安装包的演示
    tests/quick_*/         # 独立于安装包的回归与 fixtures
  csharp/
    project.godot
    QuickVfxDemo.csproj
    addons/quick_vfx_manager/csharp/
    demos/quick_vfx_manager/
    tests/quick_vfx_manager/
tools/package.py          # 两种语言的包、单模块包；不发布
tests/                    # 仓库级打包、安装与运行入口
docs/                     # 维护与验证说明
```

每个 `implementations/<language>/project.godot` 是一个独立资源根。只打开所需工程，不要在仓库根新建 Godot 工程、把两棵实现目录一起复制到游戏工程，或同时安装同一模块的两个实现。今后增加另一语言实现时放进对应工程的 `addons/quick_*`，不得以空目录或转发壳宣称已经支持。

## 安装

1. 选择语言，再将 `implementations/<language>/addons/` 下所需模块复制到目标项目的 `addons/`。
2. 保留模块目录名称与其内部路径。比如音频仍是 `res://addons/quick_audio_manager/...`，VFX 仍是 `res://addons/quick_vfx_manager/csharp/...`。
3. 在 Project Settings → Plugins 中启用需要的插件；VFX 需要 Godot .NET 和项目 Build。GDScript 的 Audio、Database、Input、Level 可注册各自 Autoload；VFX 无 Autoload。
4. 替换同一模块语言版本前移除旧实现并检查旧 Autoload/场景引用；不要叠加安装。不同模块可以按项目需求组合，但含 C# 时必须使用 .NET 编辑器。

不需要复制 demos、tests、开发工程的 project.godot 或 csproj。运行演示时直接打开对应语言工程，在 `demos/<module>/` 中选择场景；GDScript 示例需要的插件按模块说明启用。VFX 默认运行 VfxLab。

## 开发与验证

GDScript 自动检查使用 Godot 4.6.3；VFX 现有基线使用 Godot 4.7.2 .NET / .NET 9，具体兼容边界见模块文档。

```bash
python -m unittest discover -s tests -p 'test_*.py' -v
GODOT=/path/to/godot bash tests/run_gdscript.sh
python tests/check_install.py --language gdscript --godot /path/to/godot
dotnet build implementations/csharp/QuickVfxDemo.csproj -c Debug
dotnet build implementations/csharp/QuickVfxDemo.csproj -c Release
GODOT=/path/to/godot_mono DOTNET=dotnet bash tests/quick_vfx_manager/run.sh
python tests/check_install.py --language csharp --godot /path/to/godot_mono --dotnet dotnet
```

## 打包

同一个已验证版本/tag 生成两份语言 ZIP；它们只包含各自实际存在的模块，不承诺功能完全对等。也支持单模块包。ZIP 根目录均为 `addons/`，不含 demo、测试、工程配置和构建缓存。

```bash
python tools/package.py --language gdscript --version v0.1.0
python tools/package.py --language csharp --version v0.1.0
python tools/package.py --language gdscript --module quick_audio_manager --version v0.1.0
```

生成在 `dist/`；版本号示意，不会创建 tag 或发布 Release。CI 生成构建产物供检查，发布和 Asset Library 提交仍是单独步骤。详见 [维护契约](docs/MAINTAINING.md) 和 [本次迁移验证](docs/LAYOUT_VALIDATION.md)。

## 许可证

仓库尚未声明正式开源许可证。本次没有擅自选择许可证；使用、修改、分发及正式发布前需确认作者授权范围。

## 作者

- 原文档作者：[@RedisTKey](https://github.com/RedisTKey)
- 当前仓库：[ReedZhu37/Godot-Quick-Dev-Kit](https://github.com/ReedZhu37/Godot-Quick-Dev-Kit)
