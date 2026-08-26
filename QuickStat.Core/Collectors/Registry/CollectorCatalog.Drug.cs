using QuickStat.Collectors.Sql;

namespace QuickStat.Collectors.Registry;

/// <content>
/// <c>AddCollectorsDrug</c> - 36 collectors, called from inside the
/// <c>GBD|LANGTID|KORTTID</c> block only (<c>QuickStat.Collectors.pas:313-385</c>).
/// </content>
/// <remarks>
/// <para>
/// The Delphi procedure also registers 22 datapoint classes with the datapoint factory before it
/// creates any collector. Those keys embed the raw <c>%</c> and <c>[]</c> of the ATC patterns
/// (<c>ATC_A10%</c>, <c>ATC_C0[23789]%</c> and so on) while the emitted column names are
/// <c>ATC_A10.F</c>, <c>ATC_A10.B</c>, … so none of them can ever match. They are effectively dead
/// registrations, they belong to the datapoint factory rather than to the collector registry, and
/// step 2.5 owns that factory - so they are not ported here
/// (<c>Docs/Port/03-collectors.md</c> §A.8).
/// </para>
/// </remarks>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateDrugCollectors() =>
    [
        // Simple ATC-pattern collectors, TDrugCollector.CreateChecksum.
        Make.Drug(CollectorTitles.DrugA10, AtcPatterns.A10),
        Make.Drug(CollectorTitles.DrugA10Ba02, AtcPatterns.A10Ba02),
        Make.Drug(CollectorTitles.DrugA11Ea, AtcPatterns.A11Ea),
        Make.Drug(CollectorTitles.DrugB01Aa03, AtcPatterns.B01Aa03),
        Make.Drug(CollectorTitles.DrugB01Af, AtcPatterns.B01Af),
        Make.Drug(CollectorTitles.DrugB03Ba, AtcPatterns.B03Ba),
        Make.Drug(CollectorTitles.DrugB03Ba01, AtcPatterns.B03Ba01),
        Make.Drug(CollectorTitles.DrugB03Ba03, AtcPatterns.B03Ba03),
        Make.Drug(CollectorTitles.DrugC01A, AtcPatterns.C01A),
        Make.Drug(CollectorTitles.DrugC02, AtcPatterns.C02),
        Make.Drug(CollectorTitles.DrugC03, AtcPatterns.C03),
        Make.Drug(CollectorTitles.DrugC07, AtcPatterns.C07),
        Make.Drug(CollectorTitles.DrugC08, AtcPatterns.C08),
        Make.Drug(CollectorTitles.DrugC08D, AtcPatterns.C08D),
        Make.Drug(CollectorTitles.DrugC09, AtcPatterns.C09),
        Make.Drug(CollectorTitles.DrugC0X23789, AtcPatterns.C0X23789),
        Make.Drug(CollectorTitles.DrugM01A, AtcPatterns.M01A),
        Make.Drug(CollectorTitles.DrugN04Ba, AtcPatterns.N04Ba),

        // TDrugCollector.CreateForTreatType( …, ttAnyTreatType, … ). The Delphi comment calls these
        // "Collectors fast", but ttAnyTreatType adds no TreatType clause, so they are identical in
        // behaviour to the eighteen above.
        Make.Drug(CollectorTitles.DrugN02A, AtcPatterns.N02A),
        Make.Drug(CollectorTitles.DrugN02B, AtcPatterns.N02B),
        Make.Drug(CollectorTitles.DrugN05A, AtcPatterns.N05A),
        Make.Drug(CollectorTitles.DrugN05B, AtcPatterns.N05B),
        Make.Drug(CollectorTitles.DrugN05C, AtcPatterns.N05C),
        Make.Drug(CollectorTitles.DrugN06A, AtcPatterns.N06A),
        Make.Drug(CollectorTitles.DrugN06D, AtcPatterns.N06D),

        // Drug-drug interactions.
        Make.DrugInteraction(
            CollectorNames.DruidCount,
            CollectorTitles.DruidCountPerLevel,
            CollectorNames.DruidPrefix,
            QaSql.DruidCountByLevel()),
        Make.DrugInteraction(
            CollectorNames.DruidSpecific,
            CollectorTitles.DruidSpecific,
            string.Empty,
            QaSql.DruidIndividualInteractions(5)),

        // Drug collectors of various sorts.
        Make.DrugSetCollector(
            CollectorNames.DrugCountGroup,
            CollectorTitles.DrugCountGroup,
            CollectorNames.DrugCountVariablePrefix,
            DrugSql.DrugCountByAtcGroup),
        Make.DrugSetCollector(
            CollectorNames.DrugCountNoAtc,
            CollectorTitles.DrugCountNoAtc,
            CollectorNames.DrugCountVariablePrefix,
            QaSql.DrugCountNoAtc()),
        Make.DrugSetCollector(
            CollectorNames.DrugCount,
            CollectorTitles.DrugCountTreatType,
            CollectorNames.DrugTreatVariablePrefix,
            DrugSql.DrugCountByType),
        Make.DrugSetCollector(
            CollectorNames.DrugMetformin,
            CollectorTitles.DrugMetformin,
            CollectorNames.DrugSetVariablePrefix,
            DrugSql.Metformin),
        Make.DrugSetCollector(
            CollectorNames.DrugAnticholinergicN05,
            CollectorTitles.DrugAnticholinergicN05,
            CollectorNames.DrugSetVariablePrefix,
            DrugSql.AnticholinergicsN05A),
        Make.DrugSetCollector(
            CollectorNames.DrugAnticholinergicAb,
            CollectorTitles.DrugAnticholinergicAb,
            CollectorNames.DrugSetVariablePrefix,
            DrugSql.AnticholinergicsAb),

        // PORT-PLAN.md §8.4: the caption is the shipping lineage's "Antibiotika: Resistendrivende"
        // and the ATC set has no J01FF%. The three antibiotic collectors that follow are Phase 4's,
        // registered in the order the commented-out lines sat in
        // (QuickStat.Collectors.pas:381-383) - position is the column order of every export.
        // Exactly one of them, QS_DRUG_ANTIBIOTIC_INTERMEDIATE, needs CollectorAvailability for
        // KB.AntibioticResistance2 (R7): it is the only one that joins the table
        // (EPR.QA.SQL.pas:453).
        Make.DrugSetCollector(
            CollectorNames.DrugAntibioticResistance,
            CollectorTitles.DrugAntibioticResistance,
            CollectorNames.DrugSetVariablePrefix,
            DrugSql.DrugSetAntibioticResistance()),
        Make.DrugSetCollector(
            CollectorNames.DrugAntibioticIntermediate,
            CollectorTitles.DrugAntibioticIntermediate,
            CollectorNames.DrugSetVariablePrefix,
            DrugSql.DrugSetAntibioticIntermediate(),
            CollectorAvailability.RequiringDatabaseObject(DrugSql.AntibioticResistanceKnowledgeBase)),

        Make.DrugSetCollector(
            CollectorNames.DrugNorGeP,
            CollectorTitles.DrugNorGeP,
            CollectorNames.NorGePVariablePrefix,
            DrugSql.NorGeP),
    ];
}
