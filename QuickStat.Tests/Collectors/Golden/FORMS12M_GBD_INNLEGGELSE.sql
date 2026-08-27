SELECT ce.PersonId, UPPER(mf.FormName) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId
FROM dbo.ClinForm cf
JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName='GBD_INNLEGGELSE'
WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < 12 ) AND ( cf.DeletedAt IS NULL )
GROUP BY ce.PersonId, mf.FormName
