-- Create "diagram" table
CREATE TABLE "diagram" (
  "id" bigserial NOT NULL,
  "name" text NOT NULL,
  "time_range" text NOT NULL,
  "version" text NOT NULL,
  PRIMARY KEY ("id")
);

-- Rename train_diagram_timetable first (has FK to train_diagram, must come before renaming the referenced table)
ALTER TABLE "train_diagram_timetable" RENAME TO "diagram_train_timetable";

-- Rename train_diagram to diagram_train
ALTER TABLE "train_diagram" RENAME TO "diagram_train";

-- Rename indexes
ALTER INDEX "idx_train_diagram_dia_id_train_number" RENAME TO "idx_diagram_train_dia_id_train_number";
ALTER INDEX "idx_train_diagram_timetable_train_diagram_id_index" RENAME TO "idx_diagram_train_timetable_train_diagram_id_index";

-- Change dia_id column type from INT to BIGINT
ALTER TABLE "diagram_train" ALTER COLUMN "dia_id" TYPE BIGINT;

-- Add FK from diagram_train.dia_id to diagram.id
ALTER TABLE "diagram_train" ADD CONSTRAINT "diagram_train_dia_id_fkey" FOREIGN KEY ("dia_id") REFERENCES "diagram" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;

-- Rename primary key constraints
ALTER TABLE "diagram_train" RENAME CONSTRAINT "train_diagram_pkey" TO "diagram_train_pkey";
ALTER TABLE "diagram_train_timetable" RENAME CONSTRAINT "train_diagram_timetable_pkey" TO "diagram_train_timetable_pkey";

-- Rename FK constraints on diagram_train
ALTER TABLE "diagram_train" DROP CONSTRAINT "train_diagram_from_station_id_fkey", DROP CONSTRAINT "train_diagram_to_station_id_fkey", DROP CONSTRAINT "train_diagram_train_type_id_fkey", ADD CONSTRAINT "diagram_train_from_station_id_fkey" FOREIGN KEY ("from_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "diagram_train_to_station_id_fkey" FOREIGN KEY ("to_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "diagram_train_train_type_id_fkey" FOREIGN KEY ("train_type_id") REFERENCES "train_type" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;

-- Rename FK constraints on diagram_train_timetable
ALTER TABLE "diagram_train_timetable" DROP CONSTRAINT "train_diagram_timetable_station_id_fkey", DROP CONSTRAINT "train_diagram_timetable_train_diagram_id_fkey", ADD CONSTRAINT "diagram_train_timetable_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "diagram_train_timetable_train_diagram_id_fkey" FOREIGN KEY ("train_diagram_id") REFERENCES "diagram_train" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
