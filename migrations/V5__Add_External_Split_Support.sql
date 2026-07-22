-- Make user_id nullable in expense_splits to support external guests
ALTER TABLE expense_splits ALTER COLUMN user_id DROP NOT NULL;

-- Add guest_name column to store external person names
ALTER TABLE expense_splits ADD COLUMN guest_name VARCHAR(100) NULL;
