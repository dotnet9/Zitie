/* 原型只维护演示状态；系统目录、更新与安装均使用受控交互。 */
(() => {
  const $ = id => document.getElementById(id);
  const escape = value => String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character]);
  const catalog = window.ZITIE_CATALOG, resources = window.ZITIE_RESOURCES;
  const paletteNames = { DotNet: 'DotNetPalette', Aurora: 'AuroraPalette', Ember: 'EmberPalette', Default: 'DefaultPalette', Aquatic: 'AquaticPalette', Desert: 'DesertPalette', Dusk: 'DuskPalette', NightSky: 'NightSkyPalette' };
  const categoryKeys = { 汉字: 'CategoryChinese', 拼音: 'CategoryPinyin', 数字: 'CategoryNumbers', 英文: 'CategoryEnglish', 有笔顺: 'CategoryStrokes', 组词: 'CategoryWords', 书法: 'CategoryCalligraphy', 生字: 'CategoryNewWords', 脱格: 'CategoryBorderless', 文章: 'CategoryArticles', 试卷: 'CategoryExams', 测试卡: 'CategoryTestCards', 名字: 'CategoryNames', 封面: 'CategoryCovers', 基础: 'CategoryBasics', 蒙学: 'CategoryClassics', 诗词: 'CategoryPoetry', 主题: 'CategoryThemes' };
  const paths = ['M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z', 'M12 5v14 M5 12h14', 'M12 3a9 9 0 1 0 0 18 9 9 0 1 0 0-18 M12 11v6 M12 7v1', 'M6 3h9l4 4v14H6z M9 11h7 M9 15h7 M9 7h3'];
  document.querySelectorAll('.rail button').forEach((button, index) => button.innerHTML = `<svg viewBox="0 0 24 24"><path d="${paths[index]}"/></svg>`);
  const state = { route: 'gallery', mode: 'System', palette: 'DotNet', language: 'zh-CN', category: '', tab: 0, template: null, text: '床前明月光，疑是地上霜。举头望明月，低头思故乡。', title: '', dynasty: '唐', author: '李白', header: 2, paper: 'a4', margin: 15, blankLines: 0, practiceMode: 'trace', intensity: 'Moderate', spacing: 2, texture: 'plain', showAuthor: false, grid: 'rice', columns: 0, cellSize: 14, repeats: 5, opacity: 40, gridColor: '#b04a3f', textColor: '#242424', pinyin: false, pinyinOnly: false, hollow: false, vertical: false, update: 'idle', progress: 0, drawer: null, selectedContent: null, selectedTemplate: null };
  let updateTimer, filtered = [], unknownKeys = new Set();
  const t = (key, ...arguments_) => {
    let value = resources[state.language].Zitie.Text[key];
    if (value === undefined) { unknownKeys.add(key); return key; }
    return value.replace(/\{(\d+)(?::[^}]*)?\}/g, (_, index) => arguments_[Number(index)] ?? '');
  };
  const categoryName = value => categoryKeys[value] ? t(categoryKeys[value]) : value;
  const options = (keys, current) => keys.map(key => `<option value="${escape(key)}"${key === current ? ' selected' : ''}>${escape(t(key))}</option>`).join('');
  const label = key => `<label>${escape(t(key))}</label>`;
  const field = (key, content) => `<div class="field">${label(key)}${content}</div>`;
  const check = (key, name) => `<div class="field"><label class="check"><input type="checkbox" data-state="${name}"${state[name] ? ' checked' : ''}>${escape(t(key))}</label></div>`;
  const input = (name, type = 'text', extra = '') => `<input type="${type}" data-state="${name}" value="${escape(state[name])}" ${extra}>`;
  const select = (name, items) => `<select data-state="${name}">${items.map(([value, key]) => `<option value="${value}"${String(state[name]) === String(value) ? ' selected' : ''}>${escape(t(key))}</option>`).join('')}</select>`;
  function applyAppearance() {
    const dark = state.mode === 'Dark' || state.mode === 'System' && matchMedia('(prefers-color-scheme: dark)').matches;
    const tokens = window.ZITIE_PALETTES[state.palette][dark ? 'dark' : 'light'];
    for (const [name, token] of Object.entries({ primary: 'StudioPrimaryColor', 'primary-hover': 'StudioPrimaryHoverColor', 'primary-pressed': 'StudioPrimaryPressedColor', 'on-primary': 'StudioOnPrimaryColor', background: 'StudioBackgroundColor', surface: 'StudioSurfaceColor', raised: 'StudioRaisedColor', border: 'StudioBorderColor', text: 'StudioTextColor', muted: 'StudioMutedColor', accent: 'StudioAccentColor' })) document.documentElement.style.setProperty('--' + name, tokens[token]);
    document.documentElement.dataset.mode = dark ? 'dark' : 'light'; document.documentElement.dataset.palette = state.palette;
    $('palette').innerHTML = Object.keys(paletteNames).map(palette => `<button class="palette-dot" role="option" data-palette="${palette}" aria-selected="${state.palette===palette}" title="${escape(t(paletteNames[palette]))}" aria-label="${escape(t(paletteNames[palette]))}"><i style="background:${window.ZITIE_PALETTES[palette].swatch}"></i></button>`).join('');
    $('mode').dataset.mode=state.mode; $('mode').textContent=state.mode==='Light'?'☀':state.mode==='Dark'?'☾':'◐'; $('mode').title=t(state.mode+'Mode'); $('mode').setAttribute('aria-label',$('mode').title);
    $('language').title=t('LanguageName'); $('language').setAttribute('aria-label',$('language').title);
    document.documentElement.lang = state.language;
  }
  function localize() {
    for (const button of document.querySelectorAll('[data-window],[data-about]')) {
      const action = button.dataset.window || button.dataset.about;
      const labels = state.language === 'zh-CN' ? { minimize: '最小化', maximize: '最大化或还原', close: '关闭' } : { minimize: 'Minimize', maximize: 'Maximize or restore', close: 'Close' };
      button.title = labels[action]; button.setAttribute('aria-label', button.title);
    }
    document.querySelectorAll('[data-text]').forEach(node => node.textContent = t(node.dataset.text));
    document.querySelectorAll('[data-tip]').forEach(node => { node.title = t(node.dataset.tip); node.setAttribute('aria-label', node.title); });
    document.querySelectorAll('[data-placeholder]').forEach(node => node.placeholder = t(node.dataset.placeholder));
    const icons = {
      'open-document': 'M3 8a2 2 0 0 1 2-2h4l2.2 2.5H19a2 2 0 0 1 2 2V17a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z',
      'save-document': 'M5 3h11l3 3v13a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M8 3v5h7 M8 13h8v5H8z',
      'save-as': 'M5 3h11l3 3v13a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z M14 17l4-4 1.5 1.5L15.5 18.5 13 19z',
      'save-template': 'M6 3h12v18l-6-4.2L6 21z', 'batch-png': 'M8 3h8v8H8z M3 13h8v8H3z M13 13h8v8h-8z',
      'export-png': 'M3 5h18v14H3z M10.1 10a1.6 1.6 0 1 1-3.2 0 1.6 1.6 0 0 1 3.2 0z M21 16l-5-5-7 7',
      'export-pdf': 'M12 3v11 M7.5 10.5L12 15l4.5-4.5 M4 17.5V19a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-1.5'
    };
    for (const [id, path] of Object.entries(icons)) { const button = $(id); button.classList.add('command-icon'); button.innerHTML = `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="${path}"/></svg><span>${escape(button.textContent)}</span>`; }
    renderGallery(); renderEditor(); renderUpdate(); if (state.drawer) renderDrawer(true);
  }
  function renderGallery() {
    $('categories').innerHTML = Object.keys(categoryKeys).filter(category => catalog.templates.some(item => item.categories.includes(category))).map(category => `<button data-category="${escape(category)}" class="${state.category === category ? 'active' : ''}">${escape(categoryName(category))}</button>`).join('');
    const query = $('gallery-search').value.trim().toLocaleLowerCase();
    const terms = query.split(/[\s\p{P}\p{S}]+/u).filter(Boolean);
    filtered = catalog.templates.filter(item => {
      const searchable = (item.id + ' ' + item.name + ' ' + item.image + (terms.length > 1 ? '' : ' ' + item.description + ' ' + item.categories.join(' '))).toLocaleLowerCase();
      return (!state.category || item.categories.includes(state.category)) && (!query || searchable.includes(query) || terms.every(term => searchable.includes(term)));
    });
    $('gallery-count').textContent = t('TemplateCount', filtered.length);
    $('catalog-count').textContent = t('TemplateCount', catalog.templates.length) + ' · ' + Object.keys(categoryKeys).map(category => { const count = catalog.templates.filter(item => item.categories.includes(category)).length; return count ? categoryName(category) + ' ' + count : ''; }).filter(Boolean).join(' · ');
    $('cards').innerHTML = filtered.map(item => {
      const image = window.ZITIE_IMAGES[item.image || item.id];
      const preview = image ? `<img src="${image}" alt="" loading="lazy">` : `<div class="fallback-paper"><h5>${escape(item.name)}</h5><div class="fallback-grid">${[...(item.text || '字帖练习天地玄黄宇宙洪荒').repeat(5)].slice(0, 54).map(character => `<span>${escape(character)}</span>`).join('')}</div></div>`;
      const description = item.kind === 'styleTemplate' ? t('StyleTemplateDescription', item.categories.map(categoryName).join(' / ')) : item.description;
      return `<button class="card" data-template="${escape(item.id)}"${item.enabled ? '' : ' disabled'}><div class="thumb">${preview}</div><span class="tag">${escape(categoryName(item.categories[0] || ''))}</span><strong>${escape(item.name)}</strong><div class="description">${escape(description)}</div><span class="card-status">${escape(item.enabled ? t('SampleTemplate', t(item.grid === 'none' ? 'NoGrid' : 'RiceGrid')) : t('ComingSoon'))}</span></button>`;
    }).join('');
    if (!filtered.length) $('cards').innerHTML = `<p>${escape(t('NoTemplatesFound'))}</p>`;
  }
  function renderEditor() {
    $('current-template').textContent = state.template?.name || t('NoTemplateSelected');
    const tabs = ['Content', 'Layout', 'Practice', 'Writing', 'Pinyin'], symbols = ['M4 7V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v2 M12 4v16 M9 20h6', 'M3 3h18v18H3z M3 9h18 M9 3v18', 'M12 2v3 M12 19v3 M2 12h3 M19 12h3 M5 5l2 2 M17 17l2 2 M19 5l-2 2 M7 17l-2 2 M12 8a4 4 0 1 0 0 8 4 4 0 1 0 0-8', 'M12 19l7-7a4.95 4.95 0 0 0-7-7l-7 7v7z M18 13l-7-7', 'M4 12h3l2-6 4 12 2-6h5'];
    $('tabs').innerHTML = tabs.map((key, index) => `<button data-tab="${index}" class="${state.tab === index ? 'active' : ''}"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="${symbols[index]}"/></svg>${escape(t(key))}</button>`).join('');
    let content = '';
    if (state.tab === 0) content = field('PracticeText', `<textarea data-state="text">${escape(state.text)}</textarea><small>${escape(t('PracticeTextHint'))}</small>`) + field('ContentLibrary', `<button data-action="content">${escape(t('OpenContentLibrary'))}</button><small>${escape(t('ContentLibraryHint'))}</small>`) + `<div class="field-row">${field('Title', input('title'))}${field('HeaderStyle', select('header', [[0, 'NoHeader'], [1, 'FillInHeader'], [2, 'TitleAndFillIn'], [3, 'PoemHeader'], [4, 'Custom']]))}</div><div class="field-row">${field('Dynasty', input('dynasty'))}${field('Author', input('author'))}</div>` + check('ShowPoemAuthor', 'showAuthor');
    if (state.tab === 1) content = `<div class="field-row">${field('PaperSize', select('paper', [['a4', 'A4Portrait'], ['a4Landscape', 'A4Landscape'], ['a3', 'A3Portrait'], ['letter', 'LetterPortrait']]))}${field('PageMargin', input('margin', 'number', 'min="0" max="40"'))}</div>` + field('GridType', select('grid', [['none', 'NoGrid'], ['rice', 'RiceGrid'], ['field', 'FieldGrid'], ['palace', 'PalaceGrid'], ['square', 'SquareGrid'], ['english', 'EnglishGrid'], ['nine', 'NineGrid'], ['pinyin', 'PinyinGrid']])) + `<div class="field-row">${field('CellsPerRow', `<select data-state="columns">${[0,5,7,8,10,12,14,16,20,24].map(value => `<option value="${value}"${state.columns === value ? ' selected' : ''}>${value || escape(t('Auto'))}</option>`).join('')}</select>`)}${field('EmptyCellRows', input('blankLines', 'number', 'min="0" max="10"'))}</div>` + field('CellSize', input('cellSize', 'range', 'min="8" max="60"') + `<small>${state.cellSize} mm</small>`) + check('GroupWords', 'groupWords') + check('VerticalLayout', 'vertical');
    if (state.tab === 2) content = field('PracticeMode', select('practiceMode', [['trace', 'TracingMode'], ['copy', 'CopyingMode']])) + `<div class="field-row">${field('CellsPerCharacter', input('repeats', 'number', 'min="1" max="8"'))}${field('TracingOpacity', select('intensity', ['VeryDark','Dark','MediumDark','Moderate','Light','VeryLight','White','Outline'].map(value => [value, value])))}</div>` + field('GroupSpacing', input('spacing', 'range', 'min="1" max="10"') + `<small>${state.spacing} mm</small>`) + check('HollowGlyph', 'hollow');
    if (state.tab === 3) content = `<div class="field-row">${field('GridColor', input('gridColor') + select('gridPreset', [['red','Red'],['black','Black'],['green','Green']]))}${field('TextColor', input('textColor') + select('textPreset', [['red','Red'],['black','Black'],['green','Green']]))}</div>` + field('SheetFont', '<select data-state="font"><option>LXGW WenKai</option><option>KaiTi</option><option>Microsoft YaHei UI</option></select>') + field('PaperTexture', select('texture', [['plain', 'PlainPaper'], ['red', 'RedLinedPaper'], ['blue', 'BlueLinedPaper'], ['rice', 'RicePaper']])) + check('DecorativeFrame', 'frame');
    if (state.tab === 4) content = check('ShowPinyin', 'pinyin') + check('PinyinOnly', 'pinyinOnly');
    $('parameters').innerHTML = content; renderSheet();
  }
  function renderSheet() {
    const sheet = $('sheet'); sheet.style.setProperty('--grid-color', state.gridColor); sheet.style.setProperty('--ink-color', state.textColor); sheet.style.setProperty('--trace-opacity', state.opacity / 100); sheet.style.setProperty('--cell-size', state.cellSize * 3.78 * .8 + 'px');
    sheet.style.fontFamily = state.font || 'WenKai'; sheet.style.border = state.frame ? '3px double ' + state.gridColor : 'none';
    const landscape = state.paper === 'a4Landscape';
    const scale = Number($('sheet-zoom').value) / 100;
    sheet.style.width = (landscape ? 1123 : state.paper === 'a3' ? 1123 : 794) * scale + 'px';
    sheet.style.height = sheet.style.minHeight = (landscape ? 794 : state.paper === 'a3' ? 1587 : 1123) * scale + 'px';
    sheet.style.padding = Math.max(0, Math.min(40, Number(state.margin))) * 3.78 * scale + 'px';
    sheet.style.background = state.texture === 'rice' ? '#fff9e9' : state.texture === 'red' ? 'repeating-linear-gradient(0deg,#fff 0,#fff 22px,#e9c4c4 23px,#fff 24px)' : state.texture === 'blue' ? 'repeating-linear-gradient(0deg,#fff 0,#fff 22px,#b9cfe9 23px,#fff 24px)' : '#fff';
    $('sheet-title').textContent = state.title; $('sheet-author').textContent = state.showAuthor ? state.dynasty + ' · ' + state.author : '';
    $('sheet-title').hidden = Number(state.header) < 2; $('sheet-author').hidden = Number(state.header) !== 3;
    document.querySelector('.sheet-meta').hidden = ![1, 2].includes(Number(state.header));
    const pageWidth = landscape || state.paper === 'a3' ? 297 : state.paper === 'letter' ? 216 : 210;
    const repeats = Math.max(1, Math.min(8, Number(state.repeats)));
    const columns = Math.max(1, Math.min(24, Number(state.columns) || Math.max(repeats, Math.floor((pageWidth - state.margin * 2) / (state.cellSize + 2) / repeats) * repeats)));
    const blankLines = Math.max(0, Math.min(10, Number(state.blankLines))), cells = [];
    const cell = (character, trace = false) => `<span class="gcell ${escape(state.grid)}${trace ? ' trace' : ''}">${state.pinyin && character ? `<small class="pinyin">${escape(catalog.pinyin?.[character] || '')}</small>` : ''}<b class="glyph${state.hollow || state.intensity === 'Outline' ? ' hollow' : ''}">${state.pinyinOnly ? '' : escape(character)}</b></span>`;
    const append = (character, trace) => cells.push(cell(character, trace));
    const finishLine = () => {
      while (cells.length % columns) append('');
    };
    if (state.groupWords || state.vertical) {
      const groups = state.text.trim().split(state.vertical ? /[\s，。！？；、]+/u : /\s+/u).filter(Boolean);
      for (const group of groups) {
        const glyphs = [...group];
        if (cells.length % columns && glyphs.length > columns - cells.length % columns) finishLine();
        for (const character of glyphs) {
          if (cells.length && cells.length % columns === 0) for (let index = 0; index < blankLines * columns; index++) append('');
          append(state.practiceMode === 'copy' ? '' : character, true);
        }
      }
    } else {
      for (const character of [...state.text].filter(character => !/\s/.test(character)).slice(0, 48)) {
        if (cells.length % columns && repeats > columns - cells.length % columns) finishLine();
        if (cells.length && cells.length % columns === 0) for (let index = 0; index < blankLines * columns; index++) append('');
        for (let repeat = 0; repeat < repeats; repeat++) append(repeat < (state.practiceMode === 'copy' ? 1 : 3) ? character : '', repeat > 0);
      }
    }
    const grid = $('sheet-grid'); grid.style.gridTemplateColumns = `repeat(${columns},1fr)`; grid.style.gap = Math.max(1, Number(state.spacing)) * 3.78 * scale + 'px'; grid.style.writingMode = state.vertical ? 'vertical-rl' : 'horizontal-tb'; grid.innerHTML = cells.slice(0, 120).join('');
  }
  const gridKinds = { none: 'None', rice: 'Mi', field: 'Tian', palace: 'HuiGong', square: 'Plain', english: 'English', nine: 'Nine', pinyin: 'Pinyin' };
  const headerKinds = ['None', 'Fields', 'TitleAndFields', 'Poem', 'Custom'];
  const backgrounds = { plain: 'Plain', red: 'RedGrid', blue: 'Letter', rice: 'RicePaper' };
  const intensities = { Moderate: 'Medium', Outline: 'Hollow' };
  function createDocument() {
    const dimensions = { a4: [210, 297], a4Landscape: [297, 210], a3: [297, 420], letter: [216, 279] }[state.paper];
    return { version: 1, moduleId: state.template?.id || null, spec: {
      text: state.text, title: state.title, dynasty: state.dynasty, author: state.author,
      grid: gridKinds[state.grid], mode: state.practiceMode === 'copy' ? 'Copy' : 'Trace',
      headerPreset: headerKinds[state.header], showHeaderFields: [1, 2].includes(Number(state.header)), showPoemHeader: state.showAuthor,
      cellsPerLine: Number(state.columns), blankCellLineCount: Number(state.blankLines), groupByWord: Boolean(state.groupWords),
      repeatsPerChar: Number(state.repeats), traceSlotCount: 2, gridSizeMm: Number(state.cellSize), gridGapMm: 2, groupGapMm: Number(state.spacing),
      traceIntensity: intensities[state.intensity] || state.intensity, gridColor: state.gridColor, textColor: state.textColor,
      orientation: state.vertical ? 'Vertical' : 'Horizontal', background: backgrounds[state.texture], frameBorder: Boolean(state.frame),
      showPinyin: Boolean(state.pinyin), pinyinOnly: Boolean(state.pinyinOnly), hollowGlyph: Boolean(state.hollow), fontFamilyName: state.font || null,
      page: { widthMm: dimensions[0], heightMm: dimensions[1], marginTopMm: Number(state.margin), marginBottomMm: Number(state.margin), marginLeftMm: Number(state.margin), marginRightMm: Number(state.margin) }
    } };
  }
  function loadDocument(document_) {
    if (document_?.version !== 1 || !document_.spec || typeof document_.spec !== 'object') throw new Error('Invalid Zitie document');
    const spec = document_.spec;
    for (const key of ['text', 'title', 'dynasty', 'author', 'gridColor', 'textColor']) if (spec[key] !== undefined) state[key] = spec[key] ?? '';
    const mappings = { columns: 'cellsPerLine', blankLines: 'blankCellLineCount', groupWords: 'groupByWord', repeats: 'repeatsPerChar', cellSize: 'gridSizeMm', spacing: 'groupGapMm', showAuthor: 'showPoemHeader', frame: 'frameBorder', pinyin: 'showPinyin', pinyinOnly: 'pinyinOnly', hollow: 'hollowGlyph', font: 'fontFamilyName' };
    for (const [key, property] of Object.entries(mappings)) if (spec[property] !== undefined) state[key] = spec[property];
    const reverse = (mapping, value, fallback) => Object.keys(mapping).find(key => mapping[key] === value) || fallback;
    state.grid = reverse(gridKinds, spec.grid, 'rice'); state.texture = reverse(backgrounds, spec.background, 'plain');
    state.header = Math.max(0, headerKinds.indexOf(spec.headerPreset)); state.practiceMode = spec.mode === 'Copy' ? 'copy' : 'trace';
    state.vertical = spec.orientation === 'Vertical'; state.intensity = reverse(intensities, spec.traceIntensity, spec.traceIntensity || 'Moderate');
    state.opacity = ({ VeryDark: 90, Dark: 75, MediumDark: 60, Moderate: 40, Light: 25, VeryLight: 15, White: 0, Outline: 30 })[state.intensity] ?? 40;
    state.template = catalog.templates.find(item => item.id === document_.moduleId) || null;
    if (spec.page) { const page = spec.page; state.margin = page.marginTopMm ?? 15; state.paper = page.widthMm === 297 ? page.heightMm === 210 ? 'a4Landscape' : 'a3' : page.widthMm === 216 ? 'letter' : 'a4'; }
    navigate('editor');
  }
  function navigate(route) { state.route = route; $('gallery').hidden = route !== 'gallery'; $('editor').hidden = route !== 'editor'; document.querySelectorAll('[data-route]').forEach(button => button.classList.toggle('active', button.dataset.route === route)); if (route === 'editor') renderEditor(); }
  function chooseTemplate(id) { const item = catalog.templates.find(item => item.id === id); if (!item?.enabled) return; state.template = item; if (item.text) state.text = item.text; state.grid = ({ mi: 'rice', tian: 'field', huigong: 'palace', plain: 'square' })[item.grid] || item.grid || 'rice'; state.practiceMode = item.mode || 'trace'; closeDrawer(); navigate('editor'); }
  function notice(key) { $('notice-text').textContent = t(key); $('notice').showModal(); }
  function openDrawer(kind) { state.drawer = kind; state.selectedContent = null; state.selectedTemplate = null; $('overlay').hidden = false; renderDrawer(); }
  function closeDrawer() { state.drawer = null; $('overlay').hidden = true; }
  function renderDrawer(preserve = false) {
    const content = state.drawer === 'content';
    const query = preserve ? $('drawer-query')?.value || '' : '';
    const selections = preserve ? Object.fromEntries([...document.querySelectorAll('[data-filter]')].map(filter => [filter.dataset.filter, filter.value])) : {};
    $('drawer').innerHTML = `<header><h2>${escape(t(content ? 'ContentLibrary' : 'TemplateGallery'))}</h2><button data-action="close-drawer">×</button></header><div class="drawer-filter"><input id="drawer-query" placeholder="${escape(t('Search'))}">${content ? ['subject', 'edition', 'grade', 'semester', 'unit'].map(name => `<select data-filter="${name}"><option value="">${escape(t('AllItems'))}</option>${[...new Set(catalog.contents.map(item => item[name]))].filter(Boolean).map(value => `<option value="${escape(value)}">${escape(value)}</option>`).join('')}</select>`).join('') : ''}</div><div class="drawer-results" id="drawer-results"></div><footer><button class="primary" id="apply-selection" data-action="apply-selection" disabled>${escape(t(content ? 'ApplyContent' : 'ApplyTemplate'))}</button></footer>`;
    $('drawer-query').value = query;
    for (const filter of document.querySelectorAll('[data-filter]')) filter.value = selections[filter.dataset.filter] || '';
    $('apply-selection').disabled = !state.selectedContent && !state.selectedTemplate;
    renderDrawerResults();
  }
  function renderDrawerResults() {
    const content = state.drawer === 'content', query = $('drawer-query').value.toLowerCase();
    const filters = [...document.querySelectorAll('[data-filter]')];
    const items = content ? catalog.contents.filter(item => (!query || (item.title + item.body + item.path).toLowerCase().includes(query)) && filters.every(filter => !filter.value || item[filter.dataset.filter] === filter.value)) : catalog.templates.filter(item => item.enabled && (!query || (item.id + item.name).toLowerCase().includes(query)));
    $('drawer-results').innerHTML = items.map((item) => `<button class="result" data-${content ? 'content-index' : 'select-template'}="${content ? catalog.contents.indexOf(item) : item.id}"><strong>${escape(content ? item.title : item.name)}</strong><small>${escape(content ? item.path : item.categories.map(categoryName).join(' · '))}</small><p>${escape((content ? item.body : item.description).slice(0, 80))}</p></button>`).join('');
    for (const result of document.querySelectorAll('.result')) result.classList.toggle('active', content ? catalog.contents[Number(result.dataset.contentIndex)] === state.selectedContent : result.dataset.selectTemplate === state.selectedTemplate);
  }
  function renderUpdate() {
    const status = { idle: 'NotChecked', checking: 'Checking', available: 'UpdateAvailable', downloading: 'Downloading', ready: 'DownloadReady', installed: 'InstallStarted', cancelled: 'DownloadCancelled' };
    $('update-status').textContent = t(status[state.update], state.update === 'available' ? 'v9.0.0' : state.update === 'downloading' ? state.progress : 'Zitie-win-x64-setup.exe');
    $('update-progress').hidden = state.update !== 'downloading'; $('update-progress').value = state.progress;
    let buttons = `<button data-update="check">${escape(t('CheckUpdates'))}</button>`;
    if (['available', 'cancelled'].includes(state.update)) buttons += `<button class="primary" data-update="download">${escape(t('Download'))}</button>`;
    if (state.update === 'downloading') buttons = `<button data-update="cancel">${escape(t('Cancel'))}</button>`;
    if (state.update === 'ready') buttons += `<button class="primary" data-update="install">${escape(t('Install'))}</button>`;
    $('update-actions').innerHTML = buttons;
  }
  document.addEventListener('click', event => {
    const button = event.target.closest('button'); if (!button) return;
    if (button.dataset.route) navigate(button.dataset.route);
    if (button.dataset.category !== undefined) { state.category = state.category === button.dataset.category ? '' : button.dataset.category; renderGallery(); }
    if (button.dataset.template) chooseTemplate(button.dataset.template);
    if (button.dataset.tab !== undefined) { state.tab = +button.dataset.tab; renderEditor(); }
    if (button.dataset.contentIndex !== undefined) { state.selectedContent = catalog.contents[Number(button.dataset.contentIndex)]; document.querySelectorAll('.result').forEach(result => result.classList.toggle('active', result === button)); $('apply-selection').disabled = false; }
    if (button.dataset.selectTemplate) { state.selectedTemplate = button.dataset.selectTemplate; document.querySelectorAll('.result').forEach(result => result.classList.toggle('active', result === button)); $('apply-selection').disabled = false; }
    if (button.dataset.action === 'content') openDrawer('content');
    if (button.dataset.action === 'close-drawer') closeDrawer();
    if (button.dataset.action === 'apply-selection') { if (state.selectedContent) { const entry = state.selectedContent; state.text = entry.body; state.title = entry.title; state.dynasty = entry.dynasty; state.author = entry.author; closeDrawer(); renderEditor(); } else if (state.selectedTemplate) chooseTemplate(state.selectedTemplate); }
    if (button.dataset.update === 'check') { state.update = 'checking'; renderUpdate(); setTimeout(() => { state.update = 'available'; renderUpdate(); }, 150); }
    if (button.dataset.update === 'download') { state.update = 'downloading'; state.progress = 0; renderUpdate(); updateTimer = setInterval(() => { state.progress += 10; if (state.progress >= 100) { clearInterval(updateTimer); state.update = 'ready'; } renderUpdate(); }, 60); }
    if (button.dataset.update === 'cancel') { clearInterval(updateTimer); state.update = 'cancelled'; renderUpdate(); }
    if (button.dataset.update === 'install') { state.update = 'installed'; renderUpdate(); }
  });
  document.addEventListener('input', event => { const input = event.target; if (input.dataset.state) { state[input.dataset.state] = input.type === 'checkbox' ? input.checked : (['range', 'number'].includes(input.type) || ['header', 'columns'].includes(input.dataset.state)) ? Number(input.value) : input.value; if (input.dataset.state === 'intensity') state.opacity = ({VeryDark:90,Dark:75,MediumDark:60,Moderate:40,Light:25,VeryLight:15,White:0,Outline:30})[input.value]; if (input.dataset.state === 'gridPreset') state.gridColor = ({red:'#b04a3f',black:'#242424',green:'#477b51'})[input.value]; if (input.dataset.state === 'textPreset') state.textColor = ({red:'#b04a3f',black:'#242424',green:'#477b51'})[input.value]; renderSheet(); } if (input.id === 'drawer-query') renderDrawerResults(); });
  document.addEventListener('change', event => { if (event.target.dataset.filter !== undefined) renderDrawerResults(); });
  $('mode').onclick = () => { state.mode = ({System:'Light',Light:'Dark',Dark:'System'})[state.mode]; applyAppearance(); };
  $('palette').onclick = event => { const button=event.target.closest('[data-palette]');if(button){state.palette=button.dataset.palette;applyAppearance();} };
  $('language').onclick = () => { $('language-menu').hidden=!$('language-menu').hidden; };
  $('language-menu').onclick = event => { const button=event.target.closest('[data-language]');if(button){state.language=button.dataset.language;$('language-menu').hidden=true;applyAppearance();localize();} };
  document.addEventListener('click',event=>{if(!event.target.closest('.language-picker'))$('language-menu').hidden=true;});
  $('font').oninput = event => { document.documentElement.style.setProperty('--font', event.target.value + 'px'); $('font-value').value = event.target.value; };
  $('gallery-search').oninput = renderGallery;
  $('gallery-zoom').oninput = event => { document.documentElement.style.setProperty('--card-scale', event.target.value / 100); $('gallery-zoom-value').value = event.target.value + '%'; };
  $('sheet-zoom').oninput = event => { document.documentElement.style.setProperty('--sheet-scale', event.target.value / 100); $('sheet-zoom-value').value = event.target.value + '%'; renderSheet(); };
  $('back').onclick = () => navigate('gallery'); $('template-switch').onclick = () => openDrawer('template');
  $('folder').onclick = () => notice('PrototypeFolderAction'); $('refresh').onclick = renderGallery;
  $('show-about').onclick = () => { renderUpdate(); $('about').showModal(); }; $('close-about').onclick = () => $('about').close();
  for (const button of document.querySelectorAll('[data-window]')) button.onclick = () => {
    const frame = document.querySelector('.window'), action = button.dataset.window;
    if (action === 'close') { frame.hidden = true; $('restore-window').hidden = false; }
    else frame.classList.toggle(action === 'minimize' ? 'is-minimized' : 'is-restored');
  };
  for (const button of document.querySelectorAll('[data-about]')) button.onclick = () => $('about').classList.toggle(button.dataset.about === 'minimize' ? 'is-minimized' : 'is-maximized');
  $('restore-window').onclick = () => { document.querySelector('.window').hidden = false; $('restore-window').hidden = true; };
  $('show-log').onclick = () => { $('log-panel').hidden = !$('log-panel').hidden; $('log').textContent = `Zitie __APP_VERSION__\n${t('TemplateCount', catalog.templates.length)}\n${t('ContentCount', catalog.contents.length)}`; }; $('close-log').onclick = () => $('log-panel').hidden = true;
  $('close-notice').onclick = () => $('notice').close();
  $('save-document').onclick = $('save-as').onclick = () => { const blob = new Blob([JSON.stringify(createDocument(), null, 2)], { type: 'application/json' }); const a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = 'practice.zitie.json'; a.click(); URL.revokeObjectURL(a.href); };
  $('save-template').onclick = () => notice('PrototypeFileAction');
  $('open-document').onclick = () => $('document-file').click(); $('document-file').onchange = async event => { const file = event.target.files[0]; if (!file) return; try { loadDocument(JSON.parse(await file.text())); } catch { notice('InvalidDocument'); } finally { event.target.value = ''; } };
  for (const id of ['batch-png', 'export-png', 'export-pdf']) $(id).onclick = () => notice('PrototypeExport');
  $('overlay').onclick = event => { if (event.target === $('overlay')) closeDrawer(); };
  document.addEventListener('keydown', event => { if (event.key === 'Escape') { closeDrawer(); if ($('about').open) $('about').close(); if ($('notice').open) $('notice').close(); } });
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', applyAppearance);
  window.zitie = { state, navigate, chooseTemplate, applyAppearance, localize, openDrawer, closeDrawer, renderSheet, createDocument, loadDocument, get unknownKeys() { return [...unknownKeys]; }, get templateCount() { return catalog.templates.length; }, get contentCount() { return catalog.contents.length; } };
  applyAppearance(); localize(); navigate('gallery');
})();
