-- Create "protection_zone_state" table
CREATE TABLE "protection_zone_state" ("id" bigserial NOT NULL, "protection_zone" bigint NOT NULL, "train_number" character varying(100) NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "protection_zone_state_protection_zone_train_number_key" UNIQUE ("protection_zone", "train_number"));
