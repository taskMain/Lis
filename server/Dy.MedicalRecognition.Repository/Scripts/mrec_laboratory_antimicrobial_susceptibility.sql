-- postgresql schema
create table mrec_laboratory_antimicrobial_susceptibility (
  id uuid not null,
  bacteria_result_id uuid not null,
  source_detail_key text,
  drug_code text,
  drug_name text not null,
  susceptibility_code text,
  source_conclusion_text text not null,
  resistance_result_code text,
  disk_content text,
  mic_value text,
  inhibition_zone_diameter text,
  reference_value text,
  display_order integer not null,
  inspector_id text,
  inspector_name text,
  testing_method text,
  test_panel_order text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_laboratory_antimicrobial_susceptibility is '药敏结果';
comment on column mrec_laboratory_antimicrobial_susceptibility.id is '主键ID';
comment on column mrec_laboratory_antimicrobial_susceptibility.bacteria_result_id is '细菌鉴定结果ID';
comment on column mrec_laboratory_antimicrobial_susceptibility.source_detail_key is '来源明细标识';
comment on column mrec_laboratory_antimicrobial_susceptibility.drug_code is '受试药物编码';
comment on column mrec_laboratory_antimicrobial_susceptibility.drug_name is '受试药物名称';
comment on column mrec_laboratory_antimicrobial_susceptibility.susceptibility_code is '药敏结论编码';
comment on column mrec_laboratory_antimicrobial_susceptibility.source_conclusion_text is '来源结论';
comment on column mrec_laboratory_antimicrobial_susceptibility.resistance_result_code is '抗药结果编码';
comment on column mrec_laboratory_antimicrobial_susceptibility.disk_content is '纸片含药量';
comment on column mrec_laboratory_antimicrobial_susceptibility.mic_value is 'MIC';
comment on column mrec_laboratory_antimicrobial_susceptibility.inhibition_zone_diameter is '抑菌环直径';
comment on column mrec_laboratory_antimicrobial_susceptibility.reference_value is '参考值';
comment on column mrec_laboratory_antimicrobial_susceptibility.display_order is '展示序号';
comment on column mrec_laboratory_antimicrobial_susceptibility.inspector_id is '检测人ID';
comment on column mrec_laboratory_antimicrobial_susceptibility.inspector_name is '检测人名称';
comment on column mrec_laboratory_antimicrobial_susceptibility.testing_method is '检测方法';
comment on column mrec_laboratory_antimicrobial_susceptibility.test_panel_order is '试验板序号';
comment on column mrec_laboratory_antimicrobial_susceptibility.oper_time is '操作时间';
comment on column mrec_laboratory_antimicrobial_susceptibility.oper_id is '操作人';

create index ix_mrec_laboratory_antimicrobial_susceptibility_bacteria
on mrec_laboratory_antimicrobial_susceptibility (bacteria_result_id);

comment on index ix_mrec_laboratory_antimicrobial_susceptibility_bacteria is '按细菌鉴定结果读取药敏结果';
