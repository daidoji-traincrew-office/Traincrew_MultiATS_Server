-- Create enum type "ttc_path_type"
CREATE TYPE "ttc_path_type" AS ENUM ('arriving', 'departure', 'switching');
-- Create "ttc_path" table
CREATE TABLE "ttc_path" (
  "id" bigserial NOT NULL,
  "name" text NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "source_track" text NOT NULL,
  "target_track" text NOT NULL,
  "type" "ttc_path_type" NOT NULL,
  "define_order_track_circuit_count" integer NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_path_name_key" UNIQUE ("name"),
  CONSTRAINT "ttc_path_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_define_order_track_circuit_count_check" CHECK (define_order_track_circuit_count >= 0)
);
-- Create "ttc_path_approach_track_circuit" table
CREATE TABLE "ttc_path_approach_track_circuit" (
  "id" bigserial NOT NULL,
  "index" integer NOT NULL,
  "ttc_path_id" bigint NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_path_approach_track_circui_ttc_path_id_track_circuit_id_key" UNIQUE ("ttc_path_id", "track_circuit_id"),
  CONSTRAINT "ttc_path_approach_track_circuit_ttc_path_id_index_key" UNIQUE ("ttc_path_id", "index"),
  CONSTRAINT "ttc_path_approach_track_circuit_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_approach_track_circuit_ttc_path_id_fkey" FOREIGN KEY ("ttc_path_id") REFERENCES "ttc_path" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_approach_track_circuit_index_check" CHECK (index >= 0)
);
-- Create "ttc_path_predecessor" table
CREATE TABLE "ttc_path_predecessor" (
  "id" bigserial NOT NULL,
  "index" integer NOT NULL,
  "source_ttc_path_id" bigint NOT NULL,
  "target_ttc_path_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_path_predecessor_source_ttc_path_id_index_key" UNIQUE ("source_ttc_path_id", "index"),
  CONSTRAINT "ttc_path_predecessor_source_ttc_path_id_target_ttc_path_id_key" UNIQUE ("source_ttc_path_id", "target_ttc_path_id"),
  CONSTRAINT "ttc_path_predecessor_source_ttc_path_id_fkey" FOREIGN KEY ("source_ttc_path_id") REFERENCES "ttc_path" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_predecessor_target_ttc_path_id_fkey" FOREIGN KEY ("target_ttc_path_id") REFERENCES "ttc_path" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_predecessor_check" CHECK (source_ttc_path_id <> target_ttc_path_id),
  CONSTRAINT "ttc_path_predecessor_index_check" CHECK (index >= 0)
);
-- Create "ttc_path_route" table
CREATE TABLE "ttc_path_route" (
  "id" bigserial NOT NULL,
  "index" integer NOT NULL,
  "ttc_path_id" bigint NOT NULL,
  "route_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_path_route_ttc_path_id_index_key" UNIQUE ("ttc_path_id", "index"),
  CONSTRAINT "ttc_path_route_ttc_path_id_route_id_key" UNIQUE ("ttc_path_id", "route_id"),
  CONSTRAINT "ttc_path_route_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_route_ttc_path_id_fkey" FOREIGN KEY ("ttc_path_id") REFERENCES "ttc_path" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_path_route_index_check" CHECK (index >= 0)
);
