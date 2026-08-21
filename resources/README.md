# Resources

内置资源集中放在这里，并通过 `src/Zitie.Desktop/Zitie.Desktop.csproj` 链接到应用输出目录的 `resources/` 下。

- `modules/`：字帖模板源码目录；每个子目录放 `module.yml` 与 `assets/`。发布时由 `scripts/pack_modules.ps1` 压缩成同名 `.zi` 包。
- `texts/`：可直接生成字帖的练习文本，语文和英语已覆盖小学一年级到高中三年级。
- `pinyin/`：拼音词表 YAML。
- `textbooks/`：教材版本索引与教材资源元数据 YAML。

`textbooks/textbook-editions-2026.yml` 来自国家中小学智慧教育平台当前在线电子教材资源快照，仅作为教材版本索引使用。
`texts/chinese-all-grades.md` 与 `texts/english-all-grades.md` 是全年级同步拓展练习内容；其他 Markdown 文件为诗词、蒙学、名言和既有年级补充资源。
