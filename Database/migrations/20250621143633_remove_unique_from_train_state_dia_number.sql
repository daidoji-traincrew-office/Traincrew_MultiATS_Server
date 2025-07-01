-- Modify "train_state" table
ALTER TABLE "train_state" DROP CONSTRAINT "train_state_dia_number_key";
-- Create index "train_state_dia_number_index" to table: "train_state"
CREATE INDEX "train_state_dia_number_index" ON "train_state" USING hash ("dia_number");
