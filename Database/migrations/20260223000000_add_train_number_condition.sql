-- Create enum type "both_odd_even"
CREATE TYPE both_odd_even AS ENUM ('both', 'odd', 'even');
-- Modify "lock_condition_object" table
ALTER TABLE "lock_condition_object" ADD COLUMN "train_number_condition" both_odd_even NOT NULL DEFAULT 'both';
