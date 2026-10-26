-- postgresql schema
-- T02 迁移（Base → IAM）组织实例标识列的回收脚本。由负责人执行；本阶段只交付脚本文件，不执行、不连库。
--
-- 回收对象：20261023_t02_organization_instance_ids.sql 在四张业务表上新增的 10 个 uuid 实例标识列。
--   mrec_medical_recognition_report                     organization_id / hospital_id / branch_id
--   mrec_recognition_match_record                       receiver_organization_id / receiver_hospital_id / receiver_branch_id
--   mrec_organization_hospital_branch_recognition_amount organization_id / hospital_id / branch_id
--   mrec_mutual_recognition_item                        organization_id
-- 组织范围在业务表上以 IAM 组织业务编码（organization_code / hospital_code / branch_code 及匹配记录的
-- receiver_* 编码列）表达：写入、查询过滤、分组投影与统计排序都只使用编码列，唯一键与范围索引本就落在编码列，
-- 因此删除上述实例标识列不改变任何业务键、索引或约束。
--
-- 执行前置条件（全部成立才可执行）：
--   1. 已部署的后端版本不再读写这 10 个列：建表脚本与全部 SqlMap 中已无对应列，实体、命令、查询类型与读模型上
--      也无同名属性，应用层不再向业务表写入实例标识。
--   2. 目标库已完成本回收脚本之前的所有迁移（含 20261023_t02_organization_instance_ids.sql），
--      编码列取值为空的行在删除实例列后不参与按组织检索：回滚脚本不改写历史行，也不要求按 IAM 目录回填；
--      确需让这些行重新可见时另行按业务口径补录，不在本脚本范围内。
--   3. 已确认没有其他系统或离线作业直接读取这 10 个列。
--   4. 已备份目标库或至少备份上述四张表。
--
--
-- 说明：文件 mutual_recognition_item.sql 的物理表名是 mrec_mutual_recognition_item，语句以物理表名为准。


-- 本文件只含 down（按 20261023 原文重建列）。仅在需要回滚时执行。

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
