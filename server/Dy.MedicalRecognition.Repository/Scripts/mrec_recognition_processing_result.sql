-- postgresql schema
create table mrec_recognition_processing_result (
  id uuid not null,
  recognition_match_record_id uuid not null,
  recognition_match_item_id uuid not null,
  recognition_time timestamp without time zone not null,
  recognition_result integer not null,
  recognition_dept_id text not null,
  recognition_dept_name text not null,
  recognition_doctor_id text not null,
  recognition_doctor_name text not null,
  non_adoption_reason integer,
  non_adoption_description text,
  estimated_saving_amount numeric(18,2),
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_recognition_processing_result is '互认处理结果';
comment on column mrec_recognition_processing_result.id is '主键ID';
comment on column mrec_recognition_processing_result.recognition_match_record_id is '互认匹配记录ID';
comment on column mrec_recognition_processing_result.recognition_match_item_id is '互认匹配项ID';
comment on column mrec_recognition_processing_result.recognition_time is '互认时间';
comment on column mrec_recognition_processing_result.recognition_result is '互认结果';
comment on column mrec_recognition_processing_result.recognition_dept_id is '互认科室ID';
comment on column mrec_recognition_processing_result.recognition_dept_name is '互认科室名称';
comment on column mrec_recognition_processing_result.recognition_doctor_id is '互认医生ID';
comment on column mrec_recognition_processing_result.recognition_doctor_name is '互认医生名称';
comment on column mrec_recognition_processing_result.non_adoption_reason is '不采纳原因';
comment on column mrec_recognition_processing_result.non_adoption_description is '不采纳补充说明';
comment on column mrec_recognition_processing_result.estimated_saving_amount is '预计节省金额';
comment on column mrec_recognition_processing_result.oper_time is '操作时间';
comment on column mrec_recognition_processing_result.oper_id is '操作人';

create unique index ux_mrec_recognition_processing_result_match_item
on mrec_recognition_processing_result (recognition_match_item_id);

comment on index ux_mrec_recognition_processing_result_match_item is '同一互认匹配项至多一条处理结果';

create index ix_mrec_recognition_processing_result_recognition_time
on mrec_recognition_processing_result (recognition_time);

comment on index ix_mrec_recognition_processing_result_recognition_time is '按互认时间统计采纳次数、不采纳次数与预计节省金额的时间范围扫描';
