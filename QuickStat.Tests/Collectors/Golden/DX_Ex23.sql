SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption FROM (
SELECT cp.PersonId, 'Ex23' AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption,
RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo
FROM dbo.ClinProblem cp
JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1
JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem
JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId
WHERE ( mni.ItemCode LIKE 'E[23]%' ) ) agg WHERE OrderNo = 1
