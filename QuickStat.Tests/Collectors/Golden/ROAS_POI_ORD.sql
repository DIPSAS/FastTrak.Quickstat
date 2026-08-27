SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( ISNULL(cdp.Quantity,-1) <> -1 )
AND ( cdp.ItemId IN ( 2143, 6299, 6090, 6314, 6321, 6663, 6312, 6313, 6318, 6806, 3410, 7977, 3411, 6320, 6322, 7978, 6317, 6316, 8543, 6050 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
