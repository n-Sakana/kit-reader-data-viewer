const fs=require('fs');
const lab='C:/repos-lab/shimane-20260915';
const {CdpClient,waitFor,delay}=require(lab+'/repo/build/webview2_cdp');
(async()=>{
 const pages=await(await fetch('http://127.0.0.1:18741/json/list')).json();
 const app=new CdpClient(pages.find(p=>p.type==='page'&&!p.url.includes('#dialog')).webSocketDebuggerUrl);
 const modal=new CdpClient(pages.find(p=>p.url.includes('#dialog')).webSocketDebuggerUrl);await app.open();await modal.open();
 try{
 await waitFor(app,`document.querySelector('#b-upd[aria-disabled=false]')!==null`,30000,'ready');
 await app.evaluate(`(()=>{const e=document.querySelector('#input');e.textContent='AA10000000AA';e.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('#b-search').click();})()`);
 await waitFor(modal,`document.querySelectorAll('#v-cand.show tbody tr').length===2`,20000,'two candidates');await delay(150);
 fs.writeFileSync(lab+'/evidence/21-candidates.png',Buffer.from((await modal.send('Page.captureScreenshot',{format:'png'})).data,'base64'));
 const before=await modal.evaluate('document.body.innerText');
 await modal.evaluate(`document.querySelector('#v-cand tbody tr').dispatchEvent(new MouseEvent('dblclick',{bubbles:true}));true`);
 await waitFor(app,`document.querySelector('#judge').textContent==='済'`,20000,'chosen');await delay(150);
 const after=await app.evaluate('document.body.innerText');
 fs.writeFileSync(lab+'/evidence/candidate-observations.json',JSON.stringify({before,after},null,2));
 console.log('CANDIDATES 2; selected one; '+after.includes('未送信 0 件'));
 }finally{app.close();modal.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
