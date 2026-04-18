-- Create "track_circuit_department_time" table
CREATE TABLE "track_circuit_department_time" ("id" serial NOT NULL, "track_circuit_id" bigint NOT NULL, "car_count" integer NOT NULL, "time_element" integer NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "track_circuit_department_time_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "track_circuit_department_time_track_circuit_id_car_count_index" to table: "track_circuit_department_time"
CREATE UNIQUE INDEX "track_circuit_department_time_track_circuit_id_car_count_index" ON "track_circuit_department_time" ("track_circuit_id", "car_count");
