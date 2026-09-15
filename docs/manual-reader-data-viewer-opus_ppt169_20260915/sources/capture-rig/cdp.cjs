'use strict';
// Reader Data Viewer の WebView2 を DevTools プロトコルで操作・撮影する小さな道具。
//   node cdp.cjs <port> targets
//   node cdp.cjs <port> eval <main|dialog> <js を base64>
//   node cdp.cjs <port> wait <main|dialog> <式を base64> [timeoutMs]
//   node cdp.cjs <port> shot <main|dialog> <out.png> [scale]
// main は主画面、dialog はダイアログ用の窓 (URL が #dialog)。撮影はその窓の中身だけで、
// Windows のタイトルバーや机の上は写らない。
const fs = require('fs');
const [port, action, target, ...rest] = process.argv.slice(2);
const delay = (ms) => new Promise((r) => setTimeout(r, ms));

class Cdp {
  constructor(url) {
    this.id = 1; this.pending = new Map(); this.ws = new WebSocket(url);
    this.ws.addEventListener('message', (e) => {
      const m = JSON.parse(String(e.data)); const p = this.pending.get(m.id);
      if (!p) return; this.pending.delete(m.id);
      if (m.error) p.reject(new Error(JSON.stringify(m.error))); else p.resolve(m.result);
    });
  }
  open() { return new Promise((res, rej) => { this.ws.addEventListener('open', res, { once: true }); this.ws.addEventListener('error', rej, { once: true }); }); }
  send(method, params) { const id = this.id++; return new Promise((res, rej) => { this.pending.set(id, { resolve: res, reject: rej }); this.ws.send(JSON.stringify({ id, method, params: params || {} })); }); }
  async evaluate(expression) {
    const r = await this.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (r.exceptionDetails) { const d = r.exceptionDetails.exception; throw new Error(d && d.description ? d.description : r.exceptionDetails.text); }
    return r.result.value;
  }
  close() { try { this.ws.close(); } catch (_) { } }
}

async function targets() { return (await fetch(`http://127.0.0.1:${port}/json/list`)).json(); }

async function connect(which) {
  const until = Date.now() + 60000;
  while (Date.now() < until) {
    try {
      const list = await targets();
      const t = list.find((x) => x.type === 'page' && x.url.includes('reader-data-viewer.local') && x.url.includes('#dialog') === (which === 'dialog'));
      if (t) { const c = new Cdp(t.webSocketDebuggerUrl); await c.open(); await c.send('Runtime.enable'); return c; }
    } catch (_) { }
    await delay(100);
  }
  throw new Error('target not found: ' + which);
}

async function main() {
  if (action === 'targets') { console.log(JSON.stringify(await targets(), null, 1)); return; }
  const client = await connect(target);
  try {
    if (action === 'eval') {
      const js = Buffer.from(rest[0], 'base64').toString('utf8');
      console.log(JSON.stringify(await client.evaluate(js)));
    } else if (action === 'wait') {
      const js = Buffer.from(rest[0], 'base64').toString('utf8');
      const timeout = Number(rest[1] || 20000); const start = Date.now(); let last = null;
      while (Date.now() - start < timeout) {
        try { last = await client.evaluate(js); if (last) { console.log(JSON.stringify({ ok: true, ms: Date.now() - start, value: last })); return; } } catch (e) { last = String(e); }
        await delay(80);
      }
      console.log(JSON.stringify({ ok: false, last })); process.exitCode = 2;
    } else if (action === 'shot') {
      const out = rest[0]; const scale = Number(rest[1] || 2);
      const size = await client.evaluate('({w:innerWidth,h:innerHeight,dpr:devicePixelRatio,title:document.title,url:location.href})');
      await client.send('Emulation.setDeviceMetricsOverride', { width: size.w, height: size.h, deviceScaleFactor: scale, mobile: false });
      await delay(350);
      const shot = await client.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
      await client.send('Emulation.clearDeviceMetricsOverride');
      fs.writeFileSync(out, Buffer.from(shot.data, 'base64'));
      const meta = Object.assign({ file: out, scale, takenAt: new Date().toISOString() }, size);
      fs.writeFileSync(out + '.json', JSON.stringify(meta, null, 1));
      console.log(JSON.stringify(meta));
    } else { throw new Error('unknown action ' + action); }
  } finally { client.close(); }
}
main().catch((e) => { console.error(String(e && e.stack || e)); process.exitCode = 1; });
