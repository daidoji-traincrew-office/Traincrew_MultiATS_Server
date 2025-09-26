-- Modify "throw_out_control" table
ALTER TABLE "throw_out_control" ADD CONSTRAINT "throw_out_control_source_id_target_id_key" UNIQUE ("source_id", "target_id");
