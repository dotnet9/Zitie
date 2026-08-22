# Zitie 字帖生成器

Zitie 是基于 .NET 10、Avalonia、Semi.Avalonia 和 SkiaSharp 的桌面字帖生成器，适合生字描红、临摹、古诗抄写、拼音练习和自定义文本排版。

## 功能

- 米字格、田字格、回宫格、方格、九宫格、英文四线三格和拼音四线格。
- 描红、临摹、空心双钩、按词分组、横排/竖排和拼音标注。
- A4 纵向、A4 横向、A3 纵向、Letter 纵向，以及 0-40 mm 页边距。
- 纸张底纹、格线/文字十六进制颜色、字体和页面装饰边框。
- 预览当前页，导出多页 PDF、单页 PNG、批量 PNG，并调用系统打印队列。
- 内容资源覆盖小学一年级到高中三年级，独立抽屉支持按学科、版本、年级、学期、单元筛选、关键词高亮搜索和最近使用。
- 模板库支持关键词搜索、分类筛选、模板块缩放、Ctrl+滚轮缩放和刷新用户模板目录。
- 软件字号可用 12-30 的滑块按设备显示效果调整，并保存到本机设置。
- `.zitie.json` 文档保存/打开，内置模板由 223 个样式示例图驱动。

## 使用

```powershell
dotnet run --project src/Zitie.Desktop/Zitie.Desktop.csproj -f net10.0
```

在模板页选择一个模板进入编辑器。编辑器顶部命令栏提供打开、保存、另存、保存模板、导出和打印；左侧属性面板修改版式，右侧预览会实时更新。

内置模板清单位于 `resources/module-styles/styles.json`，示例图位于仓库根目录 `docs/modules`。桌面工程会把这些图片链接到输出目录 `resources/module-styles/images`，模板库按示例图标题和标签推断可编辑版式。

内置资源统一位于仓库根目录 `resources`：

- `resources/module-styles`：223 个样式模板元数据。
- `docs/modules`：样式模板示例图。
- `resources/texts`：可直接生成字帖的练习文本 Markdown，含语文/英语小学到高中全年级同步拓展内容。
- `resources/pinyin`：拼音词表 YAML。
- `resources/textbooks`：2026 教材版本索引，覆盖小学、初中、高中在线教材元数据。

## 文档格式

字帖文档的顶层结构如下：

```json
{
  "version": 1,
  "moduleId": "poem-wuyan",
  "spec": {
    "text": "床前明月光",
    "grid": "tian",
    "mode": "copy",
    "page": {
      "widthMm": 210,
      "heightMm": 297,
      "marginTopMm": 15,
      "marginBottomMm": 15,
      "marginLeftMm": 15,
      "marginRightMm": 15
    }
  }
}
```

运行时拼音缓存不会写入文档；颜色、字体、纸张和页边距会完整保存。

## 开发与验证

```powershell
dotnet restore Zitie.slnx
dotnet test Zitie.slnx -c Release --no-restore
dotnet build Zitie.slnx -c Release --no-restore
```

发布脚本位于仓库根目录，支持 `win-x64`、`linux-x64`、`linux-arm64`、`osx-x64` 和 `osx-arm64`：

```powershell
pwsh ./scripts/publish_demo.ps1 -RuntimeIdentifier win-x64
```

Windows 发布启用 NativeAOT；macOS/Linux 使用不裁剪的单文件发布，以兼容 Avalonia、Prism 和日志组件的反射绑定。

## 项目结构

- `src/Zitie.Core`：UI 无关的规格模型、格子布局和分页引擎。
- `src/Zitie.Avalonia`：预览控件、Skia 渲染器、PDF/PNG 导出和内嵌字体。
- `src/Zitie.Desktop`：Avalonia 工作台、模板目录、文本库和文档操作。
- `resources`：桌面工程以链接方式引用的内置资源。
- `tests`：布局、导出、文档持久化和编辑状态回归测试。

项目采用 MIT 许可证；内嵌霞鹜文楷字体的许可证见 `src/Zitie.Avalonia/Fonts/OFL.txt`。
