-- Modify "train_car_state" table
ALTER TABLE "train_car_state" ADD COLUMN "occupancy_rate" double precision NOT NULL DEFAULT 0;
