-- Modify "track_circuit" table
ALTER TABLE "track_circuit" DROP COLUMN "up_station_id", DROP COLUMN "down_station_id", ADD COLUMN "station_id_for_delay" character varying(10) NULL, ADD CONSTRAINT "track_circuit_station_id_for_delay_fkey" FOREIGN KEY ("station_id_for_delay") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
