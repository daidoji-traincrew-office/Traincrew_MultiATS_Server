-- Modify "station" table
ALTER TABLE "station" ADD COLUMN "is_station" boolean NOT NULL DEFAULT true, ADD COLUMN "is_passenger_station" boolean NOT NULL DEFAULT true;
ALTER TABLE "station" ALTER COLUMN "is_station" DROP DEFAULT ,ALTER COLUMN "is_passenger_station" DROP DEFAULT;
