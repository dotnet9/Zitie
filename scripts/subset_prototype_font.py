"""为单文件原型生成同源字体子集；需要 fonttools[woff]。"""
from pathlib import Path
from fontTools import subset

root = Path(__file__).resolve().parent.parent
assets = root / "design/assets"
sources = [assets / "catalog.json", assets / "app.js", root / "scripts/prototype-source/index.template.html", *sorted((root / "src/Zitie.Desktop/I18n").glob("*.json"))]
text = "".join(path.read_text("utf-8-sig") for path in sources) + "".join(chr(code) for code in range(32, 127))
options = subset.Options()
options.flavor = "woff2"
font = subset.load_font(str(root / "src/Zitie.Avalonia/Fonts/LXGWWenKai-Regular.ttf"), options)
subsetter = subset.Subsetter(options=options)
subsetter.populate(text=text)
subsetter.subset(font)
subset.save_font(font, str(assets / "wenkai-subset.woff2"), options)
print("已生成字帖原型字体子集；许可见 design/assets/OFL.txt。")
