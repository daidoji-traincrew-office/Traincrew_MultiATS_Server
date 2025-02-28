-- Modify "switching_machine_state" table
ALTER TABLE "switching_machine_state" ALTER COLUMN "switch_end_time" SET NOT NULL, ALTER COLUMN "switch_end_time" SET DEFAULT CURRENT_TIMESTAMP;
