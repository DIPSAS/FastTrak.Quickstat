SELECT a.* FROM
(
   SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName( la.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
   RANK() OVER ( PARTITION BY PersonId ORDER BY LabDate DESC ) AS OrderBy
   FROM dbo.LabData ld
     JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
     JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
   WHERE ( la.TrustLevel = 1 )  AND ( ld.PersonId IN (/*PIDS*/) )
) a
WHERE a.OrderBy = 1
