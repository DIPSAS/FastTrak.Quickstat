SELECT PersonId, 'DEATH_CENTER' AS VarName, CenterId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId
FROM
(
  SELECT p.PersonId, p.DeceasedDate, sg.CenterId, scl.StudCaseLogId,
  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder
  FROM dbo.Person p
  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = 42
  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate
  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId
  WHERE NOT DeceasedDate IS NULL
) agg
WHERE agg.ReverseOrder = 1
