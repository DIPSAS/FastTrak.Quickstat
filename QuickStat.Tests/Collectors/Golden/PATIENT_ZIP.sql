SELECT PersonId,'ZIP' AS VarName, CONVERT(INTEGER,PostalCode) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
