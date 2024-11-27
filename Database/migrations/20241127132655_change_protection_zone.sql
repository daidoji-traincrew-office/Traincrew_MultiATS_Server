-- Modify "track_circuit" table
ALTER TABLE "track_circuit" DROP COLUMN "protection_zone";
ALTER TABLE "track_circuit" ADD COLUMN "protection_zone" integer NOT NULL;
