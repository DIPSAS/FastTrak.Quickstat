SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( ISNULL(cdp.Quantity,-1) <> -1 )
AND ( cdp.ItemId IN ( 3351, 3352, 4235, 3218, 3397, 3398, 3414, 3415, 3417, 4054, 4055, 4062, 4087, 4205, 4521, 4527, 4845, 7517, 7519, 7520, 7521 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
