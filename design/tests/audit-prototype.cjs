const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');

(async () => {
  const root = path.resolve(__dirname, '../..');
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
    check(frame.border >= 1 && frame.title >= 32 && frame.buttons === 3, 'Desktop window frame and caption buttons are rendered');
    await page.click('[data-window="minimize"]'); check(await page.locator('.window').evaluate(element => element.classList.contains('is-minimized')), 'Window minimize control changes the frame');
    await page.click('[data-window="minimize"]');
    await page.click('[data-window="maximize"]'); check(await page.locator('.window').evaluate(element => element.classList.contains('is-restored')), 'Window restore control changes the frame');
    await page.click('[data-window="maximize"]');
    await page.click('[data-window="close"]'); check(await page.locator('#restore-window').isVisible(), 'Closed prototype can be reopened'); await page.click('#restore-window');
    check(await page.evaluate(() => zitie.templateCount === 326 && zitie.contentCount === 292), 'Prototype contains the actual complete catalog');
    for (const language of ['zh-CN', 'en-US']) {
      await setLanguage(language);
      await setMode('System');
      for (const palette of ['DotNet', 'Aurora', 'Ember', 'Default', 'Aquatic', 'Desert', 'Dusk', 'NightSky']) {
        await setPalette(palette);
        for (const mode of ['Light', 'Dark']) {
          await setMode(mode);
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
      check((await page.locator('#about .metadata').textContent()).includes('2026-10-15 00:00:00'), 'About uses the fixed design build-time sample');
      check(await page.locator('#about .window-buttons button').count() === 3, 'About contains standard desktop window commands');
      await page.click('[data-update="check"]'); await page.waitForSelector('[data-update="download"]');
      await capture(`update-available-${language}`);
      await page.click('[data-update="download"]'); await page.waitForSelector('[data-update="install"]');
      await capture(`update-ready-${language}`); await page.click('[data-update="install"]');
      await page.click('#close-about');
    }
    await page.click('[data-route="gallery"]'); await page.fill('#gallery-search', 'nqez-945');
    check(await page.locator('.card').count() === 2, 'Partial ID search keeps both matching templates');
    await page.fill('#gallery-search', 'no-such-template-987654'); check(await page.locator('.card').count() === 0, 'Empty search state is reachable');
    check(external.length === 0, 'Standalone prototype must not load external resources: ' + external.join('; '));
    check(errors.length === 0, errors.join('; '));
    console.log(`PASS: ${states} states; 326 templates; 292 contents; 0 external requests; 0 JavaScript errors`);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
