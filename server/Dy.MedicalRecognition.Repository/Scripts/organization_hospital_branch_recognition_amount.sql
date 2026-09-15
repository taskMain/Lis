-- postgresql schema
create table if not exists organization_hospital_branch_recognition_amount (
  id uuid not null,
  organization_code varchar(100) not null,
  hospital_code varchar(100) not null,
  branch_code varchar(100) not null,
  standard_project_code varchar(100) not null,
  current_amount numeric(18,2) not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
