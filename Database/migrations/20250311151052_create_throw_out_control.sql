-- Create "throw_out_control" table
CREATE TABLE "throw_out_control" ("id" bigserial NOT NULL, "source_route_id" bigint NOT NULL, "target_route_id" bigint NOT NULL, "condition_lever_id" bigint NULL, PRIMARY KEY ("id"), CONSTRAINT "throw_out_control_condition_lever_id_fkey" FOREIGN KEY ("condition_lever_id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "throw_out_control_source_route_id_fkey" FOREIGN KEY ("source_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "throw_out_control_target_route_id_fkey" FOREIGN KEY ("target_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "throw_out_control_source_route_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_source_route_id_index" ON "throw_out_control" ("source_route_id");
-- Create index "throw_out_control_target_route_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_target_route_id_index" ON "throw_out_control" ("target_route_id");
-- Drop "total_control" table
DROP TABLE "total_control";
