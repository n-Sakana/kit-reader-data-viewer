const fs=require('fs');
const lab='C:/repos-lab/shimane-20260915';
const {CdpClient,waitFor,delay}=require(lab+'/repo/build/webview2_cdp');
(async()=>{
 const pages=await(await fetch('http://127.0.0.1:18741/json/list')).json();
 const app=new CdpClient(pages.find(p=>p.type==='page'&&!p.url.includes('#dialog')).webSocketDebuggerUrl);
 const modal=new CdpClient(pages.find(p=>p.url.includes('#dialog')).webSocketDebuggerUrl);await app.open();await modal.open();
 try{
 await app.evaluate(`document.querySelector('#b-upd').click();true`);
 await waitFor(modal,`document.querySelector('#v-upd.show')!==null`,15000,'update');
 await modal.evaluate(`document.querySelector('#v-upd [data-modal-default=true]').click();true`);
 await waitFor(app,`document.querySelector('#b-upd[aria-disabled=false]')!==null && document.body.innerText.includes('台帳件数 100')`,30000,'restored 100');await delay(250);
 fs.writeFileSync(lab+'/evidence/20-after-reimport.png',Buffer.from((await app.send('Page.captureScreenshot',{format:'png'})).data,'base64'));
 fs.copyFileSync(lab+'/sample-app/data/ReaderDataViewer-Ledger-PAYMAP.xlsx',lab+'/evidence/reimported-ledger.xlsx');
 console.log(await app.evaluate('document.body.innerText'));
 }finally{app.close();modal.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
