SELECT PersonId,'MOB' AS VarName, DATEPART(MM,DOB) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
