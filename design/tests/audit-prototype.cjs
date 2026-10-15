const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');

(async () => {
  const root = path.resolve(__dirname, '../..');
  const productVersion = fs.readFileSync(path.join(root, 'Directory.Build.props'), 'utf8').match(/<Version>(\d+\.\d+\.\d+)<\/Version>/)?.[1];
  if (!productVersion) throw new Error('Product version is missing');
  const output = path.resolve(process.argv[2] || path.join(root, 'artifacts/prototype-audit'));
  const standalone = path.join(output, 'standalone');
  fs.mkdirSync(standalone, { recursive: true });
  const html = path.join(standalone, 'Zitie.html');
  fs.copyFileSync(path.join(root, 'design/index.html'), html);
  const url = pathToFileURL(html).href;
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1240, height: 840 }, deviceScaleFactor: 1 });
  async function checkAppearanceLanguage(language) {
    const matches=await page.evaluate(expected=>{
      const keys={DotNet:'DotNetPalette',Aurora:'AuroraPalette',Ember:'EmberPalette',Default:'DefaultPalette',Aquatic:'AquaticPalette',Desert:'DesertPalette',Dusk:'DuskPalette',NightSky:'NightSkyPalette'};
      const words=ZITIE_RESOURCES[expected].Zitie.Text,mode=document.getElementById('mode'),languageButton=document.getElementById('language');
      return document.documentElement.lang===expected && mode.title===words[mode.dataset.mode+'Mode'] && mode.getAttribute('aria-label')===mode.title && languageButton.title===words.LanguageName && languageButton.getAttribute('aria-label')===languageButton.title && [...document.querySelectorAll('button[data-palette]')].every(button=>button.title===words[keys[button.dataset.palette]] && button.getAttribute('aria-label')===button.title);
    },language);
    if(!matches)throw Error('Title-bar language, tooltips and accessibility labels did not switch immediately: '+language);
  }
  const setLanguage=async value=>{await page.locator('#language').click();await page.locator('#language-menu [data-language="'+value+'"]').click();await checkAppearanceLanguage(value);};
  const setMode=async value=>{for(let index=0;index<3;index++){if(await page.locator('#mode').getAttribute('data-mode')===value){await checkAppearanceLanguage(await page.locator('html').getAttribute('lang'));return;}await page.locator('#mode').click();}if(await page.locator('#mode').getAttribute('data-mode')!==value)throw Error('Theme cycle failed');await checkAppearanceLanguage(await page.locator('html').getAttribute('lang'));};
  const setPalette=async value=>{await page.locator('button[data-palette="'+value+'"]').click();};
  const errors = [], external = [];
  let states = 0;
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (request.url() !== url && !request.url().startsWith('data:')) external.push(request.url()); });
  function check(condition, message) { if (!condition) throw new Error(message); }
  async function capture(name) {
    await page.screenshot({ path: path.join(output, name + '.png') }); states++;
    check((await page.evaluate(() => window.zitie.unknownKeys)).length === 0, 'Every visible control uses an existing resource key');
    check(errors.length === 0, 'No JavaScript exceptions: ' + errors.join('; '));
  }
  try {
    await page.goto(url);
    await page.evaluate(() => document.fonts.ready);
    const frame = await page.locator('.window').evaluate(element => ({ border: parseFloat(getComputedStyle(element).borderTopWidth), title: element.querySelector('.titlebar').getBoundingClientRect().height, buttons: element.querySelectorAll('.titlebar .window-buttons button').length }));
    check(frame.border >= 1 && frame.title === 44 && frame.buttons === 3, 'Desktop window frame and caption buttons match the native metrics');
    check(await page.evaluate(()=>{
      const rail=document.querySelector('.rail').getBoundingClientRect(),card=document.querySelector('.card').getBoundingClientRect();
      const chips=[...document.querySelectorAll('#categories button')],style=getComputedStyle(chips[0]);
      const rows=Object.values(chips.reduce((result,item)=>{const y=item.getBoundingClientRect().y;(result[y]??=[]).push(item.textContent);return result;},{})).map(items=>items.length);
      return rail.width===64 && card.x===91 && card.y===233 && card.width===244 && card.height===334
        && style.height==='30px' && style.paddingLeft==='13px' && style.marginRight==='6px' && style.fontWeight==='600'
        && JSON.stringify(rows)===JSON.stringify([12,6]);
    }),'Chinese gallery keeps the native navigation, category rows and card geometry');
    check(await page.locator('.titlebar').evaluate(element=>{
      const brand=element.querySelector('.brand').getBoundingClientRect(),palette=element.querySelector('#palette').getBoundingClientRect(),mode=element.querySelector('#mode').getBoundingClientRect(),language=element.querySelector('#language').getBoundingClientRect();
      return Math.abs(mode.width-30)<1 && Math.abs(language.width-30)<1 && palette.left-brand.right<=14 && mode.left-palette.right<=9 && language.left-mode.right<=9 && [...element.querySelectorAll('.window-buttons button')].every(button=>parseFloat(getComputedStyle(button).width)===32);
    }),'Title-bar icons keep native widths and compact spacing beside the brand');
    check(JSON.stringify(await page.locator('.titlebar .window-buttons button').evaluateAll(buttons=>buttons.map(button=>button.dataset.window)))===JSON.stringify(['minimize','maximize','close']),'Each caption button has the correct identity');
    await page.click('[data-window="minimize"]'); check(await page.locator('.window').evaluate(element => element.classList.contains('is-minimized')), 'Window minimize control changes the frame');
    check(!(await page.locator('.window > .body').isVisible()),'Minimize hides the main content');
    await page.click('[data-window="minimize"]');
    check(await page.locator('.window > .body').isVisible(),'Minimized window can be restored');
    await page.click('[data-window="maximize"]'); check(await page.locator('.window').evaluate(element => element.classList.contains('is-restored') && element.getBoundingClientRect().width<1240), 'Window restore control changes the frame and layout');
    await page.click('[data-window="maximize"]');
    check(await page.locator('.window').evaluate(element=>Math.abs(element.getBoundingClientRect().width-1240)<2),'Maximize returns to the full layout');
    await page.click('[data-window="close"]'); check(await page.locator('#restore-window').isVisible() && !(await page.locator('.window').isVisible()), 'Close hides the window and provides a reopen action'); await page.click('#restore-window');
    check(await page.locator('.window').isVisible(),'Closed prototype can be reopened');
    check(await page.evaluate(() => zitie.templateCount === 326 && zitie.contentCount === 292), 'Prototype contains the actual complete catalog');
    check(JSON.stringify(await page.locator('#palette button[data-palette]').evaluateAll(buttons=>buttons.map(button=>button.dataset.palette)))===JSON.stringify(['DotNet','Aurora','Ember','Default','Aquatic','Desert','Dusk','NightSky']),'Palette order matches the native title bar');
    const swatches=await page.locator('#palette button i').evaluateAll(dots=>dots.map(dot=>[getComputedStyle(dot).backgroundColor,getComputedStyle(dot).backgroundImage]));
    check(swatches[0][0]==='rgb(81, 43, 212)' && swatches[1][1].startsWith('linear-gradient('),'Fixed .NET and Aurora swatches match the native title bar');
    for (const language of ['zh-CN', 'en-US']) {
      await setLanguage(language);
      await setMode('System');
      for (const palette of ['DotNet', 'Aurora', 'Ember', 'Default', 'Aquatic', 'Desert', 'Dusk', 'NightSky']) {
        await setPalette(palette);
        for (const mode of ['Light', 'Dark']) {
          await setMode(mode);
          check(JSON.stringify(await page.locator('#palette button i').evaluateAll(dots=>dots.map(dot=>[getComputedStyle(dot).backgroundColor,getComputedStyle(dot).backgroundImage])))===JSON.stringify(swatches),'Palette swatches remain stable across theme modes');
          await page.click('[data-route="gallery"]');
          check(await page.locator('.card').count() === 326, 'All templates remain reachable');
          await capture(`gallery-${language}-${palette}-${mode}`);
          await page.evaluate(() => zitie.chooseTemplate('nqez-945'));
          check(await page.locator('#sheet-grid .gcell').count() > 0, 'Editor renders practice cells');
          await capture(`editor-${language}-${palette}-${mode}`);
        }
      }
      await setPalette('DotNet');
      await setMode('Light');
      for (let tab = 0; tab < 5; tab++) { await page.click(`[data-tab="${tab}"]`); await capture(`parameters-${language}-${tab}`); }
      await page.click('[data-tab="0"]');
      await page.fill('textarea[data-state="text"]', '天地玄黄');
      check(await page.locator('#sheet-grid').textContent().then(text => text.includes('天') && !text.includes('床')), 'Text edits change the rendered sheet');
      await page.click('[data-tab="1"]');
      await page.selectOption('[data-state="grid"]', 'field');
      await page.uncheck('[data-state="groupWords"]');
      await page.selectOption('[data-state="columns"]', '5');
      await page.fill('[data-state="blankLines"]', '1');
      check(await page.locator('#sheet-grid .gcell').count() === 35, 'Empty rows add visible grid cells between practice rows');
      await page.check('[data-state="groupWords"]');
      check(await page.locator('#sheet-grid .gcell').count() === 4, 'Word grouping renders one consecutive cell per character');
      await capture(`layout-controls-${language}`);
      await page.click('[data-tab="4"]'); await page.check('[data-state="pinyin"]');
      check(await page.locator('#sheet-grid .pinyin').first().textContent() === 'tiān', 'Pinyin contains real catalog data rather than a template expression');
      await capture(`pinyin-${language}`);
      const saved = page.waitForEvent('download'); await page.click('#save-document');
      const download = await saved;
      const document = JSON.parse(fs.readFileSync(await download.path(), 'utf8'));
      check(document.version === 1 && document.spec.grid === 'Tian' && document.spec.text === '天地玄黄', 'Saved document follows the desktop version/moduleId/spec schema');
      check(!('palette' in document) && document.spec.groupByWord && document.spec.blankCellLineCount === 1, 'Document persists editor parameters without window preferences');
      await page.evaluate(() => { zitie.state.text = 'changed'; zitie.state.groupWords = false; zitie.renderSheet(); });
      await page.locator('#document-file').setInputFiles({ name: 'restored.zitie.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(document)) });
      await page.waitForFunction(() => zitie.state.text === '天地玄黄' && zitie.state.groupWords && zitie.state.pinyin);
      check(await page.evaluate(() => zitie.state.text === '天地玄黄' && zitie.state.groupWords && zitie.state.pinyin), 'Desktop-compatible document reopens and restores parameters');
      await capture(`document-restored-${language}`);
      await page.click('[data-tab="0"]');
      await page.click('[data-action="content"]');
      await page.fill('#drawer-query', '静夜思');
      check(await page.locator('.result').count() > 0, 'Actual content search returns a selectable poem');
      await page.locator('.result').first().click();
      await setLanguage(language === 'zh-CN' ? 'en-US' : 'zh-CN');
      check(await page.locator('#drawer-query').inputValue() === '静夜思' && await page.locator('.result.active').count() === 1 && await page.locator('#apply-selection').isEnabled(), 'Open drawer retains filters and selection during language changes');
      await setLanguage(language); await capture(`content-library-${language}`);
      await page.click('[data-action="apply-selection"]');
      check(await page.locator('textarea[data-state="text"]').inputValue().then(text => text.includes('床前')), 'Content selection updates the practice text');
      await page.click('#template-switch'); await page.fill('#drawer-query', 'nqez-945'); await capture(`template-picker-${language}`); await page.keyboard.press('Escape');
      await page.click('#show-about'); await capture(`about-${language}`);
      check((await page.locator('#about .about-brand').textContent()).includes('v' + productVersion), 'Prototype and product versions match');
      check((await page.locator('#about .metadata').textContent()).includes('2026-10-15 00:00:00'), 'About uses the fixed design build-time sample');
      check(await page.locator('#about .window-buttons button').count() === 3, 'About contains standard desktop window commands');
      check(await page.locator('#about [data-about="minimize"]').isDisabled() && await page.locator('#about [data-about="maximize"]').isDisabled(),'About caption capabilities match the native window');
      await page.locator('#about [data-about="minimize"]').evaluate(button=>button.click());
      check(!(await page.locator('#about').evaluate(element=>element.classList.contains('is-minimized'))),'Disabled About caption buttons do not change its layout');
      await page.click('[data-update="check"]'); await page.waitForSelector('[data-update="download"]');
      await capture(`update-available-${language}`);
      await page.click('[data-update="download"]'); await page.waitForSelector('[data-update="install"]');
      await capture(`update-ready-${language}`); await page.click('[data-update="install"]');
      await page.click('#close-about');
      check(!(await page.locator('#about').isVisible()),'About close hides its window');
    }
    await page.click('[data-route="gallery"]'); await page.fill('#gallery-search', 'nqez-945');
    check(await page.locator('.card').count() === 2, 'Partial ID search keeps both matching templates');
    await page.fill('#gallery-search', 'no-such-template-987654'); check(await page.locator('.card').count() === 0, 'Empty search state is reachable');
    check(external.length === 0, 'Standalone prototype must not load external resources: ' + external.join('; '));
    check(errors.length === 0, errors.join('; '));
    console.log(`PASS: ${states} states; 326 templates; 292 contents; 0 external requests; 0 JavaScript errors`);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
