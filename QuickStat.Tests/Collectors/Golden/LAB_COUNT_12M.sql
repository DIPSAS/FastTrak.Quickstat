SELECT PersonId,'LABCOUNT12M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId
FROM LabData
WHERE DATEDIFF(MM,LabDate,GETDATE()) < 12
GROUP BY PersonId
