const fs=require('fs');const lab='C:/repos-lab/shimane-20260915';
const {CdpClient}=require(lab+'/repo/build/webview2_cdp');
(async()=>{const ps=await(await fetch('http://127.0.0.1:18741/json/list')).json();const c=new CdpClient(ps.find(x=>x.url.includes('#dialog')).webSocketDebuggerUrl);await c.open();try{
const text=await c.evaluate('document.body.innerText');if(!text.includes('定義された処理で台帳に変更があります'))throw Error('Unexpected prompt');
fs.writeFileSync(lab+'/evidence/20-update-confirm.png',Buffer.from((await c.send('Page.captureScreenshot',{format:'png'})).data,'base64'));
fs.writeFileSync(lab+'/evidence/update-confirm-observations.json',JSON.stringify({text},null,2));
await c.evaluate(`document.querySelector('#v-send .foot .btn:last-child').click();true`);console.log('DECLINED fixture overwrite');
}finally{c.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
