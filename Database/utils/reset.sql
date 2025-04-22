-- 全リセット用SQL
-- 実行注意！！！環境確認良いか？
delete from switching_machine_route;
delete from lock_condition_object;
delete from lock_condition;
delete from lock;

delete from signal_state;
delete from next_signal;
delete from track_circuit_signal;
delete from signal_route;
delete from signal;
delete from track_circuit_state;
delete from route_lock_track_circuit;

delete from route_lever_destination_button;

delete from throw_out_control;
delete from route_state;
delete from route;
delete from track_circuit;

delete from operation_notification_state;
delete from operation_notification_display;


delete from destination_button_state;
delete from destination_button;

delete from lever_state;
delete from lever;

delete from switching_machine_route;
delete from switching_machine_state;
delete from switching_machine;



delete from interlocking_object;

delete from station_timer_state;
delete from station;
delete from signal_type;