-- Modify "lever" table
ALTER TABLE "lever" ADD COLUMN "switching_machine_id" bigint NULL, ADD CONSTRAINT "lever_switching_machine_id_fkey" FOREIGN KEY ("switching_machine_id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
