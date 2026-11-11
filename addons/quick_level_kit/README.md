# Quick Level Kit：选择语言

两套独立实现，功能为目录、顺序关卡、可扩展 Stage/Route 流程、进度存储与编辑器 Dock。

- [C# / .NET 版](csharp/README.md)：复制 `addons/quick_level_kit/csharp`，先 Build，启用 **Quick Level Kit (C#)**。
- [GDScript 版](gds/README.md)：复制 `addons/quick_level_kit/gds`，启用 **Quick Level Kit (GDScript)**。
- 两种实现共享 `/root/QuickLevelKit`，同一项目二选一启用。插件保留不属于自己的同名 Autoload。
- 旧版本升级：备份并停用旧插件；GDScript 原文件现移至 `gds/`，更新插件、Autoload 和脚本路径。UID 保留，但显式 `res://` 路径仍需更新。
- 进度 ConfigFile schema 保持兼容；场景脚本与 Resource 并非跨语言可互换，需要重新绑定/重建 C# 资源。保留原文件至引用迁移完成。

[功能对应与迁移说明](docs/MIGRATION.md) · [验证记录](docs/VALIDATION.md)
