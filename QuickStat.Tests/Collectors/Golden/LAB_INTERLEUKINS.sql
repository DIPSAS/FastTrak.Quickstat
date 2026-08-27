SELECT agg.* FROM
(
  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy
  FROM dbo.LabData ld
  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
  WHERE ( ld.PersonId IN (/*PIDS*/) ) AND ( la.LabClassId IN (1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104) AND ( ld.NumResult >= 0 ) )
) agg
WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName
