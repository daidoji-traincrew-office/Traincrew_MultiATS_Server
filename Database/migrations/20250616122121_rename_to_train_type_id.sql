-- Rename a column from "type_id" to "train_type_id"
ALTER TABLE "train_diagram" RENAME COLUMN "type_id" TO "train_type_id";
-- Modify "train_diagram" table
ALTER TABLE "train_diagram" DROP CONSTRAINT "train_diagram_type_id_fkey", ADD CONSTRAINT "train_diagram_train_type_id_fkey" FOREIGN KEY ("train_type_id") REFERENCES "train_type" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
