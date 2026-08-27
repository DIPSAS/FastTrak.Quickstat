SELECT sc.PersonId, 'StatusId' AS VarName, sc.FinState AS DpValue, GETDATE(), sc.StudCaseId AS RowId
FROM dbo.StudCase sc
WHERE sc.StudyId = 42
