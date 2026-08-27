SELECT PersonId,'LABCOUNT6M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId
FROM LabData
WHERE DATEDIFF(MM,LabDate,GETDATE()) < 6
GROUP BY PersonId
