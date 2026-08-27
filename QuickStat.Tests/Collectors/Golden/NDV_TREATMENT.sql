SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( ISNULL(cdp.Quantity,-1) <> -1 )
AND ( cdp.ItemId IN ( 3322, 4056 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
