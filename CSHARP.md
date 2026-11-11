# Godot Quick Dev Kit — C# / .NET 版

**这是 C# 版入口。需要 Godot .NET 编辑器，不适用于普通 Godot 编辑器。**

main 已包含下面四个原生 C# 模块。每个模块的代码位于 `csharp/`，编辑器插件名也带 **(C#)**，与 `gds/` 和 **(GDScript)** 明确区分。

| 模块 | C# 源码与说明 | 启用时选择 |
| --- | --- | --- |
| Input | [addons/quick_input/csharp](addons/quick_input/csharp/README.md) | Quick Input (C#) |
| Audio | [addons/quick_audio_manager/csharp](addons/quick_audio_manager/csharp/README.md) | Quick Audio Manager (C#) |
| Database | [addons/quick_database/csharp](addons/quick_database/csharp/README.md) | Quick Database (C#) |
| VFX | [addons/quick_vfx_manager/csharp](addons/quick_vfx_manager/README.md) | Quick VFX Manager (C#) |

**Quick Level Kit 暂无 C# 版**，不要将它误认为上述 C# 集合的一部分。

## 安装到自己的项目

1. 使用与你的 Godot 版本匹配的 .NET SDK；本仓库集成验证环境是 **Godot 4.7.2 .NET / .NET 9**。
2. 只复制需要的 `addons/<模块>/csharp/`，保留整个路径及其中的资源，不复制同模块的 `gds/`。
3. 在目标项目 Build，然后启用带 **(C#)** 的插件。缺少程序集时先编译，不要改选 GDScript 来绕过。
4. 同一模块两种语言二选一。切换已有 GDScript 版本时先停用并备份，更新或转换脚本/Resource 引用；确认没有旧依赖后再移除 GDScript 文件。Audio 转换器需要迁移期间保留两套文件，但只能启用一个插件。

三个转译模块各自拥有命名空间：`QuickDevKit.QuickInput`、`QuickDevKit.QuickAudio`、`QuickDev.Database`。具体 API、Resource 类型和转换限制见各模块 C# 文档。

## 在仓库里运行示例

用 Godot .NET 打开根目录 `project.godot`，Build 后选择场景按 F6：

- [Input 示例](addons/quick_input/csharp/examples/basic_usage.tscn)
- [Audio 示例](addons/quick_audio_manager/csharp/examples/basic_usage.tscn)
- [Database 交互示例](addons/quick_database/csharp/examples/BasicUsage.tscn)
- [VFX Lab](addons/quick_vfx_manager/csharp/examples/VfxLab.tscn)，也是默认 F5 场景

示例是否使用 Autoload 按模块说明配置。根项目只默认启用 VFX，不改变你自己的游戏输入、音频或存档配置。

[返回语言对照与安装说明](README.md) · [本次集成验证](docs/CSHARP_INTEGRATION_VALIDATION.md)
