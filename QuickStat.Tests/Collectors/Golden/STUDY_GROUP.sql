SELECT sc.PersonId, 'GroupId' AS VarName, sc.GroupId AS DpValue, GETDATE(), sc.StudCaseId AS RowId
FROM dbo.StudCase sc
WHERE sc.StudyId = 42
