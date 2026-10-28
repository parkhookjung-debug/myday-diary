import { createClient,type SupabaseClient } from 'npm:@supabase/supabase-js@2.118.0';
import { createRemoteJWKSet,jwtVerify } from 'npm:jose@6.2.13';
import { boundedBody } from '../_shared/body.mjs';
declare const EdgeRuntime:{waitUntil(task:Promise<unknown>):void};

const issuer='https://kauth.kakao.com';
const unlinked='https://schemas.openid.net/secevent/oauth/event-type/user-unlinked';
const keys=createRemoteJWKSet(new URL(issuer+'/.well-known/jwks.json'),{timeoutDuration:1000,cooldownDuration:60000,cacheMaxAge:21600000});
const options={auth:{persistSession:false,autoRefreshToken:false}};
async function removeFilesAndAccount(admin: SupabaseClient,id: string,eventAt: number) {
  const bucket=admin.storage.from('diary-photos');
  async function stillDisabled(){
    const account=await admin.auth.admin.getUserById(id);
    return !account.error && account.data.user?.app_metadata.myday_connected===false && Number(account.data.user.app_metadata.myday_kakao_verified_at??0)<=eventAt+1;
  }
  async function clean(prefix: string) {
    if(!await stillDisabled())throw new Error('Account was reconnected.');
    // Always read the first page again after deleting files or subdirectories.
    const listed=await bucket.list(prefix,{limit:1000});if(listed.error)throw listed.error;
    const files:string[]=[];
    for(const item of listed.data) {
      const path=prefix+'/'+item.name;
      if(!path.startsWith(id+'/'))throw new Error('invalid owner');
      if(item.id)files.push(path);else await clean(path);
    }
    if(files.length){const deleted=await bucket.remove(files);if(deleted.error)throw deleted.error;}
    if(listed.data.length===1000)await clean(prefix);
  }
  await clean(id);
  if(!await stillDisabled())return;
  const deleted=await admin.auth.admin.deleteUser(id);if(deleted.error)throw deleted.error;
}
Deno.serve(async(req:Request)=>{
  const headers={'Content-Type':'application/json','Cache-Control':'no-store'};
  try {
    if(req.method!=='POST' || !(req.headers.get('content-type')??'').startsWith('application/secevent+jwt'))throw new Error('invalid_request');
    const appId=Deno.env.get('MYDAY_KAKAO_APP_ID'),restKey=Deno.env.get('MYDAY_KAKAO_REST_KEY');
    if(!/^[0-9]+$/.test(appId??'') || !/^[a-fA-F0-9]{32}$/.test(restKey??''))throw new Error('invalid_audience');
    const raw=await boundedBody(req,65536);
    const {payload,protectedHeader}=await jwtVerify(raw,keys,{algorithms:['RS256'],issuer,audience:restKey,requiredClaims:['sub','app_id','iat','toe','jti','events'],clockTolerance:60});
    if(protectedHeader.typ!=='secevent+jwt' || String(payload.app_id)!==Deno.env.get('MYDAY_KAKAO_APP_ID'))throw new Error('invalid_audience');
    const occurred=payload.toe;
    if(!Number.isSafeInteger(occurred) || (occurred as number)<=0 || (occurred as number)>Date.now()/1000+60)throw new Error('invalid_request');
    const event=(payload.events as Record<string,{subject?:{subject_type?:string,iss?:string,sub?:string}}>)?.[unlinked];
    if(!event)return new Response(null,{status:202,headers});
    if(!/^[0-9]{1,32}$/.test(payload.sub??'') || event.subject?.sub!==payload.sub || event.subject?.subject_type!=='iss-sub' || event.subject?.iss!==issuer)throw new Error('invalid_request');
    const modern=Object.values(JSON.parse(Deno.env.get('SUPABASE_SECRET_KEYS')||'{}')).find(v=>typeof v==='string') as string|undefined;
    const admin=createClient(Deno.env.get('SUPABASE_URL')!,modern||Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!,options);
    const found=await admin.rpc('myday_find_kakao_user',{p_id:payload.sub});if(found.error)throw found.error;
    if(found.data && Number(found.data.verifiedAt??0)<=(occurred as number)+1) {
      const id=found.data.id;
      const disabled=await admin.auth.admin.updateUserById(id,{app_metadata:{myday_connected:false}});if(disabled.error)throw disabled.error;
      const purged=await admin.rpc('myday_purge_disabled_user',{p_user:id,p_event_at:occurred});if(purged.error)throw purged.error;
      if(purged.data)EdgeRuntime.waitUntil(removeFilesAndAccount(admin,id,occurred as number).catch(()=>console.error('Account cleanup must be retried.')));
    }
    return new Response(null,{status:202,headers});
  }catch{return new Response(JSON.stringify({err:'invalid_request',description:'Kakao account event could not be verified.'}),{status:400,headers});}
});
