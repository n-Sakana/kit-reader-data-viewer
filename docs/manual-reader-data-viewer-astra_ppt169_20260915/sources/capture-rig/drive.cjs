'use strict';
const fs=require('fs'), path=require('path');
const lab='C:/repos-lab/shimane-20260915';
const {CdpClient,waitFor,delay}=require(lab+'/repo/build/webview2_cdp');
const evidence=lab+'/evidence';
const records=[];
async function connect(dialog){
 const list=await (await fetch('http://127.0.0.1:18741/json/list')).json();
 const t=list.find(x=>x.type==='page' && x.url.includes('reader-data-viewer.local') && x.url.includes('#dialog')===dialog);
 if(!t) throw Error('No '+(dialog?'dialog':'main')+' surface');
 const c=new CdpClient(t.webSocketDebuggerUrl); await c.open(); await c.send('Runtime.enable'); return c;
}
async function run(){
 const app=await connect(false), modal=await connect(true);
 const click=(c,s)=>c.evaluate(`(()=>{const e=document.querySelector(${JSON.stringify(s)});if(!e)throw Error('missing control');e.click();return true})()`);
 const ready=()=>waitFor(app,`document.querySelector('#b-upd[aria-disabled=false]')!==null`,30000,'ready');
 const shown=(id)=>waitFor(modal,`document.querySelector('#${id}.show')!==null`,20000,id);
 const snap=async(c,name)=>{
   await delay(150);
   const content=await c.evaluate(`({url:location.href,width:innerWidth,height:innerHeight,text:document.body.innerText})`);
   const shot=await c.send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});
   fs.writeFileSync(path.join(evidence,name+'.png'),Buffer.from(shot.data,'base64'));
   records.push({name,...content}); console.log('CAPTURE '+name+' '+content.width+'x'+content.height);
 };
 const search=async(key)=>{
   await app.evaluate(`(()=>{let n=document.querySelector('#input');n.textContent=${JSON.stringify(key)};n.dispatchEvent(new Event('input',{bubbles:true}));})()`);
   await click(app,'#b-search');
 };
 try{
  const phase=process.argv[2];
  if(phase==='inspect'){
   console.log(JSON.stringify({main:await app.evaluate('document.body.innerText'),modal:await modal.evaluate('document.body.innerText')},null,2));
  }else if(phase==='initial'){
   await shown('v-send'); await snap(modal,'01-first-start');
   await click(modal,'#v-send .foot .btn'); await ready();
   await snap(app,'02-main-empty');
   await click(app,'#b-upd');await shown('v-upd');await snap(modal,'03-update');
   await click(modal,'#v-upd .foot .btn:last-child');await ready();
   await search('AA10000000AA');await waitFor(app,`document.querySelector('#judge').textContent==='済'`,20000,'paid');await snap(app,'04-paid');
   await search('UQ10005480SY');await waitFor(app,`document.querySelector('#judge').textContent==='未決済'`,20000,'unpaid');await snap(app,'05-unpaid');
   await search('ZZ20000080ZZ');await waitFor(app,`document.querySelector('[data-bind="s1.item1.row3"]').textContent==='' && document.querySelector('#judge').textContent==='済'`,20000,'missing third table');await snap(app,'06-third-missing');
   await search('AA99999999AA');await waitFor(app,`document.body.innerText.includes('見つかりません')`,20000,'not found');
   const shot=await app.send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});fs.writeFileSync(path.join(evidence,'07-not-found.png'),Buffer.from(shot.data,'base64'));
   records.push({name:'07-not-found',text:await app.evaluate('document.body.innerText')});
   await click(app,'#b-clear');await snap(app,'08-clear');
  }else if(phase==='state'){
   await ready();await search('AA10000000AA');await delay(300);await click(app,'#b-work');
   await waitFor(app,`document.body.innerText.includes('未送信 1 件')`,10000,'pending');await snap(app,'09-pending');
   await click(app,'#b-del');await shown('v-del');await snap(modal,'10-delete-unsent');await click(modal,'#v-del [data-modal-default=true]');await delay(350);await ready();
   await snap(app,'11-unsent-delete-zero');
   await click(app,'#b-send');await shown('v-send');await snap(modal,'12-send-confirm');await click(modal,'#v-send .foot .btn');await ready();
   await waitFor(app,`document.body.innerText.includes('未送信 0 件')`,10000,'sent');await snap(app,'13-sent');
   fs.copyFileSync(lab+'/sample-app/data/ReaderDataViewer-Ledger-PAYMAP.xlsx',evidence+'/sent-ledger.xlsx');
   await search('AA10000000AA');await delay(250);await click(app,'#b-work');await shown('v-send');await snap(modal,'14-return-confirm');
   await click(modal,'#v-send .foot .btn:last-child');await ready();
  }else if(phase==='screens'){
   await ready();await click(app,'#b-out');await shown('v-out');await snap(modal,'15-export');
   console.log('EXPORT INPUTS '+await modal.evaluate(`JSON.stringify(Array.from(document.querySelectorAll('#v-out input,#v-out select,#v-out button')).map(e=>({tag:e.tagName,id:e.id,text:e.textContent,field:e.dataset.field,value:e.value})))`));
  }else if(phase==='settings'){
   await ready();await click(app,'#b-set');await shown('v-set');await snap(modal,'17-settings');
   await click(modal,'#v-set .foot .btn:last-child');await ready();
  }else if(phase==='delete'){
   await ready();await click(app,'#b-del');await shown('v-del');await snap(modal,'18-delete');
   await click(modal,'#v-del [data-modal-default=true]');await delay(300);await ready();
   await waitFor(app,`document.body.innerText.includes('台帳件数 99')`,20000,'99 rows');await snap(app,'19-after-delete');
   fs.copyFileSync(lab+'/sample-app/data/ReaderDataViewer-Ledger-PAYMAP.xlsx',evidence+'/deleted-ledger.xlsx');
  }else if(phase==='close'){
   await app.evaluate(`window.chrome.webview.postMessage({type:'window',command:'close'});true`);
  }else throw Error('unknown phase '+phase);
 }finally{
  fs.writeFileSync(evidence+'/'+process.argv[2]+'-observations.json',JSON.stringify(records,null,2));
  app.close();modal.close();
 }
}
run().catch(e=>{console.error(e);process.exitCode=1;});
