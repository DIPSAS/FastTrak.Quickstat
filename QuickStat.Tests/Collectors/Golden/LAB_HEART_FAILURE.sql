SELECT agg.* FROM
(
  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy
  FROM dbo.LabData ld
  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
  WHERE ( ld.PersonId IN (/*PIDS*/) ) AND ( la.LabClassId IN (6, 22, 49, 50, 51, 52, 53, 90, 91, 124, 140, 171, 575, 995, 1075) AND ( ld.NumResult >= 0 ) )
) agg
WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName
