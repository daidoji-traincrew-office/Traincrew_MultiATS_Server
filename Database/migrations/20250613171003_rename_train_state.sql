-- Modify "train_state" table
ALTER TABLE "train_state" DROP CONSTRAINT "train_state_pkey", ADD COLUMN "id" bigserial NOT NULL, ADD PRIMARY KEY ("id"), ADD CONSTRAINT "train_state_driver_id_key" UNIQUE ("driver_id"), ADD CONSTRAINT "train_state_train_number_key" UNIQUE ("train_number");
-- Modify "train_car_state" table
ALTER TABLE "train_car_state" DROP CONSTRAINT "train_car_state_pkey", DROP COLUMN "train_number", ADD COLUMN "train_state_id" bigint NOT NULL, ADD PRIMARY KEY ("train_state_id", "index"), ADD CONSTRAINT "train_car_state_train_state_id_fkey" FOREIGN KEY ("train_state_id") REFERENCES "train_state" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
