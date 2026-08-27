SELECT PersonId,'AGE' AS VarName, DATEDIFF(YYYY,DOB,GETDATE()) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
