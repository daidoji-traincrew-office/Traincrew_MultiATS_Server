-- Modify "track_circuit_state" table
ALTER TABLE "track_circuit_state" ADD COLUMN "is_correction_raise_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', ADD COLUMN "raised_at" timestamp NULL, ADD COLUMN "is_correction_drop_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', ADD COLUMN "dropped_at" timestamp NULL;
