-- postgresql schema
create table mrec_organization_hospital_branch_recognition_amount (
  id uuid not null,
  organization_code text not null,
  hospital_code text not null,
  branch_code text not null,
  standard_project_code text not null,
  current_amount numeric(18,2) not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_organization_hospital_branch_recognition_amount is '组织医院院区互认项目金额';
comment on column mrec_organization_hospital_branch_recognition_amount.id is '主键ID';
comment on column mrec_organization_hospital_branch_recognition_amount.organization_code is '组织编码';
comment on column mrec_organization_hospital_branch_recognition_amount.hospital_code is '医院编码';
comment on column mrec_organization_hospital_branch_recognition_amount.branch_code is '院区编码';
comment on column mrec_organization_hospital_branch_recognition_amount.standard_project_code is '标准项目编码';
comment on column mrec_organization_hospital_branch_recognition_amount.current_amount is '当前金额';
comment on column mrec_organization_hospital_branch_recognition_amount.oper_time is '操作时间';
comment on column mrec_organization_hospital_branch_recognition_amount.oper_id is '操作人';

create unique index ux_mrec_org_hos_brh_project
on mrec_organization_hospital_branch_recognition_amount (organization_code, hospital_code, branch_code, standard_project_code);

comment on index ux_mrec_org_hos_brh_project is '同一组织医院院区标准项目金额唯一';
