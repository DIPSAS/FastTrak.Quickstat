SELECT PersonId,'LABCOUNT3M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId
FROM LabData
WHERE DATEDIFF(MM,LabDate,GETDATE()) < 3
GROUP BY PersonId
