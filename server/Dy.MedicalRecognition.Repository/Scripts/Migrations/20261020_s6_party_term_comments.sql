-- postgresql schema
-- 阶段 6 互认统计与导出的当事方称谓口径迁移（使用方 / 提供方），由负责人在目标库执行；
-- 与 mrec_recognition_match_record 建表脚本双写维护，两处声明保持逐字一致。
-- 仅重写列注释与索引注释的中文表述，不改列名、类型、约束与索引定义。

comment on column mrec_recognition_match_record.receiver_organization_code is '使用方组织编码';
comment on column mrec_recognition_match_record.receiver_hospital_code is '使用方医院编码';
comment on column mrec_recognition_match_record.receiver_branch_code is '使用方院区编码';

comment on index ix_mrec_recognition_match_record_receiver_time is '使用方组织、使用方医院与使用方院区三值等值筛选并按互认匹配生成时间做范围扫描，与业务键索引互补';
