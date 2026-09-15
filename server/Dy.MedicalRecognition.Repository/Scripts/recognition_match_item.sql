-- postgresql schema
create table if not exists recognition_match_item (
  id uuid not null,
  recognition_match_record_id uuid not null,
  item_type integer not null,
  standard_project_code varchar(100) not null,
  report_id uuid not null,
  report_version_id uuid not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
