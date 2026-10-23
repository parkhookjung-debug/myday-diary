import importlib.util
import json
from pathlib import Path
import sys
import threading
import unittest
import urllib.request
import urllib.error
import time

sys.path.insert(0,str(Path(__file__).parent))
spec=importlib.util.spec_from_file_location('lan',Path(__file__).with_name('lan-transfer.py'));lan=importlib.util.module_from_spec(spec);spec.loader.exec_module(lan)
BACKUP=json.dumps({'Version':8,'Days':[]}).encode()
class TransferTests(unittest.TestCase):
    def setUp(self):
        self.server=lan.TransferServer(('127.0.0.1',0),BACKUP);self.thread=threading.Thread(target=self.server.serve_forever,daemon=True);self.thread.start()
    def tearDown(self): self.server.shutdown();self.server.server_close();self.thread.join()
    def request(self,path,token=None,data=None,headers=None):
        request=urllib.request.Request(self.server.origin+path,data=data,headers={'Authorization':'Bearer '+(token or self.server.token),**(headers or {})})
        try:
            with urllib.request.urlopen(request,timeout=3) as response:return response.status,response.read(),response.headers
        except urllib.error.HTTPError as e:return e.code,e.read(),e.headers
    def test_backup_transfer_and_separate_owner(self):
        status,data,headers=self.request('/download');self.assertEqual((status,data),(200,BACKUP));self.assertEqual(headers['Cache-Control'],'no-store')
        self.assertEqual(self.request('/invitation')[0],401)
        status,data,_=self.request('/invitation',self.server.admin);self.assertEqual(status,200);self.assertIn('<svg',json.loads(data)['svg']);self.assertEqual(json.loads(data)['invite'],self.server.invite)
        self.assertEqual(self.request('/upload',data=BACKUP,headers={'Content-Type':'application/json','Origin':self.server.origin})[0],200)
        self.assertEqual(self.request('/received')[0],401)
        self.assertEqual(self.request('/received',self.server.admin)[1],BACKUP)
        self.assertEqual(self.request('/upload',data=BACKUP,headers={'Content-Type':'application/json'})[0],409)
    def test_access_expiration_and_origin(self):
        self.assertEqual(self.request('/download','wrong')[0],401)
        self.assertEqual(self.request('/download',headers={'Origin':'https://other.example'})[0],403)
        self.assertEqual(self.request('/download',headers={'Host':'attacker.example'})[0],403)
        self.assertEqual(self.request('/upload',data=b'bad',headers={'Content-Type':'application/json'})[0],400)
        self.assertEqual(self.request('/upload',data=BACKUP,headers={'Content-Type':'text/plain'})[0],415)
        self.server.expiry=time.monotonic()-1;self.assertEqual(self.request('/download')[0],410)
    def test_page_does_not_disclose_tokens(self):
        _,page,_=self.request('/admin');self.assertNotIn(self.server.token.encode(),page);self.assertNotIn(self.server.admin.encode(),page)
        self.assertIn(b'location.hash',page)
    def test_private_only(self):
        for host in ['192.168.0.10','10.0.2.2','172.16.0.10','127.0.0.1']: self.assertEqual(lan.private_host(host),host)
        for host in ['0.0.0.0','8.8.8.8','example.com','169.254.1.2','::1']:
            with self.assertRaises(ValueError):lan.private_host(host)
    def test_qr_and_format(self):
        self.assertIn('viewBox=',lan.qr_svg(self.server.invite))
        for data in [b'{}',b'{"Version":9,"Days":[]}',b'{"Version":true,"Days":[]}',b'[]']:
            with self.assertRaises(ValueError):lan.validate_backup(data)

if __name__=='__main__': unittest.main()
