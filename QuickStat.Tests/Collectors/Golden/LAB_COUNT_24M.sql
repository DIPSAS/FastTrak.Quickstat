SELECT PersonId,'LABCOUNT24M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId
FROM LabData
WHERE DATEDIFF(MM,LabDate,GETDATE()) < 24
GROUP BY PersonId
