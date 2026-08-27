SELECT PersonId, VarName, DpValue, EventTime, ClinFormId
FROM
(
  SELECT ce.PersonId, mf.FormName AS VarName, cf.FormComplete AS DpValue, ce.EventTime, cf.ClinFormId,
  RANK() OVER (Partition by ce.PersonId ORDER BY cf.FormComplete, ce.EventNum DESC) AS rnk
  FROM dbo.ClinForm cf
  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId
  WHERE mf.FormName = 'LMG' AND cf.FormComplete > 0 AND cf.DeletedAt IS NULL
  AND DATEDIFF( MM, ce.EventTime, GETDATE() ) < 6
) agg
WHERE agg.rnk = 1
