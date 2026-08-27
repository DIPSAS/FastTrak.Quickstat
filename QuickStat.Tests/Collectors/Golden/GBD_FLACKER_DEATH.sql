SELECT PersonId, VarName, DataValue, EventTime, RowId, ReverseOrder
FROM
(
  SELECT ce.PersonId, cdp.Quantity AS FK_SCORE,
	 CONVERT(DECIMAL(18,4),DATEDIFF(DAY, ce.EventTime, p.DeceasedDate )) AS FK_DAYS_LIVED,
    ce.EventTime, cdp.RowId,
  ROW_NUMBER() OVER (PARTITION BY ce.PersonId ORDER BY ce.EventTime DESC ) AS ReverseOrder
  FROM dbo.ClinEvent ce
  JOIN dbo.ClinDataPoint cdp ON cdp.EventId = ce.EventId
  JOIN dbo.Person p ON p.PersonId = ce.PersonId
  WHERE cdp.ItemId = 1128
) AS SourceTable
UNPIVOT
( DataValue FOR VarName IN ( FK_SCORE, FK_DAYS_LIVED ) ) AS DestTable
WHERE ReverseOrder = 1
