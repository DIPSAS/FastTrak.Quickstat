SELECT PersonId,'YOB' AS VarName, DATEPART(YYYY,DOB) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
