export class LoginError extends Error {
  constructor(status=401) { super('카카오 로그인을 확인하지 못했어요. 앱에서 다시 로그인해 주세요.'); this.status=status; }
}
function identity(value) {
  if(typeof value==='number' && !Number.isSafeInteger(value))throw new LoginError();
  const id=String(value??''); if(!/^[0-9]{1,32}$/.test(id))throw new LoginError(); return id;
}
export async function verifyKakao(token,appId,fetcher=fetch) {
  if(typeof token!=='string' || !token || token.length>4096 || /\s/.test(token) || !/^[0-9]+$/.test(appId??''))throw new LoginError();
  async function get(path) {
    let response;
    try { response=await fetcher('https://kapi.kakao.com'+path,{headers:{Authorization:'Bearer '+token},redirect:'error',signal:AbortSignal.timeout(5000)}); }
    catch { throw new LoginError(502); }
    if(!response.ok)throw new LoginError(response.status===401?401:502);
    const raw=await response.text(); if(raw.length>65536)throw new LoginError(502);
    try { return JSON.parse(raw); } catch { throw new LoginError(502); }
  }
  const info=await get('/v1/user/access_token_info');
  if(String(info.app_id)!==appId || typeof info.expires_in!=='number' || info.expires_in<=0)throw new LoginError();
  const id=identity(info.id); const user=await get('/v2/user/me');
  if(identity(user.id)!==id)throw new LoginError();
  const nickname=String(user.kakao_account?.profile?.nickname || user.properties?.nickname || '마이데이 친구').replace(/[\u0000-\u001f\u007f]/g,'').slice(0,40) || '마이데이 친구';
  return {id,nickname};
}
export async function loginWithKakao(token,{admin,publicAuth,appId,pepper,fetcher=fetch}) {
  // Nothing administrative happens until Kakao has verified the app and identity.
  const person=await verifyKakao(token,appId,fetcher);
  if(typeof pepper!=='string' || pepper.length<32)throw new LoginError(503);
  const key=await crypto.subtle.importKey('raw',new TextEncoder().encode(pepper),{name:'HMAC',hash:'SHA-256'},false,['sign']);
  const hash=new Uint8Array(await crypto.subtle.sign('HMAC',key,new TextEncoder().encode(appId+':'+person.id)));
  const alias='k-'+Array.from(hash,b=>b.toString(16).padStart(2,'0')).join('').slice(0,60)+'@myday.invalid';
  const lookup=await admin.rpc('myday_find_kakao_user',{p_id:person.id});
  if(lookup.error)throw new LoginError(503);
  const metadata={myday_provider:'kakao',myday_kakao_id:person.id,myday_kakao_app_id:appId,myday_connected:true,myday_kakao_verified_at:Math.floor(Date.now()/1000)};
  let account=lookup.data;
  if(account?.connected===false)throw new LoginError(409);
  if(!account) {
    const created=await admin.auth.admin.createUser({email:alias,email_confirm:true,app_metadata:metadata,user_metadata:{name:person.nickname}});
    if(created.error) {
      // Handle only a concurrent creation of the same verified Kakao account.
      const retry=await admin.rpc('myday_find_kakao_user',{p_id:person.id});
      if(retry.error || !retry.data)throw new LoginError(503);
      account=retry.data;
    } else account=created.data.user;
  }
  if(!account?.id || !account.email)throw new LoginError(503);
  const updated=await admin.auth.admin.updateUserById(account.id,{app_metadata:metadata,user_metadata:{name:person.nickname}});
  if(updated.error)throw new LoginError(503);
  const link=await admin.auth.admin.generateLink({type:'magiclink',email:account.email});
  if(link.error || link.data?.user?.id!==account.id || !link.data?.properties?.hashed_token)throw new LoginError(503);
  // No email is sent. The server consumes the one-time hash after Kakao login.
  const result=await publicAuth.auth.verifyOtp({type:'email',token_hash:link.data.properties.hashed_token});
  if(result.error || result.data?.session?.user?.id!==account.id)throw new LoginError(503);
  const {access_token,refresh_token,expires_in,token_type,user}=result.data.session;
  return {access_token,refresh_token,expires_in,token_type,user};
}
