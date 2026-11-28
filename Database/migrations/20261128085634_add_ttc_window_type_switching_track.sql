-- Rename value of enum type: "ttc_window_type" ('switching' -> 'switching_main_track')
ALTER TYPE "ttc_window_type" RENAME VALUE 'switching' TO 'switching_main_track';
-- Add value to enum type: "ttc_window_type"
ALTER TYPE "ttc_window_type" ADD VALUE 'switching_track';
