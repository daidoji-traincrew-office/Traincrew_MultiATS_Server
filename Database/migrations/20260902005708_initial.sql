-- Create enum type "lcr"
CREATE TYPE "lcr" AS ENUM ('left', 'center', 'right');
-- Create enum type "lock_condition_type"
CREATE TYPE "lock_condition_type" AS ENUM ('and', 'or', 'not', 'object');
-- Create enum type "ttc_window_type"
CREATE TYPE "ttc_window_type" AS ENUM ('home_track', 'up', 'down', 'switching');
-- Create enum type "ttc_window_link_type"
CREATE TYPE "ttc_window_link_type" AS ENUM ('up', 'down', 'switching');
-- Create enum type "lock_type"
CREATE TYPE "lock_type" AS ENUM ('lock', 'signal_control', 'detector', 'route', 'approach', 'stick');
-- Create enum type "lever_type"
CREATE TYPE "lever_type" AS ENUM ('route', 'switching_machine', 'direction');
-- Create enum type "lr"
CREATE TYPE "lr" AS ENUM ('left', 'right');
-- Create enum type "nr"
CREATE TYPE "nr" AS ENUM ('reversed', 'normal');
-- Create enum type "signal_indication"
CREATE TYPE "signal_indication" AS ENUM ('R', 'YY', 'Y', 'YG', 'G');
-- Create enum type "nrc"
CREATE TYPE "nrc" AS ENUM ('reversed', 'center', 'normal');
-- Create enum type "raise_drop"
CREATE TYPE "raise_drop" AS ENUM ('raise', 'drop');
-- Create "protection_zone_state" table
CREATE TABLE "protection_zone_state" (
  "id" bigserial NOT NULL,
  "protection_zone" bigint NOT NULL,
  "train_number" character varying(100) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "protection_zone_state_protection_zone_train_number_key" UNIQUE ("protection_zone", "train_number")
);
-- Create enum type "route_type"
CREATE TYPE "route_type" AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');
-- Create "user_disconnection_state" table
CREATE TABLE "user_disconnection_state" (
  "user_id" bigint NOT NULL,
  PRIMARY KEY ("user_id")
);
-- Create "OpenIddictApplications" table
CREATE TABLE "OpenIddictApplications" (
  "id" text NOT NULL,
  "application_type" character varying(50) NULL,
  "client_id" character varying(100) NULL,
  "client_secret" text NULL,
  "client_type" character varying(50) NULL,
  "concurrency_token" character varying(50) NULL,
  "consent_type" character varying(50) NULL,
  "display_name" text NULL,
  "display_names" text NULL,
  "json_web_key_set" text NULL,
  "permissions" text NULL,
  "post_logout_redirect_uris" text NULL,
  "properties" text NULL,
  "redirect_uris" text NULL,
  "requirements" text NULL,
  "settings" text NULL,
  CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY ("id")
);
-- Create index "IX_OpenIddictApplications_client_id" to table: "OpenIddictApplications"
CREATE UNIQUE INDEX "IX_OpenIddictApplications_client_id" ON "OpenIddictApplications" ("client_id");
-- Create enum type "operation_information_type"
CREATE TYPE "operation_information_type" AS ENUM ('advertisement', 'normal', 'delay', 'suspended');
-- Create enum type "server_mode"
CREATE TYPE "server_mode" AS ENUM ('off', 'private', 'public');
-- Create enum type "raise_drop_with_force"
CREATE TYPE "raise_drop_with_force" AS ENUM ('force_drop', 'drop', 'raise');
-- Create enum type "operation_notification_type"
CREATE TYPE "operation_notification_type" AS ENUM ('none', 'yokushi', 'tsuuchi', 'tsuuchi_kaijo', 'kaijo', 'shuppatsu', 'shuppatsu_jikoku', 'torikeshi', 'other', 'class', 'tenmatsusho');
-- Create enum type "object_type"
CREATE TYPE "object_type" AS ENUM ('route', 'switching_machine', 'track_circuit', 'lever', 'direction_route', 'direction_self_control_lever', 'route_central_control_lever');
-- Create "OpenIddictScopes" table
CREATE TABLE "OpenIddictScopes" (
  "id" text NOT NULL,
  "concurrency_token" character varying(50) NULL,
  "description" text NULL,
  "descriptions" text NULL,
  "display_name" text NULL,
  "display_names" text NULL,
  "name" character varying(200) NULL,
  "properties" text NULL,
  "resources" text NULL,
  CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY ("id")
);
-- Create index "IX_OpenIddictScopes_name" to table: "OpenIddictScopes"
CREATE UNIQUE INDEX "IX_OpenIddictScopes_name" ON "OpenIddictScopes" ("name");
-- Create "operation_information_state" table
CREATE TABLE "operation_information_state" (
  "id" bigserial NOT NULL,
  "type" "operation_information_type" NOT NULL,
  "content" text NOT NULL,
  "start_time" timestamp NOT NULL,
  "end_time" timestamp NOT NULL,
  PRIMARY KEY ("id")
);
-- Create index "operation_information_state_end_time_index" to table: "operation_information_state"
CREATE INDEX "operation_information_state_end_time_index" ON "operation_information_state" ("end_time");
-- Create index "operation_information_state_start_time_index" to table: "operation_information_state"
CREATE INDEX "operation_information_state_start_time_index" ON "operation_information_state" ("start_time");
-- Create enum type "throw_out_control_type"
CREATE TYPE "throw_out_control_type" AS ENUM ('with_lever', 'without_lever', 'direction');
-- Create "OpenIddictAuthorizations" table
CREATE TABLE "OpenIddictAuthorizations" (
  "id" text NOT NULL,
  "application_id" text NULL,
  "concurrency_token" character varying(50) NULL,
  "creation_date" timestamptz NULL,
  "properties" text NULL,
  "scopes" text NULL,
  "status" character varying(50) NULL,
  "subject" character varying(400) NULL,
  "type" character varying(50) NULL,
  CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY ("id"),
  CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_application~" FOREIGN KEY ("application_id") REFERENCES "OpenIddictApplications" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "IX_OpenIddictAuthorizations_application_id_status_subject_type" to table: "OpenIddictAuthorizations"
CREATE INDEX "IX_OpenIddictAuthorizations_application_id_status_subject_type" ON "OpenIddictAuthorizations" ("application_id", "status", "subject", "type");
-- Create "OpenIddictTokens" table
CREATE TABLE "OpenIddictTokens" (
  "id" text NOT NULL,
  "application_id" text NULL,
  "authorization_id" text NULL,
  "concurrency_token" character varying(50) NULL,
  "creation_date" timestamptz NULL,
  "expiration_date" timestamptz NULL,
  "payload" text NULL,
  "properties" text NULL,
  "redemption_date" timestamptz NULL,
  "reference_id" character varying(100) NULL,
  "status" character varying(50) NULL,
  "subject" character varying(400) NULL,
  "type" character varying(50) NULL,
  CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY ("id"),
  CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_application_id" FOREIGN KEY ("application_id") REFERENCES "OpenIddictApplications" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_authorization_id" FOREIGN KEY ("authorization_id") REFERENCES "OpenIddictAuthorizations" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "IX_OpenIddictTokens_authorization_id" to table: "OpenIddictTokens"
CREATE INDEX "IX_OpenIddictTokens_authorization_id" ON "OpenIddictTokens" ("authorization_id");
-- Create index "IX_OpenIddictTokens_reference_id" to table: "OpenIddictTokens"
CREATE UNIQUE INDEX "IX_OpenIddictTokens_reference_id" ON "OpenIddictTokens" ("reference_id");
-- Create "station" table
CREATE TABLE "station" (
  "id" character varying(10) NOT NULL,
  "name" character varying(100) NOT NULL,
  "is_station" boolean NOT NULL,
  "is_passenger_station" boolean NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "station_name_key" UNIQUE ("name")
);
-- Create "destination_button" table
CREATE TABLE "destination_button" (
  "name" character varying(100) NOT NULL,
  "station_id" character varying(10) NOT NULL,
  PRIMARY KEY ("name"),
  CONSTRAINT "destination_button_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "destination_button_state" table
CREATE TABLE "destination_button_state" (
  "name" character varying(100) NOT NULL,
  "is_raised" "raise_drop" NOT NULL,
  "operated_at" timestamp NOT NULL,
  PRIMARY KEY ("name"),
  CONSTRAINT "destination_button_state_name_fkey" FOREIGN KEY ("name") REFERENCES "destination_button" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "diagram" table
CREATE TABLE "diagram" (
  "id" bigserial NOT NULL,
  "name" text NOT NULL,
  "version" text NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "diagram_name_key" UNIQUE ("name")
);
-- Create "train_type" table
CREATE TABLE "train_type" (
  "id" bigint NOT NULL,
  "name" character varying(100) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "train_type_name_key" UNIQUE ("name")
);
-- Create "diagram_train" table
CREATE TABLE "diagram_train" (
  "id" bigserial NOT NULL,
  "dia_id" bigint NOT NULL,
  "train_number" character varying(100) NOT NULL,
  "train_type_id" bigint NOT NULL,
  "from_station_id" character varying(10) NOT NULL,
  "to_station_id" character varying(10) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "diagram_train_dia_id_fkey" FOREIGN KEY ("dia_id") REFERENCES "diagram" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "diagram_train_from_station_id_fkey" FOREIGN KEY ("from_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "diagram_train_to_station_id_fkey" FOREIGN KEY ("to_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "diagram_train_train_type_id_fkey" FOREIGN KEY ("train_type_id") REFERENCES "train_type" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "idx_diagram_train_dia_id_train_number" to table: "diagram_train"
CREATE UNIQUE INDEX "idx_diagram_train_dia_id_train_number" ON "diagram_train" ("dia_id", "train_number");
-- Create "diagram_train_timetable" table
CREATE TABLE "diagram_train_timetable" (
  "id" bigserial NOT NULL,
  "train_diagram_id" bigint NOT NULL,
  "index" integer NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "track_number" character varying(50) NOT NULL,
  "arrival_time" interval NULL,
  "departure_time" interval NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "diagram_train_timetable_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "diagram_train_timetable_train_diagram_id_fkey" FOREIGN KEY ("train_diagram_id") REFERENCES "diagram_train" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "idx_diagram_train_timetable_train_diagram_id_index" to table: "diagram_train_timetable"
CREATE UNIQUE INDEX "idx_diagram_train_timetable_train_diagram_id_index" ON "diagram_train_timetable" ("train_diagram_id", "index");
-- Create "interlocking_object" table
CREATE TABLE "interlocking_object" (
  "id" bigserial NOT NULL,
  "type" "object_type" NOT NULL,
  "name" character varying(100) NOT NULL,
  "station_id" character varying(10) NULL,
  "description" text NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "interlocking_object_name_key" UNIQUE ("name"),
  CONSTRAINT "interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "direction_self_control_lever" table
CREATE TABLE "direction_self_control_lever" (
  "id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "direction_self_control_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "switching_machine" table
CREATE TABLE "switching_machine" (
  "id" bigint NOT NULL,
  "tc_name" character varying(100) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "switching_machine_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "lever" table
CREATE TABLE "lever" (
  "id" bigint NOT NULL,
  "lever_type" "lever_type" NOT NULL,
  "switching_machine_id" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "lever_switching_machine_id_fkey" FOREIGN KEY ("switching_machine_id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "direction_route" table
CREATE TABLE "direction_route" (
  "id" bigint NOT NULL,
  "lever_id" bigint NOT NULL,
  "direction_self_control_lever_id" bigint NULL,
  "l_lock_lever_id" bigint NULL,
  "l_lock_lever_direction" "lr" NULL,
  "l_single_locked_lever_id" bigint NULL,
  "l_single_locked_lever_direction" "lr" NULL,
  "r_lock_lever_id" bigint NULL,
  "r_lock_lever_direction" "lr" NULL,
  "r_single_locked_lever_id" bigint NULL,
  "r_single_locked_lever_direction" "lr" NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "direction_route_direction_self_control_lever_id_fkey" FOREIGN KEY ("direction_self_control_lever_id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_l_lock_lever_id_fkey" FOREIGN KEY ("l_lock_lever_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_l_single_locked_lever_id_fkey" FOREIGN KEY ("l_single_locked_lever_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_r_lock_lever_id_fkey" FOREIGN KEY ("r_lock_lever_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "direction_route_r_single_locked_lever_id_fkey" FOREIGN KEY ("r_single_locked_lever_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "direction_route_state" table
CREATE TABLE "direction_route_state" (
  "id" bigint NOT NULL,
  "is_lr" "lr" NOT NULL DEFAULT 'left',
  PRIMARY KEY ("id"),
  CONSTRAINT "direction_route_state_id_fkey" FOREIGN KEY ("id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "direction_self_control_lever_state" table
CREATE TABLE "direction_self_control_lever_state" (
  "id" bigint NOT NULL,
  "is_inserted_key" boolean NOT NULL DEFAULT false,
  "is_reversed" "nr" NOT NULL DEFAULT 'normal',
  PRIMARY KEY ("id"),
  CONSTRAINT "direction_self_control_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "lever_state" table
CREATE TABLE "lever_state" (
  "id" bigint NOT NULL,
  "is_reversed" "lcr" NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "lock" table
CREATE TABLE "lock" (
  "id" bigserial NOT NULL,
  "object_id" bigint NULL,
  "type" "lock_type" NOT NULL,
  "route_lock_group" integer NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lock_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "lock_object_id_type_index" to table: "lock"
CREATE INDEX "lock_object_id_type_index" ON "lock" ("object_id", "type");
-- Create "lock_condition" table
CREATE TABLE "lock_condition" (
  "id" bigserial NOT NULL,
  "lock_id" bigint NOT NULL,
  "parent_id" bigint NULL,
  "type" "lock_condition_type" NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lock_condition_lock_id_fkey" FOREIGN KEY ("lock_id") REFERENCES "lock" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "lock_condition_parent_id_fkey" FOREIGN KEY ("parent_id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "lock_condition_lock_id_index" to table: "lock_condition"
CREATE INDEX "lock_condition_lock_id_index" ON "lock_condition" ("lock_id");
-- Create "route_central_control_lever" table
CREATE TABLE "route_central_control_lever" (
  "id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "route_central_control_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "operation_notification_display" table
CREATE TABLE "operation_notification_display" (
  "name" character varying(100) NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "is_up" boolean NOT NULL,
  "is_down" boolean NOT NULL,
  PRIMARY KEY ("name"),
  CONSTRAINT "operation_notification_display_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "track_circuit" table
CREATE TABLE "track_circuit" (
  "id" bigint NOT NULL,
  "protection_zone" integer NOT NULL,
  "operation_notification_display_name" character varying(100) NULL,
  "station_id_for_delay" character varying(10) NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "track_circuit_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "track_circuit_operation_notification_display_name_fkey" FOREIGN KEY ("operation_notification_display_name") REFERENCES "operation_notification_display" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "track_circuit_station_id_for_delay_fkey" FOREIGN KEY ("station_id_for_delay") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "track_circuit_operation_notification_display_name_index" to table: "track_circuit"
CREATE INDEX "track_circuit_operation_notification_display_name_index" ON "track_circuit" ("operation_notification_display_name");
-- Create "route" table
CREATE TABLE "route" (
  "id" bigint NOT NULL,
  "tc_name" character varying(100) NOT NULL,
  "route_type" "route_type" NOT NULL,
  "root_id" bigint NULL,
  "indicator" character varying(10) NULL,
  "approach_lock_time" integer NULL,
  "approach_lock_final_track_circuit_id" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "route_approach_lock_final_track_circuit_id_fkey" FOREIGN KEY ("approach_lock_final_track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_root_id_fkey" FOREIGN KEY ("root_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "lock_condition_by_route_central_control_lever" table
CREATE TABLE "lock_condition_by_route_central_control_lever" (
  "id" bigserial NOT NULL,
  "route_id" bigint NOT NULL,
  "route_central_control_lever_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lock_condition_by_route_centr_route_id_route_central_contro_key" UNIQUE ("route_id", "route_central_control_lever_id"),
  CONSTRAINT "lock_condition_by_route_centr_route_central_control_lever__fkey" FOREIGN KEY ("route_central_control_lever_id") REFERENCES "route_central_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "lock_condition_by_route_central_control_lever_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "lock_condition_by_route_central_control_lever_rcl_id_index" to table: "lock_condition_by_route_central_control_lever"
CREATE INDEX "lock_condition_by_route_central_control_lever_rcl_id_index" ON "lock_condition_by_route_central_control_lever" ("route_central_control_lever_id");
-- Create index "lock_condition_by_route_central_control_lever_route_id_index" to table: "lock_condition_by_route_central_control_lever"
CREATE INDEX "lock_condition_by_route_central_control_lever_route_id_index" ON "lock_condition_by_route_central_control_lever" ("route_id");
-- Create "lock_condition_object" table
CREATE TABLE "lock_condition_object" (
  "id" bigint NOT NULL,
  "object_id" bigint NOT NULL,
  "timer_seconds" integer NULL,
  "is_reverse" "nr" NOT NULL,
  "is_single_lock" boolean NOT NULL,
  "is_lr" "lr" NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "lock_condition_object_id_fkey" FOREIGN KEY ("id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "lock_condition_object_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "signal_type" table
CREATE TABLE "signal_type" (
  "name" character varying(100) NOT NULL,
  "r_indication" "signal_indication" NOT NULL,
  "yy_indication" "signal_indication" NOT NULL,
  "y_indication" "signal_indication" NOT NULL,
  "yg_indication" "signal_indication" NOT NULL,
  "g_indication" "signal_indication" NOT NULL,
  PRIMARY KEY ("name")
);
-- Create "signal" table
CREATE TABLE "signal" (
  "name" character varying(100) NOT NULL,
  "station_id" character varying(10) NULL,
  "type" character varying(100) NOT NULL,
  "track_circuit_id" bigint NULL,
  "direction_route_left_id" bigint NULL,
  "direction_route_right_id" bigint NULL,
  "direction" "lr" NULL,
  PRIMARY KEY ("name"),
  CONSTRAINT "signal_direction_route_left_id_fkey" FOREIGN KEY ("direction_route_left_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "signal_direction_route_right_id_fkey" FOREIGN KEY ("direction_route_right_id") REFERENCES "direction_route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "signal_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "signal_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "signal_type_fkey" FOREIGN KEY ("type") REFERENCES "signal_type" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "signal_station_id_index" to table: "signal"
CREATE INDEX "signal_station_id_index" ON "signal" ("station_id");
-- Create "next_signal" table
CREATE TABLE "next_signal" (
  "id" bigserial NOT NULL,
  "signal_name" character varying(100) NOT NULL,
  "source_signal_name" character varying(100) NOT NULL,
  "target_signal_name" character varying(100) NOT NULL,
  "depth" integer NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "next_signal_signal_name_target_signal_name_key" UNIQUE ("signal_name", "target_signal_name"),
  CONSTRAINT "next_signal_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "next_signal_source_signal_name_fkey" FOREIGN KEY ("source_signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "next_signal_target_signal_name_fkey" FOREIGN KEY ("target_signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "next_signal_signal_name_index" to table: "next_signal"
CREATE INDEX "next_signal_signal_name_index" ON "next_signal" ("signal_name");
-- Create "operation_notification_state" table
CREATE TABLE "operation_notification_state" (
  "display_name" character varying(100) NOT NULL,
  "type" "operation_notification_type" NOT NULL,
  "content" text NOT NULL,
  "operated_at" timestamp NOT NULL,
  PRIMARY KEY ("display_name"),
  CONSTRAINT "operation_notification_state_display_name_fkey" FOREIGN KEY ("display_name") REFERENCES "operation_notification_display" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "route_central_control_lever_state" table
CREATE TABLE "route_central_control_lever_state" (
  "id" bigint NOT NULL,
  "is_inserted_key" boolean NOT NULL DEFAULT false,
  "is_reversed" "nr" NOT NULL DEFAULT 'normal',
  "is_center_controlled" boolean NOT NULL DEFAULT false,
  PRIMARY KEY ("id"),
  CONSTRAINT "route_central_control_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "route_central_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "route_include" table
CREATE TABLE "route_include" (
  "source_lever_id" bigint NOT NULL,
  "target_lever_id" bigint NOT NULL,
  CONSTRAINT "route_include_source_lever_id_target_lever_id_key" UNIQUE ("source_lever_id", "target_lever_id"),
  CONSTRAINT "route_include_source_lever_id_fkey" FOREIGN KEY ("source_lever_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_include_target_lever_id_fkey" FOREIGN KEY ("target_lever_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "route_include_source_lever_id_index" to table: "route_include"
CREATE INDEX "route_include_source_lever_id_index" ON "route_include" ("source_lever_id");
-- Create "route_lever_destination_button" table
CREATE TABLE "route_lever_destination_button" (
  "id" bigserial NOT NULL,
  "route_id" bigint NOT NULL,
  "lever_id" bigint NOT NULL,
  "destination_button_name" character varying(100) NULL,
  "direction" "lr" NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "route_lever_destination_butto_lever_id_destination_button_n_key" UNIQUE NULLS NOT DISTINCT ("lever_id", "destination_button_name", "direction"),
  CONSTRAINT "route_lever_destination_button_route_id_key" UNIQUE ("route_id"),
  CONSTRAINT "route_lever_destination_button_destination_button_name_fkey" FOREIGN KEY ("destination_button_name") REFERENCES "destination_button" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_lever_destination_button_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_lever_destination_button_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "route_lock_track_circuit" table
CREATE TABLE "route_lock_track_circuit" (
  "id" bigserial NOT NULL,
  "route_id" bigint NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "route_lock_track_circuit_route_id_track_circuit_id_key" UNIQUE ("route_id", "track_circuit_id"),
  CONSTRAINT "route_lock_track_circuit_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "route_lock_track_circuit_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "route_state" table
CREATE TABLE "route_state" (
  "id" bigint NOT NULL,
  "is_signal_control_raised" "raise_drop" NOT NULL,
  "is_route_secured" "raise_drop" NOT NULL DEFAULT 'drop',
  "is_ctc_controlled" "raise_drop" NOT NULL DEFAULT 'drop',
  PRIMARY KEY ("id"),
  CONSTRAINT "route_state_id_fkey" FOREIGN KEY ("id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "server_state" table
CREATE TABLE "server_state" (
  "id" serial NOT NULL,
  "mode" "server_mode" NOT NULL,
  "time_offset" integer NOT NULL DEFAULT 0,
  "switch_move_time" integer NOT NULL DEFAULT 5000,
  "switch_return_time" integer NOT NULL DEFAULT 500,
  "use_one_second_relay" boolean NOT NULL DEFAULT false,
  "is_all_signal_relay_raised" "raise_drop_with_force" NOT NULL DEFAULT 'drop',
  "interlocking_heartbeat_at" timestamp NULL,
  "selected_diagram_id" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "server_state_selected_diagram_id_fkey" FOREIGN KEY ("selected_diagram_id") REFERENCES "diagram" ("id") ON UPDATE NO ACTION ON DELETE SET NULL
);
-- Create "signal_route" table
CREATE TABLE "signal_route" (
  "id" bigserial NOT NULL,
  "signal_name" character varying(100) NOT NULL,
  "route_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "signal_route_signal_name_route_id_key" UNIQUE ("signal_name", "route_id"),
  CONSTRAINT "signal_route_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "signal_route_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "signal_route_signal_name_index" to table: "signal_route"
CREATE INDEX "signal_route_signal_name_index" ON "signal_route" ("signal_name");
-- Create "signal_state" table
CREATE TABLE "signal_state" (
  "signal_name" character varying(100) NOT NULL,
  "is_lighted" boolean NOT NULL,
  PRIMARY KEY ("signal_name"),
  CONSTRAINT "signal_state_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "station_interlocking_object" table
CREATE TABLE "station_interlocking_object" (
  "station_id" character varying(10) NOT NULL,
  "object_id" bigint NOT NULL,
  CONSTRAINT "station_interlocking_object_station_id_object_id_key" UNIQUE ("station_id", "object_id"),
  CONSTRAINT "station_interlocking_object_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "station_interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "station_interlocking_object_station_id_index" to table: "station_interlocking_object"
CREATE INDEX "station_interlocking_object_station_id_index" ON "station_interlocking_object" ("station_id");
-- Create "station_timer_state" table
CREATE TABLE "station_timer_state" (
  "id" bigserial NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "seconds" integer NOT NULL,
  "is_timer_condition_met" boolean NOT NULL DEFAULT false,
  PRIMARY KEY ("id"),
  CONSTRAINT "station_timer_state_station_id_seconds_key" UNIQUE ("station_id", "seconds"),
  CONSTRAINT "station_timer_state_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "switching_machine_route" table
CREATE TABLE "switching_machine_route" (
  "id" bigserial NOT NULL,
  "switching_machine_id" bigint NOT NULL,
  "route_id" bigint NOT NULL,
  "is_reverse" "nr" NOT NULL,
  "on_route_lock" boolean NOT NULL DEFAULT false,
  PRIMARY KEY ("id"),
  CONSTRAINT "switching_machine_route_switching_machine_id_route_id_key" UNIQUE ("switching_machine_id", "route_id"),
  CONSTRAINT "switching_machine_route_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "switching_machine_route_switching_machine_id_fkey" FOREIGN KEY ("switching_machine_id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "switching_machine_route_route_id_index" to table: "switching_machine_route"
CREATE INDEX "switching_machine_route_route_id_index" ON "switching_machine_route" ("route_id");
-- Create index "switching_machine_route_switching_machine_id_index" to table: "switching_machine_route"
CREATE INDEX "switching_machine_route_switching_machine_id_index" ON "switching_machine_route" ("switching_machine_id");
-- Create "switching_machine_state" table
CREATE TABLE "switching_machine_state" (
  "id" bigint NOT NULL,
  "is_switching" boolean NOT NULL,
  "is_reverse" "nr" NOT NULL,
  "switch_end_time" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY ("id"),
  CONSTRAINT "switching_machine_state_id_fkey" FOREIGN KEY ("id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "throw_out_control" table
CREATE TABLE "throw_out_control" (
  "id" bigserial NOT NULL,
  "control_type" "throw_out_control_type" NOT NULL,
  "source_id" bigint NOT NULL,
  "source_lr" "lr" NULL,
  "target_id" bigint NOT NULL,
  "target_lr" "lr" NULL,
  "condition_lever_id" bigint NULL,
  "condition_nr" "nr" NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "throw_out_control_condition_lever_id_fkey" FOREIGN KEY ("condition_lever_id") REFERENCES "direction_self_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "throw_out_control_source_id_fkey" FOREIGN KEY ("source_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "throw_out_control_target_id_fkey" FOREIGN KEY ("target_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "throw_out_control_source_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_source_id_index" ON "throw_out_control" ("source_id");
-- Create index "throw_out_control_target_id_index" to table: "throw_out_control"
CREATE INDEX "throw_out_control_target_id_index" ON "throw_out_control" ("target_id");
-- Create "track_circuit_department_time" table
CREATE TABLE "track_circuit_department_time" (
  "id" bigserial NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  "car_count" integer NOT NULL,
  "time_element" integer NOT NULL,
  "is_up" boolean NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "track_circuit_department_time_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "track_circuit_department_time_track_circuit_id_car_count_is_up_" to table: "track_circuit_department_time"
CREATE UNIQUE INDEX "track_circuit_department_time_track_circuit_id_car_count_is_up_" ON "track_circuit_department_time" ("track_circuit_id", "car_count", "is_up");
-- Create "track_circuit_signal" table
CREATE TABLE "track_circuit_signal" (
  "id" bigserial NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  "is_up" boolean NOT NULL,
  "signal_name" character varying(100) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "track_circuit_signal_track_circuit_id_is_up_signal_name_key" UNIQUE ("track_circuit_id", "is_up", "signal_name"),
  CONSTRAINT "track_circuit_signal_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "track_circuit_signal_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "track_circuit_state" table
CREATE TABLE "track_circuit_state" (
  "id" bigint NOT NULL,
  "train_number" character varying(100) NULL,
  "is_short_circuit" boolean NOT NULL,
  "is_locked" boolean NOT NULL,
  "unlocked_at" timestamp NULL,
  "locked_by" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "track_circuit_state_id_fkey" FOREIGN KEY ("id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "track_circuit_state_locked_by_fkey" FOREIGN KEY ("locked_by") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "track_circuit_state_train_number_index" to table: "track_circuit_state"
CREATE INDEX "track_circuit_state_train_number_index" ON "track_circuit_state" USING HASH ("train_number");
-- Create "train_state" table
CREATE TABLE "train_state" (
  "id" bigserial NOT NULL,
  "train_number" character varying(100) NOT NULL,
  "dia_number" integer NOT NULL,
  "from_station_id" character varying(10) NOT NULL,
  "to_station_id" character varying(10) NOT NULL,
  "delay" integer NOT NULL DEFAULT 0,
  "driver_id" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "train_state_driver_id_key" UNIQUE ("driver_id"),
  CONSTRAINT "train_state_train_number_key" UNIQUE ("train_number"),
  CONSTRAINT "train_state_from_station_id_fkey" FOREIGN KEY ("from_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "train_state_to_station_id_fkey" FOREIGN KEY ("to_station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "train_state_dia_number_index" to table: "train_state"
CREATE INDEX "train_state_dia_number_index" ON "train_state" USING HASH ("dia_number");
-- Create "train_car_state" table
CREATE TABLE "train_car_state" (
  "train_state_id" bigint NOT NULL,
  "index" integer NOT NULL,
  "car_model" character varying(100) NOT NULL,
  "has_pantograph" boolean NOT NULL DEFAULT false,
  "has_driver_cab" boolean NOT NULL DEFAULT false,
  "has_conductor_cab" boolean NOT NULL DEFAULT false,
  "has_motor" boolean NOT NULL DEFAULT false,
  "door_close" boolean NOT NULL DEFAULT true,
  "bc_press" double precision NOT NULL DEFAULT 0,
  "ampare" double precision NOT NULL DEFAULT 0,
  PRIMARY KEY ("train_state_id", "index"),
  CONSTRAINT "train_car_state_train_state_id_fkey" FOREIGN KEY ("train_state_id") REFERENCES "train_state" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "train_signal_state" table
CREATE TABLE "train_signal_state" (
  "id" bigserial NOT NULL,
  "train_number" character varying(100) NOT NULL,
  "signal_name" character varying(100) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "train_signal_state_train_number_signal_name_key" UNIQUE ("train_number", "signal_name"),
  CONSTRAINT "train_signal_state_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "train_signal_state_signal_name_index" to table: "train_signal_state"
CREATE INDEX "train_signal_state_signal_name_index" ON "train_signal_state" ("signal_name");
-- Create index "train_signal_state_train_number_index" to table: "train_signal_state"
CREATE INDEX "train_signal_state_train_number_index" ON "train_signal_state" ("train_number");
-- Create "ttc_window" table
CREATE TABLE "ttc_window" (
  "name" character varying(100) NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "type" "ttc_window_type" NOT NULL,
  PRIMARY KEY ("name"),
  CONSTRAINT "ttc_window_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "ttc_window_display_station" table
CREATE TABLE "ttc_window_display_station" (
  "id" bigserial NOT NULL,
  "ttc_window_name" character varying(100) NOT NULL,
  "station_id" character varying(10) NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_window_display_station_ttc_window_name_station_id_key" UNIQUE ("ttc_window_name", "station_id"),
  CONSTRAINT "ttc_window_display_station_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_window_display_station_ttc_window_name_fkey" FOREIGN KEY ("ttc_window_name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "ttc_window_link" table
CREATE TABLE "ttc_window_link" (
  "id" bigserial NOT NULL,
  "source_ttc_window_name" character varying(100) NOT NULL,
  "target_ttc_window_name" character varying(100) NOT NULL,
  "type" "ttc_window_link_type" NOT NULL,
  "is_empty_sending" boolean NOT NULL,
  "track_circuit_condition" bigint NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_window_link_source_ttc_window_name_target_ttc_window_na_key" UNIQUE ("source_ttc_window_name", "target_ttc_window_name"),
  CONSTRAINT "ttc_window_link_source_ttc_window_name_fkey" FOREIGN KEY ("source_ttc_window_name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_window_link_target_ttc_window_name_fkey" FOREIGN KEY ("target_ttc_window_name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_window_link_track_circuit_condition_fkey" FOREIGN KEY ("track_circuit_condition") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "ttc_window_link_route_condition" table
CREATE TABLE "ttc_window_link_route_condition" (
  "id" bigserial NOT NULL,
  "ttc_window_link_id" bigint NOT NULL,
  "route_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_window_link_route_condition_ttc_window_link_id_route_id_key" UNIQUE ("ttc_window_link_id", "route_id"),
  CONSTRAINT "ttc_window_link_route_condition_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_window_link_route_condition_ttc_window_link_id_fkey" FOREIGN KEY ("ttc_window_link_id") REFERENCES "ttc_window_link" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "ttc_window_state" table
CREATE TABLE "ttc_window_state" (
  "name" character varying(100) NOT NULL,
  "train_number" character varying(100) NOT NULL,
  CONSTRAINT "ttc_window_state_name_fkey" FOREIGN KEY ("name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create "ttc_window_track_circuit" table
CREATE TABLE "ttc_window_track_circuit" (
  "id" bigserial NOT NULL,
  "ttc_window_name" character varying(100) NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  PRIMARY KEY ("id"),
  CONSTRAINT "ttc_window_track_circuit_ttc_window_name_track_circuit_id_key" UNIQUE ("ttc_window_name", "track_circuit_id"),
  CONSTRAINT "ttc_window_track_circuit_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "ttc_window_track_circuit_ttc_window_name_fkey" FOREIGN KEY ("ttc_window_name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION
);
