SELECT PersonId, SUBSTRING(ItemCode,1,3) AS VarName, COUNT(*) AS DpValue, MIN(CreatedAt) AS MinCreatedAt, MIN(ProbId) AS MinProbId FROM
(
SELECT PersonId, mni.ItemCode, cp.ListItem, cp.CreatedAt, cp.ProbId
FROM dbo.ClinProblem cp
JOIN dbo.MetaProblemType mp ON mp.ProbType = cp.ProbType AND mp.ProbActive = 1
JOIN dbo.MetaNomListItem li ON li.ListItem = cp.ListItem
JOIN dbo.MetaNomItem mni ON mni.ItemId = li.ItemId ) pro GROUP BY PersonId, SUBSTRING(ItemCode,1,3)
