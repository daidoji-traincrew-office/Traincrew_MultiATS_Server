-- 各進路のリレー条件を確認する用SQL
SELECT io.name, *
FROM route_state
JOIN interlocking_object io on io.id = route_state.id
WHERE io.name like 'TH67_25%'

ORDER BY io.id;