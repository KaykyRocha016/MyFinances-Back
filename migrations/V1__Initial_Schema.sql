CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    renda DECIMAL(18,2) NOT NULL
);

CREATE TABLE categorias (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    tipo_divisao VARCHAR(50) NOT NULL
);

CREATE TABLE despesas (
    id SERIAL PRIMARY KEY,
    descricao VARCHAR(255) NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    data TIMESTAMP WITH TIME ZONE NOT NULL,
    usuario_id INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    categoria_id INT NOT NULL REFERENCES categorias(id) ON DELETE CASCADE
);

CREATE TABLE despesas_rateio (
    id SERIAL PRIMARY KEY,
    despesa_id INT NOT NULL REFERENCES despesas(id) ON DELETE CASCADE,
    usuario_id INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    valor DECIMAL(18,2) NOT NULL
);

-- Seed data to make testing easier
INSERT INTO categorias (nome, tipo_divisao) VALUES
('Contas Fixas', 'PROPORCIONAL'),
('Mercado e Compras para Casa', 'PROPORCIONAL'),
('Compras Individuais', 'INDIVIDUAL'),
('Saídas/Adicionais', 'CUSTOMIZADO');
