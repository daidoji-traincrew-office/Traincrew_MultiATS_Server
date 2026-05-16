-- Drop index "track_circuit_department_time_track_circuit_id_car_count_index" from table: "track_circuit_department_time"
DROP INDEX "track_circuit_department_time_track_circuit_id_car_count_index";
-- Create index "track_circuit_department_time_track_circuit_id_car_count_is_up_" to table: "track_circuit_department_time"
CREATE UNIQUE INDEX "track_circuit_department_time_track_circuit_id_car_count_is_up_" ON "track_circuit_department_time" ("track_circuit_id", "car_count", "is_up");
