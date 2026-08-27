SELECT ce.PersonId, UPPER('GBDLEGE') AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS MaxEventTime, MAX(cf.ClinFormId) AS MaxClinFormId
FROM dbo.ClinForm cf
JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName IN ( 'GBD_NOTAT_LEGE','GBD_STATUS_PRESENS','GBD_INFECTION','GBD_BESLUTNINGER' )
WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < 3 ) AND ( cf.DeletedAt IS NULL )
GROUP BY ce.PersonId
