-- postgresql schema
create table mrec_recognition_match_record (
  id uuid not null,
  receiver_organization_code text not null,
  receiver_hospital_code text not null,
  receiver_branch_code text not null,
  identity_document_type_code text not null,
  identity_document_no text not null,
  visit_type integer not null,
  visit_serial_no text not null,
  match_created_time timestamp without time zone not null,
  decision_saved_time timestamp without time zone,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_recognition_match_record is '互认匹配记录';
comment on column mrec_recognition_match_record.id is '主键ID';
comment on column mrec_recognition_match_record.receiver_organization_code is '接收组织编码';
comment on column mrec_recognition_match_record.receiver_hospital_code is '接收医院编码';
comment on column mrec_recognition_match_record.receiver_branch_code is '接收院区编码';
comment on column mrec_recognition_match_record.identity_document_type_code is '证件类型代码';
comment on column mrec_recognition_match_record.identity_document_no is '证件号码';
comment on column mrec_recognition_match_record.visit_type is '就诊类型';
comment on column mrec_recognition_match_record.visit_serial_no is '就诊流水号';
comment on column mrec_recognition_match_record.match_created_time is '互认匹配生成时间';
comment on column mrec_recognition_match_record.decision_saved_time is '处理结果保存时间';
comment on column mrec_recognition_match_record.oper_time is '操作时间';
comment on column mrec_recognition_match_record.oper_id is '操作人';

create index ix_mrec_recognition_match_record_business_key
on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, identity_document_type_code, identity_document_no, match_created_time);

comment on index ix_mrec_recognition_match_record_business_key is '主要接收三值与患者证件筛选并按互认匹配生成时间排序';

create index ix_mrec_recognition_match_record_receiver_time
on mrec_recognition_match_record (receiver_organization_code, receiver_hospital_code, receiver_branch_code, match_created_time);

comment on index ix_mrec_recognition_match_record_receiver_time is '接收组织、接收医院与接收院区三值等值筛选并按互认匹配生成时间做范围扫描，与业务键索引互补';
