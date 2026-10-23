"""Temporary, local-only MyDay backup handoff. Python 3.11+, no packages/accounts.

python tools/lan-transfer.py --host 192.168.0.10 --file MyDay-backup.json
python tools/lan-transfer.py --host 192.168.0.10 --receive
No automatic import, no cloud, no persistent upload storage. Close with Ctrl+C.
"""
import argparse
import hashlib
import html
import ipaddress
import json
from pathlib import Path
import secrets
import socket
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import webbrowser
from vendor.qrcodegen import QrCode

MAX_BYTES=128*1024*1024
ROOT=Path(__file__).resolve().parents[1]
LIFETIME=json.loads((ROOT/'shared/journal-tools.json').read_text(encoding='utf-8'))['transferLifetimeSeconds']

def private_host(value):
    ip=ipaddress.ip_address(value)
    if ip.version!=4 or not (ip.is_loopback or ip in ipaddress.ip_network('10.0.0.0/8') or ip in ipaddress.ip_network('172.16.0.0/12') or ip in ipaddress.ip_network('192.168.0.0/16')):
        raise ValueError('같은 Wi-Fi의 IPv4 사설 주소를 지정하세요. 공개 주소와 0.0.0.0은 사용할 수 없어요.')
    return str(ip)

def validate_backup(data):
    if len(data)>MAX_BYTES: raise ValueError('백업은 128MB 이하여야 합니다.')
    root=json.loads(data)
    if not isinstance(root,dict) or type(root.get('Version')) is not int or not 1<=root['Version']<=8 or not isinstance(root.get('Days'),(dict,list)):
        raise ValueError('MyDay 공통 JSON 백업을 선택하세요. 최종 내용 검사는 앱에서 가져올 때 진행해요.')
    return data

def qr_svg(text):
    qr=QrCode.encode_text(text,QrCode.Ecc.MEDIUM)
    paths=' '.join(f'M{x+4},{y+4}h1v1h-1z' for y in range(qr.get_size()) for x in range(qr.get_size()) if qr.get_module(x,y))
    size=qr.get_size()+8
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {size} {size}" role="img" aria-label="기기 연결 QR"><rect width="100%" height="100%" fill="white"/><path d="{paths}" fill="black"/></svg>'

class TransferServer(ThreadingHTTPServer):
    daemon_threads=True
    def __init__(self,address,data=None,lifetime=LIFETIME):
        self.token=secrets.token_urlsafe(32);self.admin=secrets.token_urlsafe(32)
        self.data=data;self.received=None;self.expiry=time.monotonic()+lifetime;self.lock=threading.Lock();self.downloads=0
        super().__init__(address,Handler)
        self.origin=f'http://{address[0]}:{self.server_port}'
    @property
    def invite(self): return self.origin+'/#'+self.token
    @property
    def owner(self): return self.origin+'/admin#'+self.admin

STYLE='''body{margin:0;background:#f7f5f0;color:#343c32;font:16px system-ui;line-height:1.7}main{max-width:600px;margin:36px auto;padding:24px}h1{font-family:Georgia,serif}button,a.button{display:inline-block;margin:8px 0;padding:12px 18px;background:#677354;color:white;border:0;border-radius:14px;text-decoration:none;font:inherit;cursor:pointer}section{padding:20px;background:white;border-radius:18px;margin:18px 0}svg{width:260px;max-width:100%}input{display:block;max-width:100%;margin:12px 0}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:inherit}.hint{color:#65705d;font-size:14px}'''

