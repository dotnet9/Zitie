# design — UI 原型图

新中式（宣纸 · 墨 · 朱砂）风格的 HTML 原型图，用应用内嵌的 LXGW 楷体渲染，尺寸 1240×840 与应用主窗口一致。三个页面共享同一套窗口外壳（自定义标题栏 + 侧边导航栏），对应应用的三个界面。

## 目录

- `mockups/home.html` — 主页（模板库）：标题栏 + 侧边导航 + 胶囊筛选 + 模板卡片网格 + 底部状态栏
- `mockups/editor.html` — 编辑页：图标工具栏（返回 / 模板切换 / 文件操作 / 导出）+ 左侧参数标签页 + 书桌预览舞台 + 分页与状态栏
- `mockups/content-drawer.html` — 编辑页 · 内容资源抽屉：右侧滑出抽屉，含关键词搜索、五级筛选、最近使用与高亮搜索结果
- `server.mjs` — 本地预览服务器（字体需经 HTTP 加载，`file://` 直开不生效）

## modules — 模块原型（单模板深设计）

只画字帖模板纸张本身，不含窗口外壳与参数面板；一个模板一个 HTML。

- `modules/chunlian.svg` — 春联模板（带米字格，矢量裸图零留白）：横批 + 右左双联 + 福字斗方，朱砂主线/浅粉格线/描红字；坐标系 200×264（单位≈mm），应用排版时再决定纸张与页边距
- `modules/chunlian_no_grid.svg` — 春联模板（无格线版）：布局、描边、配色与 `chunlian.svg` 完全一致，仅去掉米字格辅助线与字间虚线分隔

## 预览

```bash
node design/server.mjs
```

然后访问：

- 主页：<http://127.0.0.1:8399/design/mockups/home.html>
- 编辑页：<http://127.0.0.1:8399/design/mockups/editor.html>
- 内容抽屉：<http://127.0.0.1:8399/design/mockups/content-drawer.html>
- 春联模板：<http://127.0.0.1:8399/design/modules/chunlian.html>

卡片悬停上浮、整卡遮罩、抽屉滑入等交互效果需要打开页面实际体验。
