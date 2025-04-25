-- Drop index "throw_out_control_source_route_id_index" from table: "throw_out_control"
DROP INDEX "throw_out_control_source_route_id_index";
-- Drop index "throw_out_control_target_route_id_index" from table: "throw_out_control"
DROP INDEX "throw_out_control_target_route_id_index";
-- Rename a column from "source_route_id" to "source_id"
ALTER TABLE "throw_out_control" RENAME COLUMN "source_route_id" TO "source_id";
-- Rename a column from "target_route_id" to "target_id"
ALTER TABLE "throw_out_control" RENAME COLUMN "target_route_id" TO "target_id";
-- Modify "throw_out_control" table
ALTER TABLE "throw_out_control" DROP CONSTRAINT "throw_out_control_source_route_id_fkey", DROP CONSTRAINT "throw_out_control_target_route_id_fkey", ADD COLUMN "source_lr" "lr" NULL, ADD COLUMN "target_lr" "lr" NULL, ADD COLUMN "condition_nr" "nr" NULL, ADD CONSTRAINT "throw_out_control_source_id_fkey" FOREIGN KEY ("source_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "throw_out_control_target_id_fkey" FOREIGN KEY ("target_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "throw_out_control_source_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_source_id_index" ON "throw_out_control" ("source_id");
-- Create index "throw_out_control_target_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_target_id_index" ON "throw_out_control" ("target_id");
