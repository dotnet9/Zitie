# design — UI 效果图

新中式（宣纸 · 墨 · 朱砂）重设计的 HTML 效果图，用应用内嵌的 LXGW 楷体渲染，尺寸 1240×840 与应用主窗口一致。

## 目录

- `mockups/home.html` — 主页（模板库）效果图：左侧导航栏 + 胶囊筛选片 + 模板卡片
- `mockups/editor.html` — 编辑页效果图：图标工具栏 + 参数标签页 + 书桌预览舞台
- `server.mjs` — 本地预览服务器（字体需经 HTTP 加载，file:// 直开不生效）

## 预览

```bash
node design/server.mjs
```

然后访问 <http://127.0.0.1:8399/design/mockups/home.html>（编辑页把文件名换成 `editor.html`）。

卡片悬停上浮、整卡遮罩等交互效果需要打开页面实际体验。
