-- Rename a constraint from "OpenIddictApplications_pkey" to "PK_OpenIddictApplications"
ALTER TABLE "OpenIddictApplications" RENAME CONSTRAINT "OpenIddictApplications_pkey" TO "PK_OpenIddictApplications";
-- Rename a constraint from "OpenIddictAuthorizations_pkey" to "PK_OpenIddictAuthorizations"
ALTER TABLE "OpenIddictAuthorizations" RENAME CONSTRAINT "OpenIddictAuthorizations_pkey" TO "PK_OpenIddictAuthorizations";
-- Rename a constraint from "OpenIddictScopes_pkey" to "PK_OpenIddictScopes"
ALTER TABLE "OpenIddictScopes" RENAME CONSTRAINT "OpenIddictScopes_pkey" TO "PK_OpenIddictScopes";
-- Rename a constraint from "OpenIddictTokens_pkey" to "PK_OpenIddictTokens"
ALTER TABLE "OpenIddictTokens" RENAME CONSTRAINT "OpenIddictTokens_pkey" TO "PK_OpenIddictTokens";
-- Rename a constraint from "direction_lever_pkey" to "direction_route_pkey"
ALTER TABLE "direction_route" RENAME CONSTRAINT "direction_lever_pkey" TO "direction_route_pkey";
-- Rename a constraint from "direction_lever_state_pkey" to "direction_route_state_pkey"
ALTER TABLE "direction_route_state" RENAME CONSTRAINT "direction_lever_state_pkey" TO "direction_route_state_pkey";
-- Rename a constraint from "opening_lever_pkey" to "direction_self_control_lever_pkey"
ALTER TABLE "direction_self_control_lever" RENAME CONSTRAINT "opening_lever_pkey" TO "direction_self_control_lever_pkey";
-- Rename a constraint from "opening_lever_state_pkey" to "direction_self_control_lever_state_pkey"
ALTER TABLE "direction_self_control_lever_state" RENAME CONSTRAINT "opening_lever_state_pkey" TO "direction_self_control_lever_state_pkey";
-- Create "train_state" table
CREATE TABLE "train_state" ("id" bigserial NOT NULL, "train_number" character varying(100) NOT NULL, "dia_number" integer NOT NULL, "from_station_id" character varying(10) NOT NULL, "to_station_id" character varying(10) NOT NULL, "delay" integer NOT NULL DEFAULT 0, "driver_id" bigint NULL, PRIMARY KEY ("id"), CONSTRAINT "train_state_driver_id_key" UNIQUE ("driver_id"), CONSTRAINT "train_state_train_number_key" UNIQUE ("train_number"), CONSTRAINT "train_state_from_station_id_fkey" FOREIGN KEY ("from_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "train_state_to_station_id_fkey" FOREIGN KEY ("to_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "train_car_state" table
CREATE TABLE "train_car_state" ("train_state_id" bigint NOT NULL, "index" integer NOT NULL, "car_model" character varying(100) NOT NULL, "has_pantograph" boolean NOT NULL DEFAULT false, "has_driver_cab" boolean NOT NULL DEFAULT false, "has_conductor_cab" boolean NOT NULL DEFAULT false, "has_motor" boolean NOT NULL DEFAULT false, "door_close" boolean NOT NULL DEFAULT true, "bc_press" double precision NOT NULL DEFAULT 0, "ampare" double precision NOT NULL DEFAULT 0, PRIMARY KEY ("train_state_id", "index"), CONSTRAINT "train_car_state_train_state_id_fkey" FOREIGN KEY ("train_state_id") REFERENCES "train_state" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
