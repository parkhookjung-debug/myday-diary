-- Free-plan backend. Clients get only authenticated RPCs and private JPEGs.
begin;
create schema if not exists myday_private;
revoke all on schema myday_private from public, anon, authenticated;
create extension if not exists pgcrypto with schema extensions;

create table myday_private.profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  nickname text not null check (length(nickname) between 1 and 40)
);
create table myday_private.friendships (
  low_id uuid references myday_private.profiles(id) on delete cascade,
  high_id uuid references myday_private.profiles(id) on delete cascade,
  primary key(low_id, high_id), check(low_id < high_id)
);
create table myday_private.invites (
  owner uuid primary key references myday_private.profiles(id) on delete cascade,
  code_hash bytea unique not null, expires_at timestamptz not null
);
create table myday_private.requests (
  id uuid primary key default gen_random_uuid(),
  sender uuid not null references myday_private.profiles(id) on delete cascade,
  recipient uuid not null references myday_private.profiles(id) on delete cascade,
  created_at timestamptz not null default now(), unique(sender, recipient), check(sender <> recipient)
);
create index on myday_private.requests(recipient);
create table myday_private.posts (
  owner uuid references myday_private.profiles(id) on delete cascade,
  date date not null, payload jsonb not null, updated_at timestamptz not null default now(),
  primary key(owner, date)
);
create table myday_private.photos (
  owner uuid not null, date date not null, path text not null unique,
  foreign key(owner,date) references myday_private.posts(owner,date) on delete cascade,
  primary key(owner,date,path)
);
create table myday_private.attempts (
  owner uuid references auth.users(id) on delete cascade,
  action text not null, at timestamptz not null default now()
);
create index on myday_private.attempts(owner,action,at);
alter table myday_private.profiles enable row level security;
alter table myday_private.friendships enable row level security;
alter table myday_private.invites enable row level security;
alter table myday_private.requests enable row level security;
alter table myday_private.posts enable row level security;
alter table myday_private.photos enable row level security;
alter table myday_private.attempts enable row level security;
revoke all on all tables in schema myday_private from public,anon,authenticated;

create function myday_private.uid() returns uuid language plpgsql stable security definer set search_path='' as $$
declare u uuid := auth.uid();
begin
  if u is null or not exists(select 1 from auth.users where id=u and raw_app_meta_data->>'provider'='kakao') then
    raise exception '카카오 계정으로 로그인해 주세요.' using errcode='28000';
  end if;
  return u;
end $$;
create function myday_private.are_friends(a uuid,b uuid) returns boolean language sql stable set search_path='' as $$
  select exists(select 1 from myday_private.friendships where low_id=least(a,b) and high_id=greatest(a,b));
$$;
create function myday_private.rate(u uuid, a text, max_count integer) returns void language plpgsql set search_path='' as $$
begin
  perform pg_advisory_xact_lock(hashtextextended(u::text,0));
  delete from myday_private.attempts where owner=u and at < now()-interval '1 hour';
  if (select count(*) from myday_private.attempts where owner=u and action=a) >= max_count then
    raise exception '요청이 많아요. 잠시 후 다시 시도해 주세요.';
  end if;
  insert into myday_private.attempts(owner,action) values(u,a);
end $$;

create function public.myday_me() returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); n text;
begin
  select left(coalesce(nullif(raw_user_meta_data->>'name',''),nullif(raw_user_meta_data->>'nickname',''),'마이데이 친구'),40)
    into n from auth.users where id=u;
  insert into myday_private.profiles values(u,n) on conflict(id) do update set nickname=excluded.nickname;
  return jsonb_build_object('id',u,'nickname',n);
end $$;
create function public.myday_create_invite() returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); code text := upper(encode(extensions.gen_random_bytes(8),'hex')); expiry timestamptz := now()+interval '24 hours';
begin
  perform public.myday_me(); perform myday_private.rate(u,'invite',20);
  insert into myday_private.invites values(u,extensions.digest(code,'sha256'),expiry)
    on conflict(owner) do update set code_hash=excluded.code_hash,expires_at=excluded.expires_at;
  return jsonb_build_object('code',code,'expiresAt',expiry);
end $$;
create function public.myday_redeem_invite(p_code text) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); target uuid; code text; request_id uuid;
begin
  perform public.myday_me(); perform myday_private.rate(u,'redeem',10);
  code := upper(regexp_replace(p_code,'[\s-]','','g'));
  if length(code) <> 16 or code !~ '^[A-F0-9]+$' then return jsonb_build_object('error','초대코드를 확인해 주세요.'); end if;
  select owner into target from myday_private.invites where code_hash=extensions.digest(code,'sha256') and expires_at>now() for update;
  if target is null then return jsonb_build_object('error','초대코드가 만료되었거나 이미 사용됐어요.'); end if;
  if target=u then return jsonb_build_object('error','내 초대코드는 다른 친구에게 보내 주세요.'); end if;
  if myday_private.are_friends(u,target) then return jsonb_build_object('error','이미 친구로 연결되어 있어요.'); end if;
  if exists(select 1 from myday_private.requests where (sender=u and recipient=target) or (sender=target and recipient=u)) then
    return jsonb_build_object('error','이미 친구 요청이 있어요. 받은 요청을 확인해 주세요.');
  end if;
  insert into myday_private.requests(sender,recipient) values(u,target) returning id into request_id;
  delete from myday_private.invites where owner=target;
  return jsonb_build_object('requestId',request_id,'nickname',(select nickname from myday_private.profiles where id=target));
