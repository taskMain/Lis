-- postgresql schema
create table if not exists mutual_recognition_item (
  id uuid not null,
  organization_code varchar(100) not null,
  standard_item_id uuid not null,
  standard_project_code varchar(100) not null,
  recognition_duration_days integer not null,
  is_valid boolean not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
