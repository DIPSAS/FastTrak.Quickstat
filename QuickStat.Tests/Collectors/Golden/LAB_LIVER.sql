SELECT agg.* FROM
(
  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy
  FROM dbo.LabData ld
  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
  WHERE ( ld.PersonId IN (/*PIDS*/) ) AND ( la.LabClassId IN (123, 124, 125, 126, 127, 128, 129, 139) AND ( ld.NumResult >= 0 ) )
) agg
WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName
