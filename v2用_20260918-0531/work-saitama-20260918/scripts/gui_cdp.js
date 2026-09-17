'use strict';
// Drives the invisible probe window over the DevTools protocol: searches,
// reads the judgment band and the fields, opens the settings / update /
// delete dialogs and reads them, saves a file-name change, then closes the app.
const fs = require('fs');
const path = require('path');
const helper = require('C:/repos/kit/reader-data-viewer/build/webview2_cdp.js');
const { CdpClient, delay, harnessSource, waitFor } = helper;

const port = Number(process.argv[2]);
const outDir = process.argv[3];
const results = { steps: [] };
function note(name, value) {
  results.steps.push({ name, value });
  process.stdout.write(`## ${name}\n${JSON.stringify(value, null, 1)}\n`);
  try { fs.mkdirSync(outDir, { recursive: true }); fs.writeFileSync(path.join(outDir, 'gui-results.json'), JSON.stringify(results, null, 1)); } catch (_) { }
}

async function listTargets() {
  const response = await fetch(`http://127.0.0.1:${port}/json/list`);
  return response.json();
}
async function connectTarget(predicate, timeoutMs, label) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const entries = await listTargets();
      const entry = entries.find((e) => e.type === 'page' && predicate(e));
      if (entry) {
        const client = new CdpClient(entry.webSocketDebuggerUrl);
        await client.open();
        await client.send('Runtime.enable');
        await client.send('Page.enable');
        await client.send('Page.addScriptToEvaluateOnNewDocument', { source: harnessSource });
        try { await client.evaluate(harnessSource); } catch (_) { }
        await waitFor(client, `location.hostname === 'reader-data-viewer.local' && window.__rdvTestHookInstalled === true`, timeoutMs, label + ' hook');
        return client;
      }
    } catch (_) { }
    await delay(100);
  }
  throw new Error('no target for ' + label);
}
async function shot(client, name) {
  try {
    const r = await client.send('Page.captureScreenshot', { format: 'png' });
    const file = path.join(outDir, name + '.png');
    fs.writeFileSync(file, Buffer.from(r.data, 'base64'));
    return file;
  } catch (error) { return 'screenshot failed: ' + error.message; }
}
const readMain = `(() => {
  const s = document.querySelector('.stage');
  const v = (id) => { const n = s.querySelector('[data-bind="' + id + '"]'); return n ? n.textContent : null; };
  const band = s.querySelector('.band .ok');
  const sp = Array.from(s.querySelectorAll('.sb .sp')).map((n) => n.textContent);
  const labels = Array.from(s.querySelectorAll('.row.dynamic-row')).map((r) => (r.hidden ? '(hidden)' : r.querySelector('label').textContent));
  const legends = Array.from(s.querySelectorAll('fieldset legend')).map((l) => l.textContent);
  return { band: band ? band.textContent : null, bandClass: band ? band.className : null, labels, legends, searchLabel: s.querySelector('label[for=input]').textContent, bandLabel: s.querySelector('.band-label').textContent,
    status: sp, key: v('searchKey'), input: s.querySelector('#input').textContent,
    work: s.querySelector('#b-work').textContent, send: s.querySelector('#b-send').textContent,
    fields: { userId: v('userId'), userName: v('userName'), userCategory: v('userCategory'), applicationDate: v('applicationDate'),
      applicationNumber: v('applicationNumber'), cardNumber: v('cardNumber'), applicantName: v('applicantName'),
      birthDate: v('birthDate'), qualification: v('qualification'), office: v('office'),
      remarks: (v('remarks') || '').slice(0, 24), plan: v('plan') } };
})()`;
async function search(main, key) {
  await main.evaluate(`(() => { const i = document.querySelector('#input'); i.focus(); i.textContent = ${JSON.stringify(key)};
    i.dispatchEvent(new InputEvent('input', { bubbles: true }));
    i.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true })); return true; })()`);
  await delay(900);
  return main.evaluate(readMain);
}
async function click(client, selector) {
  await client.evaluate(`(() => { const b = document.querySelector(${JSON.stringify(selector)}); b.focus(); b.click(); return true; })()`);
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const mainPage = await connectTarget((e) => !String(e.url).includes('#dialog'), 90000, 'main page');
  await waitFor(mainPage, `document.querySelector('.stage.runtime #b-search[aria-disabled=false]') !== null`, 90000, 'ready');
  note('startup', await mainPage.evaluate(readMain));
  note('startup shot', await shot(mainPage, 'startup'));
  const dialogPage = await connectTarget((e) => String(e.url).includes('#dialog'), 30000, 'dialog page');

  note('search found (upper)', await search(mainPage, 'AB10000001CA'));
  note('search shot', await shot(mainPage, 'search-found'));
  note('search found (lower case)', await search(mainPage, 'ab10000005ma'));
  note('search found (full width)', await search(mainPage, '\uff21\uff22\uff11\uff10\uff10\uff10\uff10\uff10\uff10\uff11\uff23\uff21'));
  note('search found by 03-side number', await search(mainPage, '\u4e19\u30aa\u30abXC26-20002'));
  note('search found by 02-side number with spaces', await search(mainPage, '  \u4e19\u30aa\u30abXC26-10002 '));
  note('search found by 03-side number in full width', await search(mainPage, '\u4e19\u30aa\u30ab\uff38\uff23\uff12\uff16\uff0d\uff12\uff10\uff10\uff10\uff12'));
  note('search not found', await search(mainPage, 'AB99999999ZZ'));
  note('not found shot', await shot(mainPage, 'search-notfound'));
  note('search invalid', await search(mainPage, 'abc'));
  note('invalid shot', await shot(mainPage, 'search-invalid'));
  await click(mainPage, '#b-clear'); await delay(400);
  note('after clear', await mainPage.evaluate(readMain));

  // settings dialog
  await click(mainPage, '#b-set');
  await waitFor(dialogPage, `document.querySelector('#v-set').classList.contains('show')`, 30000, 'settings open');
  await delay(600);
  note('settings dialog', await dialogPage.evaluate(`(() => {
    const rows = Array.from(document.querySelectorAll('#v-set [data-slot=tables] .kv')).map((r) => ({
      label: r.querySelector('label').textContent, file: r.querySelector('[data-field]').textContent, match: r.querySelector('select').value }));
    const legends = Array.from(document.querySelectorAll('#v-set legend')).map((l) => l.textContent);
    return { hint: document.querySelector('#v-set .hint').textContent, legends, rows,
      dataDir: document.querySelector('#v-set [data-field=dataDir]').textContent,
      pattern: document.querySelector('#v-set [data-field=pattern]').textContent.slice(0, 60) };
  })()`));
  note('settings shot', await shot(dialogPage, 'settings'));
  // a name nothing answers to is refused in the dialog, with the reason
  await dialogPage.evaluate(`(() => {
    const app = document.querySelector('#v-set [data-field="table:受付"]'); app.textContent = '99_\u3042\u308a\u307e\u305b\u3093.csv';
    document.querySelector('#v-set [data-command=execute]').click(); return true; })()`);
  await waitFor(dialogPage, `!document.querySelector('#v-set .setting-error').hidden`, 30000, 'settings error shown');
  note('settings refuses a missing file', await dialogPage.evaluate(`({ error: document.querySelector('#v-set .setting-error').textContent, stillOpen: document.querySelector('#v-set').classList.contains('show'), focused: document.activeElement && document.activeElement.getAttribute('data-field') })`));
  // change TXN to a prefix name, PAY to a name with other width, restore APP, and save
  await dialogPage.evaluate(`(() => {
    const txn = document.querySelector('#v-set [data-field="table:取引"]'); txn.textContent = '01_\u53d6\u5f15\u30c7\u30fc\u30bf';
    document.querySelector('#v-set select[data-match="取引"]').value = 'prefix';
    const pay = document.querySelector('#v-set [data-field="table:決済"]'); pay.textContent = ' \uff10\uff12_\u6c7a\u6e08\u30c7\u30fc\u30bf.CSV ';
    const app = document.querySelector('#v-set [data-field="table:受付"]'); app.textContent = '03_\u53d7\u4ed8\u30c7\u30fc\u30bf.csv';
    document.querySelector('#v-set [data-command=execute]').click(); return true; })()`);
  await waitFor(dialogPage, `!document.querySelector('#v-set').classList.contains('show')`, 30000, 'settings closed');
  await delay(1500);
  note('during refresh after settings save', await mainPage.evaluate(readMain));
  // an error or question raised by the refresh shows as a confirm dialog: read it and answer OK
  for (let i = 0; i < 20; i++) {
    const confirm = await dialogPage.evaluate(`(() => { const v = document.querySelector('#v-send'); if (!v || !v.classList.contains('show')) { return null; }
      return { title: v.querySelector('.ttl').textContent, body: v.querySelector('.modal-message').textContent }; })()`);
    if (confirm) { note('confirm after settings save', confirm); await click(dialogPage, '#v-send .foot .btn'); await delay(500); }
    const ready = await mainPage.evaluate(`document.querySelector('.stage.runtime #b-search[aria-disabled=false]') !== null`);
    if (ready) { break; }
    await delay(500);
  }
  await waitFor(mainPage, `document.querySelector('.stage.runtime #b-search[aria-disabled=false]') !== null`, 90000, 'ready after settings');
  await delay(300);
  note('after settings save', await mainPage.evaluate(readMain));
  note('search after file rename', await search(mainPage, 'AB10000003CF'));

  // update dialog
  await click(mainPage, '#b-upd');
  await waitFor(dialogPage, `document.querySelector('#v-upd').classList.contains('show')`, 30000, 'update open');
  await delay(800);
  note('update dialog', await dialogPage.evaluate(`(() => {
    const rows = Array.from(document.querySelectorAll('#v-upd [data-slot=inputs] tbody tr')).map((r) => Array.from(r.children).map((c) => c.textContent));
    const steps = document.querySelectorAll('#v-upd [data-slot=steps] tbody tr').length;
    const run = document.querySelector('#v-upd [data-command=execute]');
    return { title: document.querySelector('#v-upd .ttl').textContent, inputs: rows, steps, canRun: run.getAttribute('aria-disabled') };
  })()`));
  note('update shot', await shot(dialogPage, 'update'));
  await click(dialogPage, '#v-upd [data-command=cancel]');
  await waitFor(dialogPage, `!document.querySelector('#v-upd').classList.contains('show')`, 30000, 'update closed');
  await delay(500);

  // delete dialog
  await click(mainPage, '#b-del');
  await waitFor(dialogPage, `document.querySelector('#v-del').classList.contains('show')`, 30000, 'delete open');
  await delay(800);
  note('delete dialog', await dialogPage.evaluate(`(() => {
    const rows = Array.from(document.querySelectorAll('#v-del [data-slot=inputs] tbody tr')).map((r) => Array.from(r.children).map((c) => c.textContent));
    const steps = Array.from(document.querySelectorAll('#v-del [data-slot=steps] tbody tr')).map((r) => Array.from(r.children).map((c) => c.textContent).join(' | '));
    return { title: document.querySelector('#v-del .ttl').textContent, inputs: rows, steps };
  })()`));
  note('delete shot', await shot(dialogPage, 'delete'));
  await click(dialogPage, '#v-del [data-command=cancel]');
  await waitFor(dialogPage, `!document.querySelector('#v-del').classList.contains('show')`, 30000, 'delete closed');
  await delay(300);

  // close the app the way the title bar would
  await mainPage.evaluate(`(window.__rdvTestNativePost || window.chrome.webview.postMessage)({ type: 'window', command: 'close' }); true`);
  fs.writeFileSync(path.join(outDir, 'gui-results.json'), JSON.stringify(results, null, 1));
  mainPage.close(); dialogPage.close();
}

main().then(() => { setTimeout(() => process.exit(0), 200); }).catch((error) => {
  process.stderr.write((error.stack || String(error)) + '\n');
  fs.writeFileSync(path.join(outDir, 'gui-results.json'), JSON.stringify(results, null, 1));
  setTimeout(() => process.exit(1), 200);
});
