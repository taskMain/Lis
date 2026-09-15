-- postgresql schema
create table if not exists examination_site (
  id uuid not null,
  examination_item_id uuid not null,
  source_site_code varchar(100),
  site_name varchar(100) not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
