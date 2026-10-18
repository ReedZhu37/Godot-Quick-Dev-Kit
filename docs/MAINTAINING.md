# 双语言维护契约

- 一个仓库、两个独立 Godot 资源根；运行时路径保持 `res://addons/quick_*`。已有 VFX 的 `csharp/` 内层路径保留，避免破坏已安装项目。
- 同一模块两种实现遵循相同概念、资源字段及公开行为契约，但不要求语法或序列化脚本路径相同。不得在一个 Godot 工程中扫描/注册同模块的两份实现。
- demos 与 tests 按语言和模块隔离，资源引用以各语言工程为根。安装包只取 addons；运行代码不得引用 demos/tests。测试与 demo 依赖运行时，反向依赖不允许。
- 当前未出现必须跨语言共享的资源，因此没有引入空 shared 目录。将来共享纯数据/素材时，在打包 staging 中复制到稳定的 `addons/<module>/...` 路径并检查引用；禁止工程外 `res://` 引用或跨实现目录扫描。
- 新增实现必须更新 README 矩阵，添加对应 demo、测试与单模块安装验证后再标记支持。未合并分支不算已经发布。
- `tools/package.py` 从同一 Git revision 构建语言 bundle 或单模块 ZIP，检查运行时资源引用，输出顺序和 ZIP 时间戳固定；不会创建 Release。维护者以同一版本/tag 构建两份包，核对内容、兼容矩阵与许可证后才发布。
- `tests/test_packages.py` 覆盖全量/单模块 ZIP、解压到空工程、确定性与资源路径；`tests/check_install.py` 补充真实 Godot 的插件启用/导入验证，C# 先编译。其通过不等同于人工交互试玩。
- `.github/workflows/packages.yml` 检查打包和 GDScript；`quick-vfx.yml` 自动检查 C# Debug/Release，现有昂贵 .NET 编辑器 cold-load/运行检查仍由 workflow_dispatch 触发。
- 仓库尚无 LICENSE；不要替作者决定授权或未经授权发布到 Asset Library。
