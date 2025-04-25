-- Modify "direction_self_control_lever" table
ALTER TABLE "direction_self_control_lever" DROP CONSTRAINT "opening_lever_id_fkey", ADD CONSTRAINT "direction_self_control_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Rename a column from "opening_lever_id" to "direction_self_control_lever_id"
ALTER TABLE "direction_lever" RENAME COLUMN "opening_lever_id" TO "direction_self_control_lever_id";
-- Modify "direction_lever" table
ALTER TABLE "direction_lever" DROP CONSTRAINT "direction_lever_opening_lever_id_fkey", ADD CONSTRAINT "direction_lever_direction_self_control_lever_id_fkey" FOREIGN KEY ("direction_self_control_lever_id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create "direction_self_control_lever_state" table
CREATE TABLE "direction_self_control_lever_state" ("id" bigint NOT NULL, "is_inserted_key" boolean NOT NULL DEFAULT false, "is_reversed" "nr" NOT NULL DEFAULT 'normal', PRIMARY KEY ("id"), CONSTRAINT "direction_self_control_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Drop "opening_lever_state" table
DROP TABLE "opening_lever_state";
