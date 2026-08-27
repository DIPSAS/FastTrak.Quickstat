SELECT PersonId,'SEX' AS VarName, GenderId AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))
