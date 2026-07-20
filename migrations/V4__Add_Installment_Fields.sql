-- Add installment columns to expenses table
ALTER TABLE expenses ADD COLUMN installment_number INT NOT NULL DEFAULT 1;
ALTER TABLE expenses ADD COLUMN total_installments INT NOT NULL DEFAULT 1;
ALTER TABLE expenses ADD COLUMN installment_group_id UUID NULL;
