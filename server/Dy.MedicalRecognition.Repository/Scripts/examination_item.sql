-- postgresql schema
create table if not exists examination_item (
  id uuid not null,
  report_version_id uuid not null,
  source_project_name varchar(100) not null,
  source_project_code varchar(100),
  standard_project_code varchar(100),
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
