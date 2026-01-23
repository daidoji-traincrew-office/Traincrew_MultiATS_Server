-- Modify "track_circuit" table
ALTER TABLE "track_circuit" ADD COLUMN "is_station" boolean NOT NULL DEFAULT FALSE, ADD COLUMN "up_station_id" character varying(10) NULL, ADD COLUMN "down_station_id" character varying(10) NULL, ADD CONSTRAINT "track_circuit_down_station_id_fkey" FOREIGN KEY ("down_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "track_circuit_up_station_id_fkey" FOREIGN KEY ("up_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
ALTER TABLE "track_circuit" ALTER COLUMN "is_station" DROP DEFAULT;
