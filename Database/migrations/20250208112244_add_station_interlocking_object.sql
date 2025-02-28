-- Modify "interlocking_object" table
ALTER TABLE "interlocking_object" DROP COLUMN "station_id", ADD CONSTRAINT "interlocking_object_name_key" UNIQUE ("name");
-- Create "station_interlocking_object" table
CREATE TABLE "station_interlocking_object" ("station_id" character varying(10) NOT NULL, "object_id" bigint NOT NULL, CONSTRAINT "station_interlocking_object_station_id_object_id_key" UNIQUE ("station_id", "object_id"), CONSTRAINT "station_interlocking_object_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "station_interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "station_interlocking_object_station_id_index" to table: "station_interlocking_object"
CREATE INDEX "station_interlocking_object_station_id_index" ON "station_interlocking_object" ("station_id");
