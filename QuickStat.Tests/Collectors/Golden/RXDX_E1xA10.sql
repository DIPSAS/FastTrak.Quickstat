SELECT rx.PersonId, 'A10_NOT_E1x01234' AS VarName, rxn AS DpValue, MaxCreatedAt, MaxTreatId FROM (
   SELECT PersonId, MAX(CreatedAt) AS MaxCreatedAt, MAX(TreatId) AS MaxTreatId, COUNT(*) AS rxn
   FROM dbo.OngoingTreatment
   WHERE ATC LIKE 'A10%'
   GROUP BY PersonId
) rx
LEFT JOIN
  (
    SELECT PersonId, COUNT(*) AS n FROM Diagnose.ICD10
    WHERE ItemCode LIKE 'E1[01234]%' AND ProbActive = 1
    GROUP BY PersonId
  ) agg ON agg.PersonId = rx.PersonId
WHERE ( agg.n IS NULL )ORDER BY PersonId
