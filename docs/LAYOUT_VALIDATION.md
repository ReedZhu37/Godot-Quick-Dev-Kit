# 目录迁移验证（2026-10-18）

基线 main：`9f0a4640d78d284cc22929f1acb407caf95b299a`。

## 通过

- 独立代码审查：两语言 addons 下的非 Markdown 文件与原 main 完全相同，未做运行时转码或功能更改。
- Python 3 项测试：两个语言 bundle + 5 个单模块 ZIP 的确定性、目录隔离、资源引用、解压到空工程、场景/资源/工程路径；根目录没有 project.godot / csproj。
- GDScript：Godot 4.6.3，全部 18 个回归脚本（Audio 2、Database 5、Input 2、Level 9）退出 0 且有通过标记，无脚本解析错误。
- GDScript 运行时安装包：四模块单独解压到空工程，以及全量包，实际启用插件并 headless 编辑器导入通过；没有 demos/tests 依赖。
- C#：Godot 4.7.2 .NET / .NET 9，隔离工程 Debug、Release 编译均 0 警告 / 0 错误；cold-load 8 断言，built-load 17 断言通过。
- VFX：20 组运行时回归，174 断言，0 失败；包括迁移后的演示资源和序列化 round-trip。
- C# 运行时包：全量包和 VFX 单模块包各自在空工程编译并启用插件导入通过，0 编译警告 / 0 错误。
- Shell 语法、Python 编译和 `git diff --check` 通过。

## 基线与环境说明

- Audio 回归退出时仍有 ObjectDB leak 警告；在原 main 相同测试上复现。Database 破损文件/恢复失败测试刻意输出错误，同样在原 main 复现，并有测试通过标记。本次未把这些既有行为当作目录迁移缺陷修改。
- 云环境初次运行因用户数据目录不可写、dotnet 未在 PATH 中失败；配置可写 XDG 目录和 SDK PATH 后重新运行。最初直接 Debug build 有 NuGet 缓存权限警告；最终隔离工程 Debug/Release 为零警告。上述“通过”均指修正执行环境后的运行。
- 未进行 GUI 交互/画面人工试玩、跨平台验证、未实现语言的等价性验证、Asset Library 验收或公开发布；没有添加 LICENSE。
- 远端 CI 以分支 commit 的 Actions 状态为准；本文件记录本地可复现检查，不提前承诺远端 CI 结果。
