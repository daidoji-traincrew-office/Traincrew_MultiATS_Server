-- Modify "signal" table
ALTER TABLE "signal" ADD COLUMN "should_send" boolean NOT NULL DEFAULT true;
