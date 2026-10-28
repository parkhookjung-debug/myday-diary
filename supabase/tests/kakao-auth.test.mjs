import assert from 'node:assert/strict';
import { loginWithKakao,verifyKakao,LoginError } from '../functions/_shared/kakao-auth.mjs';
const appId='1626827'; let checks=0;
function ok(value,label){assert(value,label);checks++;}
function kakao({app=appId,expires=3000,id=101,meId=id,status=200}={}) {
  return async(url,options)=>{
    assert(url.startsWith('https://kapi.kakao.com/'));assert.equal(options.redirect,'error');
    return new Response(JSON.stringify(url.endsWith('access_token_info')?{app_id:Number(app),expires_in:expires,id}:{id:meId,kakao_account:{profile:{nickname:'산책 친구'}}}),{status});
  };
}
function backend({mismatch=false}={}) {
  let account=null, email='',adminCalls=0,verified=0;
  const admin={rpc:async()=>{adminCalls++;return{data:account,error:null}},auth:{admin:{
    createUser:async(input)=>{adminCalls++;email=input.email;account={id:'10000000-0000-0000-0000-000000000001',email,app_metadata:input.app_metadata};return{data:{user:account}}},
    updateUserById:async(id,input)=>{adminCalls++;account.app_metadata=input.app_metadata;return{data:{user:account}}},
    generateLink:async()=>{adminCalls++;return{data:{user:account,properties:{hashed_token:'server-only-one-time-hash'}}}}
  }}};
  const publicAuth={auth:{verifyOtp:async(input)=>{verified++;assert.equal(input.token_hash,'server-only-one-time-hash');return{data:{session:{access_token:'supabase-access',refresh_token:'supabase-refresh',expires_in:3600,token_type:'bearer',user:{id:mismatch?'other-user':account.id}}}}}}};
  return{admin,publicAuth,appId,pepper:'server-only-random-pepper-with-at-least-32-bytes',get calls(){return adminCalls},get verified(){return verified},get email(){return email}};
}
for(const settings of [{app:'9999'},{expires:0},{meId:202},{id:9007199254740992},{status:401}]) {
  const fixture=backend();
  await assert.rejects(loginWithKakao('kakao-token',{...fixture,fetcher:kakao(settings)}),LoginError);
  ok(fixture.calls===0,'Unverified Kakao identities never reach administrative auth');
}
await assert.rejects(verifyKakao('token with whitespace',appId,kakao()),LoginError);checks++;
const one=backend();const session=await loginWithKakao('kakao-token',{...one,fetcher:kakao()});
ok(session.user.id==='10000000-0000-0000-0000-000000000001','Session belongs to verified account');
ok(one.verified===1,'Server consumes a single-use token without sending email');
ok(one.email.endsWith('@myday.invalid') && !one.email.includes('101'),'Alias never exposes Kakao ID or a real email');
ok(one.email.split('@')[0].length<=64,'Internal alias follows the email local-part length limit');
ok(!JSON.stringify(session).includes('server-only'),'Administrative material never reaches the app');
const two=backend();await loginWithKakao('another-token',{...two,fetcher:kakao({id:202})});
ok(one.email!==two.email,'Different verified Kakao identities stay separate');
await assert.rejects(loginWithKakao('kakao-token',{...backend({mismatch:true}),fetcher:kakao()}),LoginError);checks++;
console.log(`${checks} Kakao identity and session-issuance checks passed.`);