end $$;
create function public.myday_requests() returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); result jsonb;
begin
  select coalesce(jsonb_agg(jsonb_build_object('id',r.id,'nickname',p.nickname,'incoming',r.recipient=u) order by r.created_at desc),'[]') into result
    from myday_private.requests r join myday_private.profiles p on p.id=case when r.recipient=u then r.sender else r.recipient end
    where r.recipient=u or r.sender=u;
  return jsonb_build_object('requests',result);
end $$;
create function public.myday_answer_request(p_id uuid,p_accept boolean) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); r myday_private.requests;
begin
  select * into r from myday_private.requests where id=p_id and recipient=u for update;
  if not found then raise exception '받은 친구 요청을 확인해 주세요.'; end if;
  if p_accept then insert into myday_private.friendships values(least(u,r.sender),greatest(u,r.sender)) on conflict do nothing; end if;
  delete from myday_private.requests where id=r.id;
  return jsonb_build_object('ok',true);
end $$;
create function public.myday_remove_friend(p_id uuid) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid();
begin
  delete from myday_private.friendships where low_id=least(u,p_id) and high_id=greatest(u,p_id);
  delete from myday_private.requests where (sender=u and recipient=p_id) or (sender=p_id and recipient=u);
  return jsonb_build_object('ok',true);
end $$;
create function public.myday_friends() returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); result jsonb;
begin
  select coalesce(jsonb_agg(jsonb_build_object('id',p.id,'nickname',p.nickname) order by p.nickname),'[]') into result
    from myday_private.friendships f join myday_private.profiles p on p.id=case when f.low_id=u then f.high_id else f.low_id end
    where u in(f.low_id,f.high_id);
  return jsonb_build_object('friends',result);
end $$;
create function myday_private.summary(p myday_private.posts) returns jsonb language sql stable set search_path='' as $$
  select jsonb_build_object('ownerId',p.owner,'nickname',n.nickname,'date',p.date,'updatedAt',p.updated_at,
    'title',p.payload->>'title','mood',p.payload->>'mood','blockCount',jsonb_array_length(p.payload->'blocks'),
    'photoCount',(select count(*) from myday_private.photos where owner=p.owner and date=p.date))
    from myday_private.profiles n where n.id=p.owner;
$$;
create function public.myday_posts(p_mine boolean default false,p_owner uuid default null) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); result jsonb;
begin
  select coalesce(jsonb_agg(myday_private.summary(row(q.owner,q.date,q.payload,q.updated_at)::myday_private.posts) order by q.date desc),'[]') into result from
    (select p.* from myday_private.posts p where
      (p_mine and p.owner=u) or (not p_mine and (p_owner is null or p.owner=p_owner) and myday_private.are_friends(u,p.owner))
      order by p.date desc limit 366) q;
  return jsonb_build_object('entries',result);
end $$;
create function public.myday_detail(p_owner uuid,p_date date) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); p myday_private.posts;
begin
  if p_owner<>u and not myday_private.are_friends(u,p_owner) then raise exception '이 일기는 볼 수 없어요.'; end if;
  select * into p from myday_private.posts where owner=p_owner and date=p_date;
  if not found then raise exception '공개된 일기가 없어요.'; end if;
  return myday_private.summary(p)||p.payload;
