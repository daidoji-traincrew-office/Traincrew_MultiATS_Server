-- Modify "route" table
ALTER TABLE "route" ADD COLUMN "approach_lock_final_track_circuit_id" bigint NULL, ADD CONSTRAINT "route_approach_lock_final_track_circuit_id_fkey" FOREIGN KEY ("approach_lock_final_track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
