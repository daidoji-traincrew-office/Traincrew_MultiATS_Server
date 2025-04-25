-- Rename "opening_lever" to "direction_self_control_lever"
ALTER TABLE "opening_lever" RENAME TO "direction_self_control_lever";
-- Rename "opening_lever_state" to "direction_self_control_lever_state"
ALTER TABLE "opening_lever_state" RENAME TO "direction_self_control_lever_state";
-- Rename a column from "opening_lever_id" to "direction_self_control_lever_id"
ALTER TABLE "direction_lever" RENAME COLUMN "opening_lever_id" TO "direction_self_control_lever_id";
-- Modify "direction_self_control_lever" table
ALTER TABLE "direction_self_control_lever" DROP CONSTRAINT "opening_lever_id_fkey", ADD CONSTRAINT "direction_self_control_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "direction_lever" table
ALTER TABLE "direction_lever" DROP CONSTRAINT "direction_lever_opening_lever_id_fkey", ADD CONSTRAINT "direction_lever_direction_self_control_lever_id_fkey" FOREIGN KEY ("direction_self_control_lever_id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "direction_self_control_lever_state" table
ALTER TABLE "direction_self_control_lever_state" DROP CONSTRAINT "opening_lever_state_id_fkey", ADD CONSTRAINT "direction_self_control_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Add value to enum type: "object_type"
ALTER TYPE "object_type" ADD VALUE 'direction_lever';
-- Add value to enum type: "object_type"
ALTER TYPE "object_type" ADD VALUE 'direction_self_control_lever';
