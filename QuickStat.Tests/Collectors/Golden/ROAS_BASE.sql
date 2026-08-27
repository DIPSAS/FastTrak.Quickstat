SELECT a.* FROM (
SELECT ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( ce.PersonId IN (/*PIDS*/) )
AND ( ISNULL(cdp.Quantity,-1) <> -1 )
AND ( cdp.ItemId IN ( 4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090, 6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326, 8594, 8595, 6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543, 8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635, 6636, 6637, 6638, 6639, 6640, 6808, 6641, 5170, 9996, 3983, 7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846, 3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807 ) ) ) a
WHERE a.OrderBy = 1 ORDER BY PersonId