class Handler(BaseHTTPRequestHandler):
    server_version='MyDayLocal/1'
    def log_message(self,*args): pass  # Never log invitation tokens or diary data.
    def setup(self):
        super().setup();self.connection.settimeout(20)
    def reply(self,status,data=b'',kind='text/plain; charset=utf-8',download=False):
        self.send_response(status);self.send_header('Content-Type',kind);self.send_header('Content-Length',str(len(data)))
        self.send_header('Cache-Control','no-store');self.send_header('Referrer-Policy','no-referrer');self.send_header('X-Content-Type-Options','nosniff')
        self.send_header('Content-Security-Policy',"default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; img-src 'self'; frame-ancestors 'none'; form-action 'self'")
        self.send_header('Connection','close')
        if download: self.send_header('Content-Disposition','attachment; filename="MyDay-transfer.json"')
        self.end_headers();self.wfile.write(data)
    def authorized(self,owner=False):
        if time.monotonic()>=self.server.expiry: self.reply(410,'전송 시간이 끝났어요. 컴퓨터에서 새 전송을 열어 주세요.'.encode());return False
        if self.headers.get('Host')!=self.server.origin.removeprefix('http://'): self.reply(403);return False
        if self.headers.get('Origin') not in (None,self.server.origin): self.reply(403);return False
        token=self.headers.get('Authorization','').removeprefix('Bearer ')
        expected=self.server.admin if owner else self.server.token
        if not secrets.compare_digest(token,expected): self.reply(401);return False
        return True
    def do_GET(self):
        if self.path in ('/','/admin'):
            if time.monotonic()>=self.server.expiry: self.reply(410,b'Transfer expired');return
            admin=self.path=='/admin'
            title='Wi-Fi 기기 연결' if admin else '상몬과 기록 옮기기'
            content='''<section><h2>1. 같은 Wi-Fi에서 QR을 스캔하세요</h2>{{QR}}<pre>INVITE</pre><p class="hint">초대 주소를 아는 기기가 백업을 받을 수 있어요. 믿을 수 있는 Wi-Fi에서만 열어 주세요. HTTP로 전송하며 인터넷에는 배포하지 않아요.</p></section><section><h2>2. 휴대폰에서 보낸 백업 받기</h2><button id="received">컴퓨터로 받기</button></section>''' if admin else '''<section><h2>컴퓨터의 백업 받기</h2><button id="download">백업 다운로드</button><p>받은 JSON 파일을 MyDay 앱의 설정·백업 → 백업 복원에서 가져오세요.</p></section><section><h2>휴대폰의 백업 보내기</h2><p>MyDay에서 내보낸 공통 백업 JSON을 선택하세요. 컴퓨터에서 확인하고 직접 가져와야 적용돼요.</p><input id="file" type="file" accept=".json,application/json"><button id="upload">선택한 백업 전송</button></section>'''
            # The owner page cannot expose the invitation until its separate credential is verified.
            if admin: content=content.replace('{{QR}}','<div id="qr"></div>').replace('INVITE','<span id="invite"></span>')
            script='''const token=location.hash.slice(1);history.replaceState(null,'',location.pathname);const status=document.getElementById('status');
async function call(path,options={}){const r=await fetch(path,{...options,headers:{'Authorization':'Bearer '+token,...options.headers}});if(!r.ok)throw Error(await r.text()||'연결을 확인해 주세요.');return r;}
async function download(path){try{const r=await call(path);const data=await r.blob();const a=document.createElement('a');a.href=URL.createObjectURL(data);a.download='MyDay-transfer.json';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),30000);status.textContent='다운로드를 요청했어요. 받은 앱에서 백업 복원으로 가져오세요.';}catch(e){status.textContent=e.message;}}
'''
            if admin: script+='''call('/invitation').then(r=>r.json()).then(x=>{document.getElementById('invite').textContent=x.invite;document.getElementById('qr').innerHTML=x.svg;}).catch(e=>status.textContent=e.message);document.getElementById('received').onclick=()=>download('/received');'''
            else: script+='''document.getElementById('download').onclick=()=>download('/download');document.getElementById('upload').onclick=async()=>{const f=document.getElementById('file').files[0];if(!f){status.textContent='백업 파일을 선택하세요.';return;}if(f.size>134217728){status.textContent='128MB 이하로 선택하세요.';return;}if(!confirm('이 백업을 연결한 컴퓨터로 보낼까요?'))return;try{await call('/upload',{method:'POST',headers:{'Content-Type':'application/json'},body:f});status.textContent='보냈어요. 컴퓨터에서 받기 버튼을 눌러 확인하세요.';}catch(e){status.textContent=e.message;}};'''
            page=f'<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title} · MyDay</title><style>{STYLE}</style><main><p>✳ my day</p><h1>{title}</h1><p>10분 동안만 열리는 임시 전송이에요. Ctrl+C로 닫을 수 있어요.</p>{content}<pre id="status" role="status"></pre></main><script>{script}</script></html>'
            self.reply(200,page.encode(),'text/html; charset=utf-8');return
        owner=self.path in ('/invitation','/received')
        if not self.authorized(owner): return
        if self.path=='/invitation':
            data=json.dumps({'invite':self.server.invite,'svg':qr_svg(self.server.invite)}).encode();self.reply(200,data,'application/json');return
        if self.path in ('/download','/received'):
            with self.server.lock:
                data=self.server.received if owner else self.server.data
                if data is None: self.reply(404,'아직 받을 백업이 없어요.'.encode());return
                # Allow retries after interrupted browser downloads, bounded by the session lifetime.
                self.server.downloads+=1
            self.reply(200,data,'application/json',True);return
        self.reply(404)
    def do_POST(self):
        if not self.authorized(): return
        if self.path!='/upload': self.reply(404);return
        if self.headers.get('Content-Type','').split(';')[0]!='application/json' or self.headers.get('Transfer-Encoding') is not None: self.reply(415);return
        try:
            count=int(self.headers.get('Content-Length','-1'))
            if not 0<count<=MAX_BYTES: self.reply(413);return
            data=self.rfile.read(count)
            if len(data)!=count: raise ValueError('전송이 끊겼어요.')
            validate_backup(data)
            with self.server.lock:
                if self.server.received is not None: self.reply(409,'이미 받은 백업이 있어요. 컴퓨터에서 새 전송을 열어 주세요.'.encode());return
                self.server.received=data
            self.reply(200,b'OK')
        except (ValueError,UnicodeError,TimeoutError): self.reply(400,'올바른 MyDay 공통 백업인지 확인해 주세요.'.encode())

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host',required=True,type=private_host);parser.add_argument('--port',type=int,default=8766)
    group=parser.add_mutually_exclusive_group();group.add_argument('--file',type=Path);group.add_argument('--receive',action='store_true')
    parser.add_argument('--no-browser',action='store_true');args=parser.parse_args()
    if args.file is None and not args.receive:
        from tkinter import Tk,filedialog
        root=Tk();root.withdraw();path=filedialog.askopenfilename(title='MyDay 공통 백업 선택',filetypes=[('JSON','*.json')]);root.destroy()
        if not path: return
        args.file=Path(path)
    if args.file and args.file.stat().st_size>MAX_BYTES: raise ValueError('백업은 128MB 이하로 선택하세요.')
    data=validate_backup(args.file.read_bytes()) if args.file else None
    with TransferServer((args.host,args.port),data) as server:
        timer=threading.Timer(LIFETIME,server.shutdown);timer.daemon=True;timer.start()
        print('MyDay 임시 전송을 열었어요. 10분 후 종료됩니다. Ctrl+C로 즉시 닫을 수 있어요.')
        print('관리 화면 (이 컴퓨터에서만 열어 주세요): '+server.owner)
        if data: print('전송할 백업 SHA-256: '+hashlib.sha256(data).hexdigest())
        if not args.no_browser: webbrowser.open(server.owner)
        try: server.serve_forever()
        except KeyboardInterrupt: pass
        finally: timer.cancel();server.data=None;server.received=None
    print('임시 전송을 닫았어요.')
if __name__=='__main__': main()
