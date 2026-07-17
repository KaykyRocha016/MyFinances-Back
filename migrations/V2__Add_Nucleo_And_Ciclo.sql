-- Create nucleos table
CREATE TABLE nucleos (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL
);

-- Insert default nucleo for existing data
INSERT INTO nucleos (nome) VALUES ('Casal Principal');

-- Update usuarios table to add nucleo_id
ALTER TABLE usuarios ADD COLUMN nucleo_id INT;

-- Update existing users to point to the default nucleo
UPDATE usuarios SET nucleo_id = (SELECT id FROM nucleos LIMIT 1);

-- Make nucleo_id NOT NULL and add foreign key
ALTER TABLE usuarios ALTER COLUMN nucleo_id SET NOT NULL;
ALTER TABLE usuarios ADD FOREIGN KEY (nucleo_id) REFERENCES nucleos(id) ON DELETE CASCADE;

-- Create ciclos table
CREATE TABLE ciclos (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    data_inicio TIMESTAMP WITH TIME ZONE NOT NULL,
    data_fim TIMESTAMP WITH TIME ZONE NOT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    nucleo_id INT NOT NULL REFERENCES nucleos(id) ON DELETE CASCADE
);

-- Insert a default active cycle for existing data so we don't break existing records
INSERT INTO ciclos (nome, data_inicio, data_fim, ativo, nucleo_id)
VALUES ('Ciclo Inicial', '2026-01-01 00:00:00+00', '2026-12-31 23:59:59+00', TRUE, (SELECT id FROM nucleos LIMIT 1));

-- Update despesas table to add ciclo_id
ALTER TABLE despesas ADD COLUMN ciclo_id INT;

-- Update existing expenses to point to the default cycle
UPDATE despesas SET ciclo_id = (SELECT id FROM ciclos LIMIT 1);

-- Make ciclo_id NOT NULL and add foreign key
ALTER TABLE despesas ALTER COLUMN ciclo_id SET NOT NULL;
ALTER TABLE despesas ADD FOREIGN KEY (ciclo_id) REFERENCES ciclos(id) ON DELETE CASCADE;
