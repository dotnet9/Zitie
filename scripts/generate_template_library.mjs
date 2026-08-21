import { existsSync, mkdirSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const root = new URL("../resources/modules/", import.meta.url);
const moduleRoot = decodeURIComponent(root.pathname).replace(/^\/(.:\/)/, "$1");
const generatedPrefix = "ref-";

for (const entry of readdirSync(moduleRoot, { withFileTypes: true })) {
  if (entry.isDirectory() && entry.name.startsWith(generatedPrefix)) {
    rmSync(join(moduleRoot, entry.name), { recursive: true, force: true });
  }
}

const xml = (body, background = "#FFFFFF") => `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 210 297">
  <rect width="210" height="297" fill="${background}"/>
${body}
</svg>
`;

const line = (x1, y1, x2, y2, color, width = 0.6, opacity = 1) =>
  `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" stroke="${color}" stroke-width="${width}" opacity="${opacity}"/>`;

function outerFrame(color, variant = 0) {
  if (variant % 3 === 1) {
    return `  <rect x="7" y="7" width="196" height="283" rx="4" fill="none" stroke="${color}" stroke-width="0.65"/>
  <path d="M7 20h8V7M195 7v13h8M203 277h-8v13M15 290v-13H7" fill="none" stroke="${color}" stroke-width="1"/>`;
  }
  if (variant % 3 === 2) {
    return `  <rect x="6" y="6" width="198" height="285" fill="none" stroke="${color}" stroke-width="0.8"/>
  <rect x="9" y="9" width="192" height="279" fill="none" stroke="${color}" stroke-width="0.3" opacity=".6"/>
  <circle cx="9" cy="9" r="1.4" fill="${color}"/><circle cx="201" cy="9" r="1.4" fill="${color}"/><circle cx="9" cy="288" r="1.4" fill="${color}"/><circle cx="201" cy="288" r="1.4" fill="${color}"/>`;
  }
  return `  <rect x="7" y="7" width="196" height="283" fill="none" stroke="${color}" stroke-width="0.55"/>
  <path d="M18 13h26M166 13h26M18 284h26M166 284h26" stroke="${color}" stroke-width="1" opacity=".65"/>`;
}

function titleOrnament(color, y = 31) {
  return `  ${line(38, y, 88, y, color, 0.55, 0.7)}
  ${line(122, y, 172, y, color, 0.55, 0.7)}
  <path d="M100 ${y}l5-3 5 3-5 3z" fill="${color}" opacity=".65"/>`;
}

function panel(color, vertical, variant) {
  const x = vertical ? 53 : variant % 2 === 0 ? 40 : 25;
  const y = 62;
  const w = vertical ? 104 : variant % 2 === 0 ? 130 : 160;
  const h = vertical ? 126 : 120;
  if (variant % 4 === 0) {
    return `  <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="9" fill="#FFFFFF" fill-opacity=".78" stroke="${color}" stroke-width="0.8"/>
  <rect x="${x + 2}" y="${y + 2}" width="${w - 4}" height="${h - 4}" rx="7" fill="none" stroke="${color}" stroke-width="0.3" stroke-dasharray="1.8 1.8" opacity=".7"/>`;
  }
  if (variant % 4 === 1) {
    const cut = 9;
    return `  <path d="M${x + cut} ${y}h${w - cut * 2}l${cut} ${cut}v${h - cut * 2}l-${cut} ${cut}h-${w - cut * 2}l-${cut}-${cut}v-${h - cut * 2}z" fill="#FFFFFF" fill-opacity=".8" stroke="${color}" stroke-width="0.8"/>`;
  }
  if (variant % 4 === 2) {
    return `  <path d="M${x} ${y + 12}q0-12 12-12h${w - 24}q12 0 12 12v${h - 12}h-${w}z" fill="#FFFFFF" fill-opacity=".8" stroke="${color}" stroke-width="0.8"/>
  <path d="M${x + 4} ${y + 14}q0-10 10-10h${w - 28}q10 0 10 10" fill="none" stroke="${color}" stroke-width="0.35" opacity=".65"/>`;
  }
  return `  <path d="M${x + 10} ${y}h${w - 20}q5 8 10 0v${h}q-5-8-10 0h-${w - 20}q-5-8-10 0v-${h}q5 8 10 0z" fill="#FFFFFF" fill-opacity=".8" stroke="${color}" stroke-width="0.8"/>`;
}

function birds(x, y, color) {
  return `  <path d="M${x} ${y}q4-4 8 0q4-4 8 0M${x + 20} ${y + 8}q3-3 6 0q3-3 6 0" fill="none" stroke="${color}" stroke-width="0.75" stroke-linecap="round" opacity=".7"/>`;
}

function mountains(colors, sun = false) {
  return `  <path d="M0 255Q30 220 60 248Q92 205 126 251Q161 224 210 261V297H0Z" fill="${colors[0]}" opacity=".24"/>
  <path d="M0 274Q35 242 72 269Q108 232 146 276Q176 250 210 272V297H0Z" fill="${colors[1]}" opacity=".32"/>
  <path d="M0 288Q52 266 100 286Q145 267 210 289V297H0Z" fill="${colors[2]}" opacity=".38"/>
  ${sun ? `<circle cx="159" cy="242" r="9" fill="${colors[3]}" opacity=".55"/>` : ""}`;
}

function willow(color) {
  let leaves = "";
  for (let i = 0; i < 7; i++) {
    const x = 36 + i * 18;
    const y = 21 + (i % 3) * 7;
    leaves += `  <ellipse cx="${x}" cy="${y}" rx="5" ry="1.3" transform="rotate(${i % 2 ? -25 : 25} ${x} ${y})" fill="${color}" opacity=".65"/>\n`;
  }
  return `  <path d="M0 11Q42 4 84 21Q118 32 154 7" fill="none" stroke="${color}" stroke-width="1.1" opacity=".65"/>
${leaves}`;
}

function bamboo(color) {
  return `  <path d="M184 6q-9 34-3 78M194 9q-13 40-8 72" fill="none" stroke="${color}" stroke-width="1.2" opacity=".55"/>
  <g fill="${color}" opacity=".58">
    <ellipse cx="177" cy="24" rx="8" ry="2" transform="rotate(-34 177 24)"/><ellipse cx="189" cy="32" rx="8" ry="2" transform="rotate(31 189 32)"/>
    <ellipse cx="175" cy="46" rx="7" ry="1.8" transform="rotate(-30 175 46)"/><ellipse cx="192" cy="55" rx="8" ry="2" transform="rotate(35 192 55)"/>
    <ellipse cx="178" cy="68" rx="7" ry="1.8" transform="rotate(-28 178 68)"/>
  </g>`;
}

function plum(color, branch = "#695348") {
  const flowers = [[159, 25], [171, 37], [182, 22], [191, 48], [176, 59]]
    .map(([x, y]) => `<circle cx="${x}" cy="${y}" r="3" fill="${color}" opacity=".75"/><circle cx="${x}" cy="${y}" r="1" fill="#FFF4EA"/>`)
    .join("");
  return `  <path d="M210 9Q183 22 176 45Q171 59 154 73M192 19q-10 12-23 17M183 41q-12 7-19 20" fill="none" stroke="${branch}" stroke-width="1.4" stroke-linecap="round" opacity=".8"/>
  ${flowers}`;
}

function lotus(color) {
  return `  <g fill="none" stroke="${color}" opacity=".55">
    <path d="M13 264q16-26 32 0q-16 10-32 0zM38 278q18-25 36 0q-18 10-36 0z" fill="${color}" fill-opacity=".16"/>
    <path d="M28 263q0-22 4-42M56 277q-2-19 5-35" stroke-width="0.8"/>
    <path d="M24 238q7-11 14 0q-7 9-14 0z" fill="${color}" fill-opacity=".3"/>
  </g>`;
}

function sceneArt(scene, palette) {
  const [primary, secondary, pale, ink, accent] = palette;
  switch (scene) {
    case "willow":
      return `${willow(primary)}\n${birds(18, 49, ink)}\n${mountains([pale, secondary, primary, accent])}`;
    case "blue-mountain":
      return `${bamboo(primary)}\n${mountains([pale, secondary, primary, accent], true)}\n  <path d="M78 280h28l-5 4H83z" fill="${ink}" opacity=".55"/>`;
    case "spring-field":
      return `${birds(24, 25, ink)}\n  <path d="M0 236Q45 222 90 239T180 236T240 245V297H0Z" fill="${pale}" opacity=".45"/>\n  <path d="M0 266Q45 242 97 273Q148 247 210 268V297H0Z" fill="${primary}" opacity=".24"/>\n${plum(accent)}`;
    case "ink-village":
      return `${mountains([pale, secondary, primary, accent], true)}\n  <g fill="${ink}" opacity=".48"><path d="M8 278h12v12H8zM27 271h18v19H27zM52 280h13v10H52z"/><path d="M5 278l9-7 9 7M24 271l12-9 12 9M49 280l9-7 10 7" fill="none" stroke="${ink}" stroke-width="1.5"/></g>`;
    case "cloud-crane":
      return `${mountains([pale, secondary, primary, accent], true)}\n  <path d="M30 36q12-11 23 0q-10-4-14 5q-1-8-9-5M151 22q12-10 23 0q-10-3-14 6q-1-9-9-6" fill="none" stroke="${ink}" stroke-width="1.4" opacity=".65"/>`;
    case "river-boat":
      return `${mountains([pale, secondary, primary, accent])}\n  <path d="M0 273q30-6 60 0t60 0t60 0t60 0M0 282q28-5 56 0t56 0t56 0t56 0" fill="none" stroke="${secondary}" stroke-width="0.7" opacity=".6"/>\n  <path d="M106 269h24l-5 4h-14zM118 269v-11l9 9h-9" fill="${ink}" opacity=".6"/>`;
    case "moon-pine":
      return `  <circle cx="34" cy="40" r="15" fill="${accent}" opacity=".28"/>\n  <path d="M184 8q-10 25-6 61M177 24l-26 10 25 3-32 14 34 1-27 17" fill="none" stroke="${ink}" stroke-width="1.8" opacity=".62"/>\n${mountains([pale, secondary, primary, accent])}`;
    case "autumn-hill":
      return `${mountains([pale, secondary, primary, accent], true)}\n  <g fill="${accent}" opacity=".55"><circle cx="26" cy="32" r="2"/><circle cx="41" cy="22" r="2.5"/><circle cx="58" cy="35" r="1.7"/><circle cx="72" cy="19" r="2"/></g>`;
    case "lotus-pond":
      return `${lotus(primary)}\n  <path d="M0 281q35-8 70 0t70 0t70 0" fill="none" stroke="${secondary}" stroke-width="0.8" opacity=".5"/>\n${birds(160, 38, ink)}`;
    case "plum-snow":
      return `${plum(accent, ink)}\n${mountains([pale, secondary, primary, accent])}\n  <g fill="${secondary}" opacity=".45"><circle cx="28" cy="32" r="1"/><circle cx="53" cy="20" r="1.2"/><circle cx="91" cy="38" r="1"/><circle cx="125" cy="22" r="1.2"/></g>`;
    case "bamboo-shadow":
      return `${bamboo(primary)}\n  <path d="M0 270Q48 250 91 276Q138 248 210 274V297H0Z" fill="${pale}" opacity=".5"/>`;
    default:
      return `  <path d="M10 280Q26 244 38 280M20 266q12-8 23 0M27 251q11-8 20 1" fill="none" stroke="${primary}" stroke-width="1" opacity=".55"/>\n${mountains([pale, secondary, primary, accent])}`;
  }
}

function yamlScalar(value) {
  if (typeof value === "string") return JSON.stringify(value);
  if (typeof value === "boolean") return value ? "true" : "false";
  return String(value);
}

function moduleYaml(template) {
  const rows = [
    `id: ${yamlScalar(template.id)}`,
    `name: ${yamlScalar(template.name)}`,
    `description: ${yamlScalar(template.description)}`,
    `category: ${yamlScalar(template.category)}`,
    `kind: "customText"`,
    `enabled: true`,
    "defaults:",
  ];
  for (const [key, value] of Object.entries(template.defaults)) {
    rows.push(`  ${key}: ${yamlScalar(value)}`);
  }
  return `${rows.join("\n")}\n`;
}

const templates = [];
const add = (template) => templates.push(template);

const poems = [
  ["willow", "柳亭春晓", "春晓", "孟浩然", "春眠不觉晓，处处闻啼鸟。夜来风雨声，花落知多少。"],
  ["blue-mountain", "青山行舟", "山行", "杜牧", "远上寒山石径斜，白云生处有人家。停车坐爱枫林晚，霜叶红于二月花。"],
  ["spring-field", "春野新绿", "悯农", "李绅", "锄禾日当午，汗滴禾下土。谁知盘中餐，粒粒皆辛苦。"],
  ["ink-village", "水墨江村", "江雪", "柳宗元", "千山鸟飞绝，万径人踪灭。孤舟蓑笠翁，独钓寒江雪。"],
  ["cloud-crane", "云鹤远山", "登鹳雀楼", "王之涣", "白日依山尽，黄河入海流。欲穷千里目，更上一层楼。"],
  ["river-boat", "烟波孤舟", "早发白帝城", "李白", "朝辞白帝彩云间，千里江陵一日还。两岸猿声啼不住，轻舟已过万重山。"],
  ["moon-pine", "松月清辉", "静夜思", "李白", "床前明月光，疑是地上霜。举头望明月，低头思故乡。"],
  ["autumn-hill", "秋山晚照", "枫桥夜泊", "张继", "月落乌啼霜满天，江枫渔火对愁眠。姑苏城外寒山寺，夜半钟声到客船。"],
  ["lotus-pond", "荷塘清韵", "小池", "杨万里", "泉眼无声惜细流，树阴照水爱晴柔。小荷才露尖尖角，早有蜻蜓立上头。"],
  ["plum-snow", "梅雪暖笺", "梅花", "王安石", "墙角数枝梅，凌寒独自开。遥知不是雪，为有暗香来。"],
  ["bamboo-shadow", "竹影疏风", "竹石", "郑燮", "咬定青山不放松，立根原在破岩中。千磨万击还坚劲，任尔东西南北风。"],
  ["orchid-stone", "兰石幽芳", "鹿柴", "王维", "空山不见人，但闻人语响。返景入深林，复照青苔上。"],
];

const scenePalettes = [
  ["#6FAF8D", "#9ACDB8", "#DCEEE7", "#40534A", "#E5A077"],
  ["#6E9FC5", "#9BC4DD", "#DDECF4", "#3D5061", "#E4A36F"],
  ["#7FAE68", "#B5CFA4", "#E5EFDC", "#4E5A45", "#D98C72"],
  ["#7D8580", "#AEB5AF", "#E6E8E5", "#3F4642", "#D3A46F"],
];

poems.forEach(([scene, label, title, author, text], index) => {
  const palette = scenePalettes[index % scenePalettes.length];
  const seven = text.replace(/[，。！？]/g, "").length >= 28;
  for (const vertical of [false, true]) {
    const suffix = vertical ? "vertical" : "horizontal";
    const shortText = text.replace(/[，。！？]/g, "");
    add({
      id: `ref-poem-${scene}-${suffix}`,
      name: `${label} · ${vertical ? "古诗竖排" : seven ? "七言横排" : "五言横排"}`,
      description: `从参考图提炼的${label}矢量主题，${vertical ? "右起竖排并保留山水留白" : "中央版心配合古诗整行练习"}。`,
      category: "诗词",
      defaults: vertical ? {
        grid: index % 3 === 0 ? "plain" : "tian", mode: index % 4 === 0 ? "trace" : "copy", repeats: 1,
        traceCount: 1, traceIntensity: "verylight", vertical: true, gridSize: seven ? 13 : 15,
        gridGap: 0.8, groupGap: 3.2, headerPreset: "poem", showPoemHeader: true,
        background: "plain", backgroundColor: "#FAFCF9", backgroundArtwork: "assets/background.svg",
        gridColor: palette[0], textColor: palette[3], pageMarginTop: 49, pageMarginBottom: 104,
        pageMarginLeft: 55, pageMarginRight: 55, title, author, dynasty: "唐", text,
      } : {
        grid: index % 4 === 1 ? "mi" : "tian", mode: index % 5 === 0 ? "trace" : "copy", repeats: 1,
        traceCount: 1, traceIntensity: "verylight", cellsPerLine: seven ? 7 : 5, gridGap: 0.8,
        groupGap: 2.6, headerPreset: "poem", showPoemHeader: true, background: "plain",
        backgroundColor: "#FAFCF9", backgroundArtwork: "assets/background.svg", gridColor: palette[0],
        textColor: palette[3], pageMarginTop: 50, pageMarginBottom: 104,
        pageMarginLeft: seven ? 32 : 48, pageMarginRight: seven ? 32 : 48,
        title, author, dynasty: "唐", text: shortText,
      },
      svg: xml(`${outerFrame(palette[1], index)}\n${panel(palette[0], vertical, index)}\n${titleOrnament(palette[0])}\n${sceneArt(scene, palette)}`, "#FAFCF9"),
    });
  }
});

const practicePalettes = [
  ["cinnabar", "朱砂", "#B9574F", "#FFF9F6", "#663C37"],
  ["jade", "青玉", "#4F9A79", "#F7FCF8", "#345247"],
  ["indigo", "靛青", "#557FA5", "#F7FAFD", "#354B60"],
  ["amber", "棕金", "#A17B36", "#FCFAF3", "#594B32"],
  ["coral", "珊瑚", "#D17C72", "#FFF8F7", "#67423E"],
  ["ink", "墨灰", "#6B716E", "#FAFAF8", "#3E4240"],
];

const practiceKinds = [
  ["mi", "米字格描红", "trace", 5, 10, 2],
  ["tian", "田字格临摹", "copy", 4, 12, 0],
  ["huigong", "回宫格结构", "trace", 3, 12, 1],
];

practicePalettes.forEach(([slug, label, color, background, ink], paletteIndex) => {
  practiceKinds.forEach(([grid, title, mode, repeats, columns, traceCount], kindIndex) => {
    const id = `ref-grid-${slug}-${grid}`;
    const decoration = `${outerFrame(color, paletteIndex + kindIndex)}\n${titleOrnament(color, 30)}
  <g fill="${color}" opacity=".15"><circle cx="22" cy="31" r="4"/><circle cx="188" cy="31" r="4"/><circle cx="22" cy="269" r="3"/><circle cx="188" cy="269" r="3"/></g>`;
    add({
      id,
      name: `${label} · ${title}`,
      description: `参考缩略图中的${title}版式，使用${label}配色、分组间距和页边装饰。`,
      category: "基础",
      defaults: {
        grid, mode, repeats, traceCount, traceIntensity: "verylight", cellsPerLine: columns,
        gridGap: 0.8, groupGap: 3.5, headerPreset: "titleAndFields", background: "plain",
        backgroundColor: background, backgroundArtwork: "assets/background.svg", gridColor: color,
        textColor: ink, pageMarginTop: 35, pageMarginBottom: 25, pageMarginLeft: 20,
        pageMarginRight: 20, title: `${label}${title}`, text: "春风化雨山清水秀勤学善思宁静致远",
      },
      svg: xml(decoration, background),
    });
  });
});

const learningPalettes = [
  ["mint", "薄荷绿", "#55B894", "#F7FFFB", "#315D50"],
  ["sky", "晴空蓝", "#5DA3D6", "#F6FBFF", "#31536B"],
  ["peach", "桃花粉", "#D98291", "#FFF8FA", "#68404A"],
  ["sun", "暖阳橙", "#D8904C", "#FFFBF5", "#6A4B31"],
];

learningPalettes.forEach(([slug, label, color, background, ink], index) => {
  const art = `${outerFrame(color, index)}
  <path d="M18 30q20-9 40 0t40 0t40 0t40 0" fill="none" stroke="${color}" stroke-width="0.7" opacity=".55"/>
  <path d="M18 273q20-8 40 0t40 0t40 0t40 0" fill="none" stroke="${color}" stroke-width="0.7" opacity=".4"/>
  <g fill="${color}" opacity=".22"><circle cx="24" cy="44" r="3"/><circle cx="186" cy="44" r="2"/><circle cx="29" cy="258" r="2"/><circle cx="181" cy="258" r="3"/></g>`;
  add({
    id: `ref-pinyin-${slug}-syllable`, name: `${label} · 拼音四线格`,
    description: `清爽${label}四线格，适合声母、韵母和带调音节连续书写。`, category: "拼音",
    defaults: { grid: "pinyin", mode: "copy", repeats: 1, cellsPerLine: 8, gridGap: 2.2,
      groupGap: 3, headerPreset: "titleAndFields", background: "plain", backgroundColor: background,
      backgroundArtwork: "assets/background.svg", gridColor: color, textColor: ink, pageMarginTop: 38,
      pageMarginBottom: 28, pageMarginLeft: 22, pageMarginRight: 22, title: "拼音书写练习",
      text: "a o e i u ü ai ei ui ao ou iu ie üe er an en in un ün ang eng ing ong" },
    svg: xml(art, background),
  });
  add({
    id: `ref-pinyin-${slug}-character`, name: `${label} · 生字带拼音`,
    description: `${label}拼音生字帖骨架，范字、描红和空格按组排列。`, category: "拼音",
    defaults: { grid: "tian", mode: "trace", repeats: 4, traceCount: 2, traceIntensity: "verylight",
      cellsPerLine: 12, gridGap: 0.7, groupGap: 3.2, headerPreset: "titleAndFields", background: "plain",
      backgroundColor: background, backgroundArtwork: "assets/background.svg", gridColor: color, textColor: ink,
      pageMarginTop: 38, pageMarginBottom: 30, pageMarginLeft: 20, pageMarginRight: 20,
      title: "生字拼音练习", text: "春冬风雪花飞入姓什么国王青草清水" },
    svg: xml(art, background),
  });
  add({
    id: `ref-pinyin-${slug}-words`, name: `${label} · 词语描红`,
    description: `${label}词语专项，双格一组并加大组间留白，适合二字词语。`, category: "拼音",
    defaults: { grid: "mi", mode: "trace", repeats: 2, traceCount: 1, traceIntensity: "verylight",
      cellsPerLine: 10, gridGap: 0.8, groupGap: 4.5, headerPreset: "titleAndFields", background: "plain",
      backgroundColor: background, backgroundArtwork: "assets/background.svg", gridColor: color, textColor: ink,
      pageMarginTop: 38, pageMarginBottom: 30, pageMarginLeft: 22, pageMarginRight: 22,
      title: "词语描红练习", text: "春天冬雪大风飞鸟花草山水田园" },
    svg: xml(art, background),
  });
});

learningPalettes.forEach(([slug, label, color, background, ink], index) => {
  const art = `${outerFrame(color, index + 1)}
  <path d="M20 31h62M128 31h62M20 267h62M128 267h62" stroke="${color}" stroke-width="0.7" opacity=".55"/>
  <g fill="${color}" opacity=".22"><path d="M101 26l4-6 4 6-4 6z"/><path d="M101 272l4-6 4 6-4 6z"/></g>`;
  for (const sentence of [false, true]) {
    add({
      id: `ref-english-${slug}-${sentence ? "sentence" : "words"}`,
      name: `${label} · ${sentence ? "英文句子抄写" : "英文单词书写"}`,
      description: `${label}四线三格英文模板，${sentence ? "适合短句与段落" : "适合小学单词分组"}。`,
      category: "英文",
      defaults: { grid: "english", mode: "trace", repeats: 1, groupByWord: true,
        cellsPerLine: sentence ? 20 : 16, gridGap: 1, groupGap: 3, headerPreset: "titleAndFields",
        background: "plain", backgroundColor: background, backgroundArtwork: "assets/background.svg",
        gridColor: color, textColor: ink, pageMarginTop: 38, pageMarginBottom: 28,
        pageMarginLeft: 20, pageMarginRight: 20, title: sentence ? "English Sentences" : "English Words",
        text: sentence ? "Good morning I like reading books We learn and grow every day"
          : "name class date book ruler pencil school teacher friend family" },
      svg: xml(art, background),
    });
  }
});

const specials = [
  {
    id: "ref-special-couplet", name: "朱砂春联 · 对联描红", category: "主题",
    description: "参考春联截图提炼的横批、左右联和中心福字构图。",
    defaults: { grid: "tian", mode: "trace", repeats: 1, traceCount: 1, traceIntensity: "verylight",
      vertical: true, gridSize: 17, gridGap: 1.2, groupGap: 8, headerPreset: "none", background: "plain",
      backgroundColor: "#FFF9F6", backgroundArtwork: "assets/background.svg", gridColor: "#E77E68",
      textColor: "#8B3D32", pageMarginTop: 53, pageMarginBottom: 70, pageMarginLeft: 63,
      pageMarginRight: 63, title: "春联", text: "一帆风顺年年好，万事如意步步高。" },
    svg: xml(`${outerFrame("#E77E68", 2)}
  <rect x="73" y="20" width="64" height="18" fill="none" stroke="#E77E68" stroke-width="0.9"/>
  <rect x="28" y="55" width="22" height="174" fill="#FFFDFB" stroke="#E77E68" stroke-width="0.9"/>
  <rect x="160" y="55" width="22" height="174" fill="#FFFDFB" stroke="#E77E68" stroke-width="0.9"/>
  <rect x="89" y="116" width="32" height="32" transform="rotate(45 105 132)" fill="none" stroke="#E77E68" stroke-width="1"/>`, "#FFF9F6"),
  },
  {
    id: "ref-special-single-page", name: "草木标本 · 每字一页", category: "基础",
    description: "单个大字居中，外围保留读音、结构和多次临写区域。",
    defaults: { grid: "tian", mode: "copy", repeats: 1, hollowGlyph: false, cellsPerLine: 1,
      gridSize: 46, gridGap: 0, groupGap: 3, headerPreset: "titleAndFields", background: "plain",
      backgroundColor: "#FFFCF8", backgroundArtwork: "assets/background.svg", gridColor: "#D8A77D",
      textColor: "#65432F", pageMarginTop: 70, pageMarginBottom: 118, pageMarginLeft: 78,
      pageMarginRight: 78, title: "每字专项", text: "林" },
    svg: xml(`${outerFrame("#D8A77D", 2)}\n${panel("#D8A77D", false, 0)}
  <g fill="#8D684D" opacity=".55"><ellipse cx="32" cy="36" rx="12" ry="3" transform="rotate(-22 32 36)"/><ellipse cx="178" cy="36" rx="12" ry="3" transform="rotate(22 178 36)"/></g>`, "#FFFCF8"),
  },
  {
    id: "ref-special-name", name: "姓名专项 · 三字练习", category: "基础",
    description: "参考姓名字帖，先分字练习，再进行完整姓名连写。",
    defaults: { grid: "tian", mode: "trace", repeats: 6, traceCount: 4, traceIntensity: "verylight",
      cellsPerLine: 12, blankCellLineCount: 1, gridGap: 0.7, groupGap: 4, headerPreset: "titleAndFields",
      background: "plain", backgroundColor: "#FFF8FA", backgroundArtwork: "assets/background.svg",
      gridColor: "#D98CA0", textColor: "#6A414D", pageMarginTop: 38, pageMarginBottom: 32,
      pageMarginLeft: 24, pageMarginRight: 24, title: "姓名书写练习", text: "林欣彤" },
    svg: xml(`${outerFrame("#D98CA0", 1)}\n${titleOrnament("#D98CA0")}`, "#FFF8FA"),
  },
  {
    id: "ref-special-hollow", name: "宣纸双钩 · 大字填墨", category: "基础",
    description: "大格空心双钩字，暖色宣纸和棕金双框适合毛笔填墨。",
    defaults: { grid: "mi", mode: "copy", repeats: 1, hollowGlyph: true, cellsPerLine: 4,
      gridSize: 38, gridGap: 3, groupGap: 4, headerPreset: "titleAndFields", background: "ricepaper",
      backgroundColor: "#FBF6EA", backgroundArtwork: "assets/background.svg", gridColor: "#A9854F",
      textColor: "#4F4030", pageMarginTop: 48, pageMarginBottom: 54, pageMarginLeft: 34,
      pageMarginRight: 34, title: "双钩填墨", text: "永和九年" },
    svg: xml(`${outerFrame("#A9854F", 2)}\n${titleOrnament("#A9854F", 38)}`, "#FBF6EA"),
  },
  {
    id: "ref-special-composition", name: "橙线作文纸 · 400 格", category: "基础",
    description: "参考作文纸截图的橙色边框与密集方格，适合打印后书写。",
    defaults: { grid: "plain", mode: "copy", repeats: 1, groupByWord: true, pinyinOnly: true,
      cellsPerLine: 20, gridGap: 0, groupGap: 0, headerPreset: "titleAndFields", background: "plain",
      backgroundColor: "#FFFCF8", backgroundArtwork: "assets/background.svg", gridColor: "#DF8A52",
      textColor: "#7A4D31", pageMarginTop: 36, pageMarginBottom: 24, pageMarginLeft: 18,
      pageMarginRight: 18, title: "作文纸", text: "格".repeat(400) },
    svg: xml(`${outerFrame("#DF8A52", 0)}\n${titleOrnament("#DF8A52", 30)}`, "#FFFCF8"),
  },
  {
    id: "ref-special-ruled", name: "青绿横线 · 汉字抄写", category: "基础",
    description: "宽松横线与浅方格组合，适合句子、段落和课文抄写。",
    defaults: { grid: "plain", mode: "copy", repeats: 1, groupByWord: true, pinyinOnly: true,
      cellsPerLine: 14, gridGap: 0.8, groupGap: 1.5, headerPreset: "titleAndFields", background: "letter",
      backgroundColor: "#FAFEFC", backgroundLineColor: "#A7D8C5", backgroundLineSpacing: 11,
      backgroundArtwork: "assets/background.svg", gridColor: "#75B99E", textColor: "#355A4C",
      pageMarginTop: 38, pageMarginBottom: 28, pageMarginLeft: 22, pageMarginRight: 22,
      title: "课文抄写", text: "格".repeat(196) },
    svg: xml(`${outerFrame("#75B99E", 1)}\n${titleOrnament("#75B99E", 30)}`, "#FAFEFC"),
  },
  {
    id: "ref-special-mengxue", name: "蒙学卷轴 · 千字文", category: "蒙学",
    description: "宣纸卷轴式版心，适合三字经、千字文和弟子规描红。",
    defaults: { grid: "tian", mode: "trace", repeats: 3, traceCount: 1, traceIntensity: "verylight",
      cellsPerLine: 12, gridGap: 0.8, groupGap: 4, headerPreset: "titleAndFields", background: "ricepaper",
      backgroundColor: "#FBF6EA", backgroundArtwork: "assets/background.svg", gridColor: "#B48A56",
      textColor: "#554332", pageMarginTop: 44, pageMarginBottom: 40, pageMarginLeft: 28,
      pageMarginRight: 28, title: "蒙学描红", text: "天地玄黄宇宙洪荒日月盈昃辰宿列张寒来暑往秋收冬藏" },
    svg: xml(`${outerFrame("#B48A56", 3)}
  <path d="M22 20h166l-7 8H29zM22 277h166l-7-8H29z" fill="#B48A56" opacity=".16"/>`, "#FBF6EA"),
  },
  {
    id: "ref-special-scroll-vertical", name: "江南题签 · 古诗竖排", category: "诗词",
    description: "中央题签与江南远景组合，保留传统右起竖排留白。",
    defaults: { grid: "plain", mode: "copy", repeats: 1, vertical: true, gridSize: 14, gridGap: 0.8,
      groupGap: 3.2, headerPreset: "poem", showPoemHeader: true, background: "ricepaper",
      backgroundArtwork: "assets/background.svg", gridColor: "#C6AA7A", textColor: "#4A4035",
      pageMarginTop: 53, pageMarginBottom: 104, pageMarginLeft: 55, pageMarginRight: 55,
      title: "山行", author: "杜牧", dynasty: "唐",
      text: "远上寒山石径斜，白云生处有人家。停车坐爱枫林晚，霜叶红于二月花。" },
    svg: xml(`${panel("#C6AA7A", true, 3)}\n${outerFrame("#C6AA7A", 2)}\n${sceneArt("ink-village", scenePalettes[3])}`, "#FBF7EC"),
  },
  {
    id: "ref-special-two-column", name: "左右对照 · 两列生字", category: "基础",
    description: "参考两列偏旁、生字练习版式，强化左右对照和分组书写。",
    defaults: { grid: "tian", mode: "trace", repeats: 4, traceCount: 2, traceIntensity: "verylight",
      cellsPerLine: 8, gridGap: 0.8, groupGap: 8, headerPreset: "titleAndFields", background: "plain",
      backgroundColor: "#F8FCFF", backgroundArtwork: "assets/background.svg", gridColor: "#6FA4C7",
      textColor: "#344E60", pageMarginTop: 40, pageMarginBottom: 30, pageMarginLeft: 24,
      pageMarginRight: 24, title: "两列生字练习", text: "春冬风雪花飞入姓什国王清水" },
    svg: xml(`${outerFrame("#6FA4C7", 1)}
  <rect x="18" y="55" width="80" height="204" rx="3" fill="none" stroke="#6FA4C7" stroke-width="0.4" opacity=".45"/>
  <rect x="112" y="55" width="80" height="204" rx="3" fill="none" stroke="#6FA4C7" stroke-width="0.4" opacity=".45"/>`, "#F8FCFF"),
  },
  {
    id: "ref-special-practice-card", name: "每日打卡 · 分区练习", category: "基础",
    description: "参考每日练习卡，将范字、描红、临写按行分区，便于重复打卡。",
    defaults: { grid: "nine", mode: "trace", repeats: 4, traceCount: 2, traceIntensity: "verylight",
      cellsPerLine: 12, blankCellLineCount: 1, gridGap: 0.8, groupGap: 4, headerPreset: "titleAndFields",
      background: "plain", backgroundColor: "#FAFCF7", backgroundArtwork: "assets/background.svg",
      gridColor: "#89AF6C", textColor: "#415636", pageMarginTop: 38, pageMarginBottom: 28,
      pageMarginLeft: 22, pageMarginRight: 22, title: "每日练字打卡", text: "天地玄黄日月山川春夏秋冬" },
    svg: xml(`${outerFrame("#89AF6C", 0)}
  <path d="M20 49h170M20 114h170M20 179h170M20 244h170" stroke="#89AF6C" stroke-width="0.45" opacity=".35"/>`, "#FAFCF7"),
  },
];

specials.forEach(add);

for (const template of templates) {
  const directory = join(moduleRoot, template.id);
  const assets = join(directory, "assets");
  mkdirSync(assets, { recursive: true });
  writeFileSync(join(directory, "module.yml"), moduleYaml(template), "utf8");
  writeFileSync(join(assets, "background.svg"), template.svg, "utf8");
}

if (templates.length !== 72) {
  throw new Error(`Expected 72 generated templates, got ${templates.length}.`);
}

console.log(`Generated ${templates.length} editable template directories in ${moduleRoot}`);
