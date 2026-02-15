-- Create enum type "phone_call_status"
CREATE TYPE "phone_call_status" AS ENUM ('calling', 'answered', 'rejected', 'busy', 'held', 'ended');
-- Create "phone_call_session" table
CREATE TABLE "phone_call_session" (
  "id" bigserial NOT NULL,
  "caller_number" character varying(20) NOT NULL,
  "caller_connection_id" character varying(256) NOT NULL,
  "target_number" character varying(20) NOT NULL,
  "target_connection_id" character varying(256) NULL,
  "status" "phone_call_status" NOT NULL DEFAULT 'calling',
  "created_at" timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "ended_at" timestamp NULL,
  PRIMARY KEY ("id")
);
