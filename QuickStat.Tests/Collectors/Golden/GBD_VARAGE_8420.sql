SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, DATALENGTH(cdp.TextVal) AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( NOT cdp.TextVal IS NULL )
AND ( cdp.ItemId IN ( 8420 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
