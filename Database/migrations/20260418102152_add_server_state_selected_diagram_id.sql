-- Modify "server_state" table
ALTER TABLE "server_state" ADD COLUMN "selected_diagram_id" bigint NULL, ADD CONSTRAINT "server_state_selected_diagram_id_fkey" FOREIGN KEY ("selected_diagram_id") REFERENCES "diagram" ("id") ON UPDATE NO ACTION ON DELETE SET NULL;
