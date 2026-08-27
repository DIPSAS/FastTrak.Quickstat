SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId,mi.ItemId ORDER BY ce.EventTime DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND NOT ( cdp.Quantity IS NULL AND cdp.DTVal IS NULL AND cdp.TextVal IS NULL )
AND ( cdp.ItemId IN ( 3224 ) ) ) a
WHERE a.OrderBy = 1