end $$;
create function public.myday_publish(p_date date,p_payload jsonb) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); b jsonb; photo_path text; photo_count integer := 0; block_ids text[] := '{}';
begin
  perform public.myday_me(); perform myday_private.rate(u,'publish',60);
  if jsonb_typeof(p_payload) is distinct from 'object' or octet_length(p_payload::text)>400000 or
    (p_payload - array['title','mood','blocks'])<>'{}' or jsonb_typeof(p_payload->'title') is distinct from 'string' or
    jsonb_typeof(p_payload->'mood') is distinct from 'string' or length(p_payload->>'title')>80 or length(p_payload->>'mood')>40 then
    raise exception '공개할 제목과 기록을 확인해 주세요.';
  end if;
  if jsonb_typeof(p_payload->'blocks') is distinct from 'array' then raise exception '공개할 기록을 선택해 주세요.'; end if;
  if jsonb_array_length(p_payload->'blocks') not between 1 and 30 then raise exception '기록을 1~30개 골라 주세요.'; end if;
  for b in select value from jsonb_array_elements(p_payload->'blocks') loop
    if jsonb_typeof(b)<>'object' or (b-array['id','type','title','text','checked','photoPath'])<>'{}' or
      jsonb_typeof(b->'id') is distinct from 'string' or length(b->>'id') not between 1 and 128 or (b->>'id')=any(block_ids) or
      (b->>'type') not in('text','todo','habit','emotion','photo') or jsonb_typeof(b->'type') is distinct from 'string' or
      jsonb_typeof(b->'title') is distinct from 'string' or length(b->>'title')>80 or
      jsonb_typeof(b->'text') is distinct from 'string' or length(b->>'text')>10000 or
      jsonb_typeof(b->'checked') is distinct from 'boolean' then raise exception '공개할 기록 형식을 확인해 주세요.';
    end if;
    block_ids := array_append(block_ids,b->>'id');
    if b ? 'photoPath' then
      photo_path := b->>'photoPath'; photo_count := photo_count+1;
      if b->>'type'<>'photo' or jsonb_typeof(b->'photoPath') is distinct from 'string' or
        photo_path !~ ('^'||u::text||'/[a-f0-9-]{36}/[a-f0-9-]{36}\.jpg$') or
        not exists(select 1 from storage.objects where bucket_id='diary-photos' and name=photo_path) then
        raise exception '사진을 다시 선택해 주세요.';
      end if;
    end if;
  end loop;
  if photo_count>6 then raise exception '사진은 한 번에 6장까지 공개할 수 있어요.'; end if;
  if (select count(*) from myday_private.posts where owner=u and date<>p_date)>=366 then raise exception '공개 보관함이 가득 찼어요. 이전 공개 글을 정리해 주세요.'; end if;
  insert into myday_private.posts values(u,p_date,p_payload,now()) on conflict(owner,date) do update set payload=excluded.payload,updated_at=excluded.updated_at;
  delete from myday_private.photos where owner=u and date=p_date;
  insert into myday_private.photos select u,p_date,value->>'photoPath' from jsonb_array_elements(p_payload->'blocks') where value ? 'photoPath';
  return jsonb_build_object('ok',true);
end $$;
create function public.myday_unpublish(p_date date) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid();
begin
  delete from myday_private.posts where owner=u and date=p_date;
  return jsonb_build_object('ok',true);
end $$;
create function public.myday_own_photo_paths(p_date date default null) returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid(); result jsonb;
begin
  select coalesce(jsonb_agg(path),'[]') into result from myday_private.photos where owner=u and (p_date is null or date=p_date);
  return jsonb_build_object('paths',result);
end $$;
create function public.myday_clear_shared() returns jsonb language plpgsql security definer set search_path='' as $$
declare u uuid := myday_private.uid();
begin
  delete from myday_private.profiles where id=u;
  return jsonb_build_object('ok',true);
end $$;

create function public.myday_can_read_photo(p_path text) returns boolean language sql stable security definer set search_path='' as $$
  select auth.uid() is not null and (split_part(p_path,'/',1)=auth.uid()::text or exists(
    select 1 from myday_private.photos p where p.path=p_path and myday_private.are_friends(auth.uid(),p.owner)));
$$;
insert into storage.buckets(id,name,public,file_size_limit,allowed_mime_types)
  values('diary-photos','diary-photos',false,2097152,array['image/jpeg']);
create policy myday_photo_read on storage.objects for select to authenticated using(bucket_id='diary-photos' and public.myday_can_read_photo(name));
create policy myday_photo_insert on storage.objects for insert to authenticated with check(
  bucket_id='diary-photos' and (select myday_private.uid())::text=split_part(name,'/',1)
  and name ~ '^[a-f0-9-]{36}/[a-f0-9-]{36}/[a-f0-9-]{36}\.jpg$'
);
create policy myday_photo_delete on storage.objects for delete to authenticated using(bucket_id='diary-photos' and auth.uid()::text=split_part(name,'/',1));

-- Private helper functions are never callable by application clients.
revoke all on all functions in schema myday_private from public,anon,authenticated;
-- uid is needed only by the storage INSERT policy: expose a narrowly scoped wrapper.
create function public.myday_is_owner(p_path text) returns boolean language plpgsql stable security definer set search_path='' as $$
begin return myday_private.uid()::text=split_part(p_path,'/',1); end $$;
alter policy myday_photo_insert on storage.objects with check(bucket_id='diary-photos' and public.myday_is_owner(name)
  and name ~ '^[a-f0-9-]{36}/[a-f0-9-]{36}/[a-f0-9-]{36}\.jpg$');
do $$ declare r record; begin
  for r in select p.oid::regprocedure as signature from pg_proc p join pg_namespace n on n.oid=p.pronamespace
    where n.nspname='public' and p.proname like 'myday\_%' escape '\' loop
    execute format('revoke all on function %s from public,anon,authenticated',r.signature);
    execute format('grant execute on function %s to authenticated',r.signature);
  end loop;
end $$;
commit;
