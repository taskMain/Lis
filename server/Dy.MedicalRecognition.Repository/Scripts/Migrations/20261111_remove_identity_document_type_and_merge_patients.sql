-- 目的：按身份证号码归并历史患者，修复可确定的报告患者归属，移除证件类型字段及旧复合索引。
-- 执行前提：已备份三张业务表及两份待删除报告；执行账号具备表结构与数据修改权限；执行前复核下方预期主患者与记录数。
-- 恢复方式：提交前发生错误时事务自动回滚；提交后如需恢复，停止相关写入并从执行前备份恢复三张表及其依赖行，再恢复原索引定义。

begin;

lock table mrec_platform_patient,
           mrec_medical_recognition_report,
           mrec_medical_report_version,
           mrec_recognition_match_item,
           mrec_recognition_match_record
  in access exclusive mode;

do $$
declare
  survivor_id uuid := '3a23c2c4-1535-ce64-cfb4-b3f321c4b2af';
  duplicate_id uuid := '3a245384-6eec-d33e-1b06-fc689f968209';
  duplicate_no text := '530101199005120011';
  actual_survivor_no text;
  actual_survivor_name text;
  actual_duplicate_no text;
  actual_duplicate_name text;
  duplicate_count integer;
  orphan_report_count integer;
  orphan_report_ids uuid[] := array[
    '3a23c2c9-3980-3b35-df89-33ffd7741ff2'::uuid,
    '3a23c2cb-5ffb-2671-dc7d-51f5fab23f97'::uuid
  ];
begin
  select identity_document_no, patient_name
    into actual_survivor_no, actual_survivor_name
    from mrec_platform_patient
   where id = survivor_id;
  select identity_document_no, patient_name
    into actual_duplicate_no, actual_duplicate_name
    from mrec_platform_patient
   where id = duplicate_id;

  if actual_survivor_no is distinct from duplicate_no
     or actual_survivor_name is distinct from '验收患者甲'
     or actual_duplicate_no is distinct from duplicate_no
     or actual_duplicate_name is distinct from 'T02验收患者' then
    raise exception '迁移前置检查失败：历史患者主档与已确认映射不一致。';
  end if;

  select count(*) into duplicate_count
    from mrec_platform_patient
   where identity_document_no = duplicate_no;
  if duplicate_count <> 2 then
    raise exception '迁移前置检查失败：身份证号码 % 当前患者记录数为 %，预期为 2。', duplicate_no, duplicate_count;
  end if;

  if exists (select 1 from critical_value where patient_id = duplicate_id::text) then
    raise exception '迁移前置检查失败：重复患者标识仍被危急值记录引用。';
  end if;

  select count(*) into orphan_report_count
    from mrec_medical_recognition_report r
   where r.patient_id = '00000000-0000-0000-0000-000000000000'
     and not exists (select 1 from mrec_medical_report_version v where v.report_id = r.id);
  if orphan_report_count <> 2
     or (select count(*) from mrec_medical_recognition_report r
          where r.id = any(orphan_report_ids)
            and r.patient_id = '00000000-0000-0000-0000-000000000000'
            and r.current_version_id = '00000000-0000-0000-0000-000000000000'
            and btrim(r.identity_document_no) = '') <> 2
     or exists (select 1 from mrec_medical_report_version v where v.report_id = any(orphan_report_ids))
     or exists (select 1 from mrec_recognition_match_item i where i.report_id = any(orphan_report_ids)) then
    raise exception '迁移前置检查失败：两份无版本全零患者报告与预期记录不一致。';
  end if;

  raise notice '将删除两份无版本、无患者号码且没有互认匹配项的报告主体：%', orphan_report_ids;
end $$;

delete from mrec_medical_recognition_report
 where id in (
   '3a23c2c9-3980-3b35-df89-33ffd7741ff2',
   '3a23c2cb-5ffb-2671-dc7d-51f5fab23f97');

drop index ix_mrec_recognition_match_record_business_key;

update mrec_medical_report_version
   set patient_id = '3a23c2c4-1535-ce64-cfb4-b3f321c4b2af'
 where patient_id = '3a245384-6eec-d33e-1b06-fc689f968209';

update mrec_medical_recognition_report
   set patient_id = '3a23c2c4-1535-ce64-cfb4-b3f321c4b2af'
 where patient_id = '3a245384-6eec-d33e-1b06-fc689f968209';

delete from mrec_platform_patient
 where id = '3a245384-6eec-d33e-1b06-fc689f968209';

update mrec_medical_recognition_report r
   set patient_id = v.patient_id
  from mrec_medical_report_version v
 where r.id = v.report_id
   and r.patient_id = '00000000-0000-0000-0000-000000000000'
   and r.current_version_id = v.id
   and v.patient_id <> '00000000-0000-0000-0000-000000000000';

drop index ux_mrec_platform_patient_document;

alter table mrec_platform_patient
  drop column identity_document_type_code;

alter table mrec_medical_report_version
  drop column identity_document_type_code;

alter table mrec_recognition_match_record
  drop column identity_document_type_code;

create unique index ux_mrec_platform_patient_document
    on mrec_platform_patient (identity_document_no);

comment on index ux_mrec_platform_patient_document is '平台患者身份证号码唯一';

create index ix_mrec_recognition_match_record_business_key
    on mrec_recognition_match_record (
      receiver_organization_code,
      receiver_hospital_code,
      receiver_branch_code,
      identity_document_no,
      match_created_time);

do $$
begin
  if exists (
    select 1 from mrec_platform_patient
     group by identity_document_no
    having count(*) > 1
  ) then
    raise exception '迁移结果检查失败：平台患者身份证号码仍有重复。';
  end if;

  if exists (
    select 1
      from mrec_medical_recognition_report r
      join mrec_medical_report_version v on v.report_id = r.id
     where r.patient_id <> v.patient_id
  ) then
    raise exception '迁移结果检查失败：报告主体与当前版本患者标识不一致。';
  end if;
end $$;

commit;
