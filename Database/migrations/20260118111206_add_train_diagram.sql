-- Delete "train_diagram" table
DROP TABLE "train_diagram";
-- Create "train_diagram" table
CREATE TABLE "train_diagram" ("id" bigserial NOT NULL, "train_number" character varying(100) NOT NULL, "train_type_id" bigint NOT NULL, "from_station_id" character varying(10) NOT NULL, "to_station_id" character varying(10) NOT NULL, "dia_id" integer NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "train_diagram_from_station_id_fkey" FOREIGN KEY ("from_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "train_diagram_to_station_id_fkey" FOREIGN KEY ("to_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "train_diagram_train_type_id_fkey" FOREIGN KEY ("train_type_id") REFERENCES "train_type" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "idx_train_diagram_dia_id_train_number" to table: "train_diagram"
CREATE UNIQUE INDEX "idx_train_diagram_dia_id_train_number" ON "train_diagram" ("dia_id", "train_number");
-- Create "train_diagram_timetable" table
CREATE TABLE "train_diagram_timetable" ("id" bigserial NOT NULL, "train_diagram_id" bigint NOT NULL, "index" integer NOT NULL, "station_id" character varying(10) NOT NULL, "track_number" character varying(50) NOT NULL, "arrival_time" interval NULL, "departure_time" interval NULL, PRIMARY KEY ("id"), CONSTRAINT "train_diagram_timetable_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "train_diagram_timetable_train_diagram_id_fkey" FOREIGN KEY ("train_diagram_id") REFERENCES "train_diagram" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "idx_train_diagram_timetable_train_diagram_id_index" to table: "train_diagram_timetable"
CREATE UNIQUE INDEX "idx_train_diagram_timetable_train_diagram_id_index" ON "train_diagram_timetable" ("train_diagram_id", "index");
