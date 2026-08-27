SELECT ce.PersonId, mi.VarName, cdp.Quantity, ce.EventTime, cdp.RowId
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( cdp.ItemId = 4342 )
AND ( NOT cdp.Quantity IS NULL )
AND DATEDIFF( MM, ce.EventTime, GETDATE()) < 6
ORDER BY ce.EventNum
