-- Modify "interlocking_object" table
ALTER TABLE "interlocking_object" ADD COLUMN "station_id" character varying(10) NULL, ADD CONSTRAINT "interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
