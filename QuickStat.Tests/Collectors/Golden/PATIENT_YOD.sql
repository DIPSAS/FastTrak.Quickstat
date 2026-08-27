SELECT PersonId,'YOD' AS VarName, DATEPART(YYYY,DeceasedDate) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
