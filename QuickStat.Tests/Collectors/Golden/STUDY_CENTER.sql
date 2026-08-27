SELECT sc.PersonId, 'CenterId' AS VarName, sg.CenterId AS DpValue, GETDATE(), sc.StudCaseId AS RowId
FROM dbo.StudCase sc
JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = sc.GroupId
WHERE sc.StudyId = 42
