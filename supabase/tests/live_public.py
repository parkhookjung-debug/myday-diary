"""Check a configured deployment without creating users or uploading diaries."""
from pathlib import Path
import json
from urllib.request import Request,urlopen
from urllib.error import HTTPError

root=Path(__file__).resolve().parents[2]
settings=dict(line.split('=',1) for line in (root/'local.properties').read_text(encoding='utf-8').splitlines() if '=' in line)
url=settings['MYDAY_SUPABASE_URL'].rstrip('/')
key=settings['MYDAY_SUPABASE_KEY']
assert key.startswith('sb_publishable_')
results=[]
def check(name,route,expected,body=None,content_type='application/json',bearer=None):
    headers={'apikey':key,'Content-Type':content_type}
    if bearer:headers['Authorization']='Bearer '+bearer
    data=None if body is None else (json.dumps(body).encode() if isinstance(body,dict) else body)
    request=Request(url+route,data=data,headers=headers,method='GET' if body is None else 'POST')
    try:response=urlopen(request,timeout=30)
    except HTTPError as error:response=error
    with response:
        status=response.status;raw=response.read(4096)
    assert status in expected,(name,status)
    results.append({'check':name,'status':status})
    return json.loads(raw) if raw else None
health=check('health','/functions/v1/myday-kakao-login',{200})
assert health['ready'] is True
check('invalid Kakao token is rejected','/functions/v1/myday-kakao-login',{401},{'kakaoAccessToken':'not-a-valid-kakao-token'})
check('malformed login is rejected','/functions/v1/myday-kakao-login',{400},b'{')
check('forged account event is rejected','/functions/v1/myday-kakao-events',{400},b'invalid.event.token','application/secevent+jwt')
check('anonymous diary API is denied','/rest/v1/rpc/myday_me',{401,403},{})
check('anonymous admin lookup is denied','/rest/v1/rpc/myday_find_kakao_user',{401,403},{'p_id':'101'})
check('forged Supabase session is denied','/rest/v1/rpc/myday_friends',{401,403},{},bearer='not-a-jwt')
print(json.dumps({'service':url,'checks':results,'result':'passed'},ensure_ascii=False,indent=2))
