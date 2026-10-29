begin;
create or replace function myday_private.uid() returns uuid language plpgsql stable security definer set search_path='' as $$
declare u uuid := auth.uid();
begin
  if u is null or not exists(select 1 from auth.users where id=u and raw_app_meta_data->>'myday_provider'='kakao'
    and raw_app_meta_data->>'myday_kakao_app_id'='1626827' and raw_app_meta_data->>'myday_connected'='true') then
    raise exception '카카오 계정으로 로그인해 주세요.' using errcode='28000';
  end if;
  return u;
end $$;
create or replace function public.myday_can_read_photo(p_path text) returns boolean language plpgsql stable security definer set search_path='' as $$
declare u uuid := myday_private.uid();
begin
  return split_part(p_path,'/',1)=u::text or exists(select 1 from myday_private.photos p where p.path=p_path and myday_private.are_friends(u,p.owner));
end $$;
create function public.myday_find_kakao_user(p_id text) returns jsonb language sql stable security definer set search_path='' as $$
  select jsonb_build_object('id',id,'email',email,'verifiedAt',raw_app_meta_data->'myday_kakao_verified_at','connected',raw_app_meta_data->'myday_connected') from auth.users
    where raw_app_meta_data->>'myday_provider'='kakao' and raw_app_meta_data->>'myday_kakao_id'=p_id and raw_app_meta_data->>'myday_kakao_app_id'='1626827'
    limit 1;
$$;
create function public.myday_purge_disabled_user(p_user uuid,p_event_at bigint) returns boolean language plpgsql security definer set search_path='' as $$
begin
  if exists(select 1 from auth.users where id=p_user and raw_app_meta_data->>'myday_provider'='kakao'
    and raw_app_meta_data->>'myday_connected'='false' and coalesce((raw_app_meta_data->>'myday_kakao_verified_at')::bigint,0)<=p_event_at+1) then
    delete from myday_private.profiles where id=p_user;
    return true;
  end if;
  return false;
end $$;
revoke all on function public.myday_find_kakao_user(text),public.myday_purge_disabled_user(uuid,bigint) from public,anon,authenticated;
grant execute on function public.myday_find_kakao_user(text),public.myday_purge_disabled_user(uuid,bigint) to service_role;
revoke all on function myday_private.uid() from public,anon,authenticated;
commit;
