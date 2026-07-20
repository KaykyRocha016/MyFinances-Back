-- Rename tables
ALTER TABLE nucleos RENAME TO households;
ALTER TABLE usuarios RENAME TO users;
ALTER TABLE ciclos RENAME TO cycles;
ALTER TABLE categorias RENAME TO categories;
ALTER TABLE despesas RENAME TO expenses;
ALTER TABLE despesas_rateio RENAME TO expense_splits;

-- Rename columns in households
ALTER TABLE households RENAME COLUMN nome TO name;

-- Rename columns in users
ALTER TABLE users RENAME COLUMN nome TO name;
ALTER TABLE users RENAME COLUMN renda TO income;
ALTER TABLE users RENAME COLUMN nucleo_id TO household_id;

-- Rename columns in cycles
ALTER TABLE cycles RENAME COLUMN nome TO name;
ALTER TABLE cycles RENAME COLUMN data_inicio TO start_date;
ALTER TABLE cycles RENAME COLUMN data_fim TO end_date;
ALTER TABLE cycles RENAME COLUMN ativo TO is_active;
ALTER TABLE cycles RENAME COLUMN nucleo_id TO household_id;

-- Rename columns in categories
ALTER TABLE categories RENAME COLUMN nome TO name;
ALTER TABLE categories RENAME COLUMN tipo_divisao TO division_type;

-- Rename columns in expenses
ALTER TABLE expenses RENAME COLUMN descricao TO description;
ALTER TABLE expenses RENAME COLUMN valor TO amount;
ALTER TABLE expenses RENAME COLUMN data TO date;
ALTER TABLE expenses RENAME COLUMN usuario_id TO user_id;
ALTER TABLE expenses RENAME COLUMN categoria_id TO category_id;
ALTER TABLE expenses RENAME COLUMN ciclo_id TO cycle_id;

-- Rename columns in expense_splits
ALTER TABLE expense_splits RENAME COLUMN despesa_id TO expense_id;
ALTER TABLE expense_splits RENAME COLUMN usuario_id TO user_id;
ALTER TABLE expense_splits RENAME COLUMN valor TO amount;
