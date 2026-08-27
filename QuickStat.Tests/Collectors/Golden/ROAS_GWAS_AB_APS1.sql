SELECT a.* FROM
(
select ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, cdp.ItemId,
RANK() OVER ( PARTITION BY ce.PersonId, cdp.ItemId ORDER BY Quantity DESC, cdp.RowId DESC ) AS rnk
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( cdp.ItemId IN ( 6076, 6077, 6078, 6079, 6080, 6073, 6045, 6074 ) ) ) a where a.rnk = 1
