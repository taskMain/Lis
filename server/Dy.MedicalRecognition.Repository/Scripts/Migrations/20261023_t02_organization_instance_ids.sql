-- postgresql schema
-- T02 迁移（Base → IAM）的组织实例标识列迁移，由负责人在目标库执行；
-- 与 mrec_medical_recognition_report、mrec_recognition_match_record、mrec_organization_hospital_branch_recognition_amount、
-- mutual_recognition_item 四份建表脚本双写维护，两处声明保持逐字一致。
-- 新列可空：迁移期与旧码列并存，旧码列继续承载既有读取，契约收缩时再删除。
-- 注意：文件 mutual_recognition_item.sql 的物理表名是 mrec_mutual_recognition_item，语句以物理表名为准。

alter table mrec_medical_recognition_report add column if not exists organization_id uuid;
comment on column mrec_medical_recognition_report.organization_id is '组织实例ID';
alter table mrec_medical_recognition_report add column if not exists hospital_id uuid;
comment on column mrec_medical_recognition_report.hospital_id is '医院实例ID';
alter table mrec_medical_recognition_report add column if not exists branch_id uuid;
comment on column mrec_medical_recognition_report.branch_id is '院区实例ID';

alter table mrec_recognition_match_record add column if not exists receiver_organization_id uuid;
comment on column mrec_recognition_match_record.receiver_organization_id is '使用方组织实例ID';
alter table mrec_recognition_match_record add column if not exists receiver_hospital_id uuid;
comment on column mrec_recognition_match_record.receiver_hospital_id is '使用方医院实例ID';
alter table mrec_recognition_match_record add column if not exists receiver_branch_id uuid;
comment on column mrec_recognition_match_record.receiver_branch_id is '使用方院区实例ID';

alter table mrec_organization_hospital_branch_recognition_amount add column if not exists organization_id uuid;
comment on column mrec_organization_hospital_branch_recognition_amount.organization_id is '组织实例ID';
alter table mrec_organization_hospital_branch_recognition_amount add column if not exists hospital_id uuid;
comment on column mrec_organization_hospital_branch_recognition_amount.hospital_id is '医院实例ID';
alter table mrec_organization_hospital_branch_recognition_amount add column if not exists branch_id uuid;
comment on column mrec_organization_hospital_branch_recognition_amount.branch_id is '院区实例ID';

alter table mrec_mutual_recognition_item add column if not exists organization_id uuid;
comment on column mrec_mutual_recognition_item.organization_id is '组织实例ID';
