SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( ISNULL(cdp.Quantity,-1) <> -1 )
AND ( cdp.ItemId IN ( 2143, 6089, 6299, 6090, 6312, 6321, 6313, 6314, 6317, 3411, 6318, 8594, 3410, 6320, 6050 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
