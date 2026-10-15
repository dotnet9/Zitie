# 发布流程规范（RELEASE）

> 本文档是各仓库统一的标准发布流程。**每次发版都按本文档执行**，避免出现
> 「Release 页只有 Full Changelog 链接」「安装包缺失」「版本号不一致」这类不规范情况。

## 1. 版本号约定

- 版本号唯一来源：根目录 `Directory.Build.props` 的 `<Version>`，当前为 `0.2.4`，对应 `v0.2.4`。
- 发布 tag 必须与该版本一致：`<Version>1.2.3</Version>` ↔ tag `v1.2.3`。
- README 的「当前版本」行（如有）同步更新。
- 更新日志：有 `UpdateLog.md` 的仓库，发布前新增对应版本条目（`## 版本 (YYYY-MM-DD)`）。

## 2. 标准发布流程

```text
1. 改版本号（Directory.Build.props / csproj）+ 更新 UpdateLog.md + 同步 README 版本行
2. 直接在 main 按修改范围使用简体中文规范提交，不创建工作分支。
3. 打附注 tag —— tag 的附注消息就是 GitHub Release 的发布说明（见下节）：
       git tag -a v0.2.4 -F .github/release-notes/v0.2.4.md
4. 推送 main 与新 tag；已发布标签不得移动。
5. CI（release.yml）自动：测试 → 全平台 NativeAOT 发布 → 制作安装包 → 创建 GitHub Release
6. 到 Release 页确认：说明可读、各平台安装包齐全（win exe / linux deb / mac dmg）
```

## 3. 发布说明规范（重要）

Release 优先使用精选说明文件，标签附注复用同一文件。当前说明为 `.github/release-notes/v0.2.4.md`。打 tag 时同样写清楚：

```bash
git tag -a vX.Y.Z -m "应用名 X.Y.Z

本版本做了什么（第一段概览）

- 变更要点 1
- 变更要点 2"
```

- 工作流的「准备发布说明」步骤会按以下优先级取内容：
  1. 精选说明 `.github/release-notes/vX.Y.Z.md`（存在则优先）；
  2. **附注 tag 消息**（`git tag -l vX.Y.Z --format='%(contents)'`）；
  3. 两者均缺失时必须终止发布，不允许仅生成版本标题或 Full Changelog。
- 不确定时，发布后打开 Release 页检查说明是否可读。

## 4. 平台与安装包矩阵

| 平台 | 产物 | 说明 |
| --- | --- | --- |
| win-x64 | `.exe` 安装器 | NativeAOT |
| linux-x64 / linux-arm64 | `.deb`（amd64/arm64） | NativeAOT；arm64 在 arm runner 上原生编译 |
| osx-x64 / osx-arm64 | `.dmg` | NativeAOT；保留符号（Apple ld_classic 不支持压缩调试段） |

- 统一配方：`-p:PublishAot=true -p:PublishTrimmed=true -p:PublishSingleFile=false
  -p:IlcGenerateCompleteTypeMetadata=true -p:IlcTrimMetadata=false -p:IlcSingleThreaded=true`。
- Linux runner 需 `clang zlib1g-dev`；不要在 x64 runner 上交叉编译其他架构的 AOT。
- 平台相关功能未适配时，应用内以「当前平台功能正在开发中」友好提示，不阻塞发布与启动。

## 5. 发布后检查

- Release 页：说明可读，五个平台安装包与对应 `.sha256` 共十项资产齐全。
- 安装包抽查：至少在 Windows 本机跑一次发布产物。

`scripts/verify_release_installers.ps1 -Version 0.2.4` 按 Zitie 产品名、完整版本、RID 和安装格式精确校验，重新计算实际文件摘要，并拒绝空包、缺失校验文件和任何多余文件。`scripts/tests/release_installers_test.ps1` 使用隔离受控文件覆盖正反场景。

本机执行正式 `scripts/publish.ps1 -RuntimeIdentifier win-x64 -Version 0.2.4` 后，运行发布目录 `artifacts/publish/win-x64/Zitie.Desktop/Zitie.Desktop.exe --selfcheck`。独立原生界面验证器的程序集名为 `Zitie.UiVerification`，发布时使用 `PublishAot=true`、完整反射元数据与单线程 ILC；运行时把正式发布目录作为第二个参数，以验证真实分发的 `.zi` 模板模块。同一仓库的构建、测试和 AOT 编译依次执行。

本机可按 `temp_nugets` 环境变量使用中间包，正式提交只保留公开 NuGet 引用，不提交本地源或跨仓库项目引用。离屏 UI 和正式自检不能代替普通桌面、系统文件选择与真实安装验收。
