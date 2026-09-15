const fs=require('fs');
const lab='C:/repos-lab/shimane-20260915';
const {CdpClient,waitFor,delay}=require(lab+'/repo/build/webview2_cdp');
(async()=>{
 const pages=await(await fetch('http://127.0.0.1:18741/json/list')).json();
 const c=new CdpClient(pages.find(p=>p.url.includes('#dialog')).webSocketDebuggerUrl);await c.open();
 try{
 await c.evaluate(`(()=>{const s=document.querySelectorAll('#v-out select');s[0].value='PAYMAP.決済確認済';s[0].dispatchEvent(new Event('change'));s[1].value='equals';const v=document.querySelector('[data-field=filterFirst]');v.textContent='済';v.dispatchEvent(new Event('input',{bubbles:true}));const d=document.querySelector('[data-field=exportPath]');d.textContent='C:/repos-lab/shimane-20260915/sample-app/output/sample-paid-80.csv';d.dispatchEvent(new Event('input',{bubbles:true}));Array.from(document.querySelectorAll('#v-out .btn')).find(x=>x.textContent==='追加').click();})()`);
 await waitFor(c,`document.querySelector('#v-out tbody tr')!==null`,15000,'filter added');await delay(150);
 fs.writeFileSync(lab+'/evidence/16-export-filter.png',Buffer.from((await c.send('Page.captureScreenshot',{format:'png'})).data,'base64'));
 fs.writeFileSync(lab+'/evidence/export-observations.json',JSON.stringify({text:await c.evaluate('document.body.innerText')},null,2));
 await c.evaluate(`document.querySelector('#v-out .foot .btn').click();true`);await delay(1000);
 const out=lab+'/sample-app/output/sample-paid-80.csv';if(!fs.existsSync(out))throw Error('Export missing');fs.copyFileSync(out,lab+'/evidence/sample-paid-80.csv');console.log('EXPORT_CREATED '+fs.statSync(out).size);
 }finally{c.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
