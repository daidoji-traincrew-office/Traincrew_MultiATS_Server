-- Modify "track_circuit_state" table
ALTER TABLE "track_circuit_state" ADD COLUMN "locked_by" bigint NULL, ADD CONSTRAINT "track_circuit_state_locked_by_fkey" FOREIGN KEY ("locked_by") REFERENCES "route" ("id");
