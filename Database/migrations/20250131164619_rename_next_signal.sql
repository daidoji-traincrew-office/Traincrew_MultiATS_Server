-- Create index "track_circuit_state_train_number_index" to table: "track_circuit_state"
CREATE INDEX "track_circuit_state_train_number_index" ON "track_circuit_state" USING hash ("train_number");
-- Drop next_signal table
DROP TABLE "next_signal";
-- Create "next_signal" table
CREATE TABLE "next_signal" ("id" bigserial NOT NULL, "signal_name" character varying(100) NOT NULL, "source_signal_name" character varying(100) NOT NULL, "target_signal_name" character varying(100) NOT NULL, "depth" integer NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "next_signal_signal_name_target_signal_name_key" UNIQUE ("signal_name", "target_signal_name"), CONSTRAINT "next_signal_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "next_signal_source_signal_name_fkey" FOREIGN KEY ("source_signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "next_signal_target_signal_name_fkey" FOREIGN KEY ("target_signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "next_signal_signal_name_index" to table: "next_signal"
CREATE INDEX "next_signal_signal_name_index" ON "next_signal" ("signal_name");