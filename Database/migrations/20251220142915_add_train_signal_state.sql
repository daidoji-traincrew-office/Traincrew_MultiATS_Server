-- Create "train_signal_state" table
CREATE TABLE "train_signal_state" ("id" bigserial NOT NULL, "train_number" character varying(100) NOT NULL, "signal_name" character varying(100) NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "train_signal_state_train_number_signal_name_key" UNIQUE ("train_number", "signal_name"), CONSTRAINT "train_signal_state_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "train_signal_state_signal_name_index" to table: "train_signal_state"
CREATE INDEX "train_signal_state_signal_name_index" ON "train_signal_state" ("signal_name");
-- Create index "train_signal_state_train_number_index" to table: "train_signal_state"
CREATE INDEX "train_signal_state_train_number_index" ON "train_signal_state" ("train_number");
