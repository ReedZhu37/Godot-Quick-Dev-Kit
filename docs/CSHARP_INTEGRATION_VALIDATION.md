# C# 转译合并验证（2026-11-11）

## 来源与范围

- main 基线：`9f0a4640d78d284cc22929f1acb407caf95b299a`
- Input：`93101268a64722f923d7fd21c8c5aa06c8d50c76`
- Audio：`dfa3a469f4ef44de4cde8a5208bf946eaa3834e1`
- Database：`c769f95c40d2d042f3247dec7fa5970ed14c5a2f`

保留三条功能分支历史，通过正常合并整合；只解决共同文档和忽略配置冲突。原模块实现、UID 与 gds/csharp 路径保留，除下述必要的空值类型注解修复外，不改运行逻辑。根工程新增全部 C# 模块编译覆盖；README、CSHARP.md、插件显示名明确区分语言。Quick Level Kit 只有 GDScript，VFX 只有 C#。

未合入单独的整仓目录迁移分支，未修改 TapTap2026，未发布安装包或新增许可证。

## 本轮验证状态

验证环境：Linux x86_64，官方 Godot 4.7.2 Mono，.NET SDK 9.0.318，headless / Dummy 音频。Godot 归档 SHA256 与官方 release 元数据一致：`129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247`。

- 全部四个 C# 模块一起 Debug / Release / ExportRelease 构建：均 0 warnings / 0 errors，使用 `-warnaserror`。
- 根工程编辑器导入及默认 VFX 场景启动：退出 0，无 SCRIPT ERROR / ERROR。
- 同一根工程、同一程序集依次执行 Input 490 断言、Audio 186 检查、Database 75 案例、VFX 20 组 174 断言：全部通过。不是只依赖各自隔离工程。
- Input 隔离全套：490 断言、13 项实际编辑器启停/互斥检查、两语言示例、GDScript 与 C# 存档互操作全部通过；空值注解修正后完整重跑通过。
- Audio 隔离全套：186 检查、跨五个编辑器进程 68 项启停/持久化检查通过，包含实际生成波形与总线路由检查。
- Database 隔离全套：75 案例，编辑器冷导入/构建后/持久化/重开共 3/18/19/20 断言，GDScript 五组、双语言互读 12 断言、跨进程存档种入/读取通过。
- VFX 隔离全套：冷启动 8、构建后 17 断言，20 组 174 断言通过。
- Level 原 GDScript 九组测试通过；本次仅显示名增加 (GDScript)，没有新增 C# 版本。
- 新增四项 Python 检查通过：双版本插件语言名、单语言标记、全部 C# 文件被根工程恰好编译一次、两个入口文档的本地链接存在。
- Python 编译、Shell 语法和 `git diff --check` 通过；独立审查检查了类名、UID、资源路径和安装说明。

## 合并时发现的兼容问题

根工程启用 nullable，而原 Input 隔离工程关闭 nullable。`QuickInputEventCodec.Decode` 明确允许返回 null，却没有相应类型注解，合并构建出现 8 条 CS8603。现在该文件显式启用 nullable，并将签名改为 `InputEvent? Decode(Dictionary? data)`，不更改判断或返回行为。修复后联合构建与 Input 全套重跑均通过。

Input 测试入口增加 SDK / framework 参数，保留原 4.6.3 / net8.0 默认值；本轮显式使用 4.7.2 / net9.0，避免跨 Godot 版本错误读取 bundled NuGet 包。

## CI 与未验证范围

自动检查遵循现有主线的“自动编译、完整 Godot 手动触发”方式。新增工具箱集成检查覆盖全部 C# 源码、语言标记和文档链接；Input 的历史自动运行流程调整为手动，并增加适用 4.7.2 下载包的校验。完整四模块 Godot 回归可从 `Quick Dev Kit C# integration` 手动运行。

本文记录本次云端实际执行结果。远端 CI 应以发布提交对应的 Actions 结果为准，不把本地通过写成远端已通过。

未重新进行 GUI 人工试玩、实体手柄/键盘布局、实际音箱试听、Windows/macOS/移动平台或导出包验证。Database 故障注入与 VFX 非法配置用例会刻意输出诊断；通过判定检查了成功标记、退出 0 和无脚本/未处理异常，不能把预期诊断隐藏为完全无日志。
