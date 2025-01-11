-- Add value to enum type: "lock_type"
ALTER TYPE "lock_type" ADD VALUE 'stick';
-- Modify "track_circuit_state" table
ALTER TABLE "track_circuit_state" ALTER COLUMN "is_short_circuit" SET NOT NULL;
-- Rename a column from "target_route_id" to "target_object_id"
ALTER TABLE "lock_state" RENAME COLUMN "target_route_id" TO "target_object_id";
-- Rename a column from "source_route_id" to "source_object_id"
ALTER TABLE "lock_state" RENAME COLUMN "source_route_id" TO "source_object_id";
-- Modify "lock_state" table
ALTER TABLE "lock_state" DROP CONSTRAINT "lock_state_source_route_id_fkey", DROP CONSTRAINT "lock_state_target_route_id_fkey", ADD COLUMN "is_reverse" "nr" NOT NULL, ADD CONSTRAINT "lock_state_source_object_id_fkey" FOREIGN KEY ("source_object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "lock_state_target_object_id_fkey" FOREIGN KEY ("target_object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Rename an index from "lock_state_target_route_id_index" to "lock_state_target_object_id_index"
ALTER INDEX "lock_state_target_route_id_index" RENAME TO "lock_state_target_object_id_index";
