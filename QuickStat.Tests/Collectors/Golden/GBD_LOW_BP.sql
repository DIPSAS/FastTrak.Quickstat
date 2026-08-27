SELECT v.PersonId, mi.VarName, v.Quantity AS DpValue, v.EventTime, 0 AS RowId
FROM dbo.GetLastQuantityTable( 3556, NULL ) v
JOIN dbo.MetaItem mi ON mi.ItemId = 3556
WHERE v.Quantity < 120
