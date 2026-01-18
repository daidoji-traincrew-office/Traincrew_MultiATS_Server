-- Modify "train_diagram" table
ALTER TABLE "train_diagram" DROP CONSTRAINT "train_diagram_pkey", ADD COLUMN "id" bigserial NOT NULL, ADD PRIMARY KEY ("id");
-- Create index "idx_train_diagram_dia_id_train_number" to table: "train_diagram"
CREATE UNIQUE INDEX "idx_train_diagram_dia_id_train_number" ON "train_diagram" ("dia_id", "train_number");
-- Modify "train_diagram_timetable" table
ALTER TABLE "train_diagram_timetable" ALTER COLUMN "id" TYPE bigint, DROP COLUMN "train_number", ADD COLUMN "train_diagram_id" bigint NOT NULL, ADD CONSTRAINT "train_diagram_timetable_train_diagram_id_fkey" FOREIGN KEY ("train_diagram_id") REFERENCES "train_diagram" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "idx_train_diagram_timetable_train_diagram_id_index" to table: "train_diagram_timetable"
CREATE UNIQUE INDEX "idx_train_diagram_timetable_train_diagram_id_index" ON "train_diagram_timetable" ("train_diagram_id", "index");
