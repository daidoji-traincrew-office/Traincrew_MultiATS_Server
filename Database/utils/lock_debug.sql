-- 鎖錠条件を確認する用SQL
SELECT 
    io1.name, lock.type,
    lc.id, lc.parent_id,
    lc.type, io2.name, io2.type, lco.is_reverse
FROM lock 
    join interlocking_object io1 on lock.object_id = io1.id
    join lock_condition lc on lock.id = lc.lock_id
    left join lock_condition_object lco on lc.id = lco.id
    left join interlocking_object io2 on lco.object_id = io2.id
WHERE io1.name like 'TH67_22%'
ORDER BY lock.id;
