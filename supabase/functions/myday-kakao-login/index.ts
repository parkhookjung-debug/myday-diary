import { createClient } from 'npm:@supabase/supabase-js@2.118.0';
import { loginWithKakao, LoginError } from '../_shared/kakao-auth.mjs';
import { boundedBody } from '../_shared/body.mjs';

Deno.serve(async(req: Request)=>{
  const headers={'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'};
  const modern=(name:string)=>{try{return Object.values(JSON.parse(Deno.env.get(name)||'{}')).find(v=>typeof v==='string') as string|undefined;}catch{return undefined;}};
  const adminKey=modern('SUPABASE_SECRET_KEYS')||Deno.env.get('SUPABASE_SERVICE_ROLE_KEY');
  const publicKey=modern('SUPABASE_PUBLISHABLE_KEYS')||Deno.env.get('SUPABASE_ANON_KEY');
  if(req.method==='GET')return new Response(JSON.stringify({ok:true,service:'myday-friends',ready:!!(adminKey && publicKey && /^[0-9]+$/.test(Deno.env.get('MYDAY_KAKAO_APP_ID')??'') && (Deno.env.get('MYDAY_AUTH_PEPPER')?.length??0)>=32)}),{status:200,headers});
  if(req.method!=='POST')return new Response(JSON.stringify({error:'POST required'}),{status:405,headers});
  try {
    if(!(req.headers.get('content-type')??'').startsWith('application/json'))throw new LoginError(400);
    let input;
    try{input=JSON.parse(await boundedBody(req,8192));}catch{throw new LoginError(400);}
    const url=Deno.env.get('SUPABASE_URL')!;
    const admin=createClient(url,adminKey!,{auth:{persistSession:false,autoRefreshToken:false}});
    const publicAuth=createClient(url,publicKey!,{auth:{persistSession:false,autoRefreshToken:false}});
    const session=await loginWithKakao(input.kakaoAccessToken,{admin,publicAuth,appId:Deno.env.get('MYDAY_KAKAO_APP_ID'),pepper:Deno.env.get('MYDAY_AUTH_PEPPER')});
    return new Response(JSON.stringify(session),{status:200,headers});
  }catch(error){
    const status=error instanceof LoginError?error.status:503;
    return new Response(JSON.stringify({error:'카카오 로그인을 확인하지 못했어요. 앱에서 다시 시도해 주세요.'}),{status,headers});
  }
});
