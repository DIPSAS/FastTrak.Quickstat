namespace QuickStat.Collectors.Registry;

/// <summary>
/// The Norwegian strings the data-element list shows, transcribed character for character from the
/// <c>resourcestring</c> block of <c>EPR.QA.Collector.Names.pas</c>.
/// </summary>
/// <remarks>
/// <para>
/// Parity that must not drift (PORT-PLAN.md §6): titles are how users recognise rows, and
/// <see cref="ICollectorRegistry.TryFind"/> matches on the title as well as the name, so they must
/// stay unique. Several carry typos, a stray parenthesis, a doubled space or a trailing space.
/// <b>All of those are deliberate</b> - they are what the shipped build displays, and every one is
/// called out on the member concerned.
/// </para>
/// <para>
/// These are the <em>registered</em> titles. Four collector families add a suffix in their
/// constructor; that transformation lives in <see cref="CollectorTitle"/> and is applied once, by
/// the <see cref="Make"/> helpers, so it can never be double-applied or forgotten.
/// </para>
/// <para>
/// Plain constants rather than a <c>.resx</c>: the Delphi <c>resourcestring</c> mechanism was never
/// used for translation here, and a resource file would make the golden-file tests indirect for no
/// benefit (<c>Docs/Port/03-collectors.md</c> §G.3).
/// </para>
/// </remarks>
public static class CollectorTitles
{
    // ---- Demographics and study case ---------------------------------------------------------

    /// <summary><c>StrTitleDemographicsAge</c>.</summary>
    public const string DemographicsAge = "^ Alder";

    /// <summary><c>StrTitleDemographicsSex</c>.</summary>
    public const string DemographicsSex = "^ Kjønn";

    /// <summary><c>StrTitleDemographicsYob</c>.</summary>
    public const string DemographicsYearOfBirth = "^ Fødselsår";

    /// <summary><c>StrTitleDemographicsYod</c>.</summary>
    public const string DemographicsYearOfDeath = "^ Dødsår";

    /// <summary><c>StrTitleDemographicsMob</c>. The missing <c>s</c> in "Fødselmåned" is upstream.</summary>
    public const string DemographicsMonthOfBirth = "^ Fødselmåned";

    /// <summary><c>StrTitleDemographicsPostCode</c>.</summary>
    public const string DemographicsPostCode = "^ Postnummer";

    /// <summary><c>StrTitleStudyStatus</c>.</summary>
    public const string StudyStatus = "^ Statuskode";

    /// <summary><c>StrTitleStudyCenter</c>.</summary>
    public const string StudyCenter = "^ Institusjon / sted";

    /// <summary><c>StrTitleStudyGroup</c>.</summary>
    public const string StudyGroup = "^ Gruppe / avdeling nå";

    /// <summary><c>StrTitleStudyGroupDeath</c>.</summary>
    public const string StudyGroupAtDeath = "^ Gruppe / avdeling ved død";

    /// <summary><c>StrTitleStudyCenterDeath</c>.</summary>
    public const string StudyCenterAtDeath = "^ Institusjon / sted ved død";

    // ---- Forms -------------------------------------------------------------------------------

    /// <summary><c>StrTitleFormFrequencies</c>.</summary>
    public const string FormFrequencies = "Skjema: Antall totalt per type";

    /// <summary><c>StrTitleFormCount3m</c>.</summary>
    public const string FormCount3M = "Skjema: Antall siste 3 mnd per type";

    /// <summary><c>StrTitleFormCount6m</c>.</summary>
    public const string FormCount6M = "Skjema: Antall siste 6 mnd per type";

    /// <summary><c>StrTitleFormCount12m</c>.</summary>
    public const string FormCount12M = "Skjema: Antall siste 12 mnd per type";

    /// <summary><c>StrTitleFormCount24m</c>.</summary>
    public const string FormCount24M = "Skjema: Antall siste 24 mnd per type";

    /// <summary><c>StrTitleFormAgeTemplate</c>. Slots: form title, form name.</summary>
    public const string FormAgeTemplate = "Skjema-alder: {0} ({1})";

    /// <summary><c>StrTitleFormDataTemplate</c>. Slots: form title, form name.</summary>
    public const string FormDataTemplate = "Skjema-data: {0} ({1})";

    // ---- Lab group names (fed through CollectorTitle.ForLabSet) -------------------------------

    /// <summary><c>StrTitleLabsetKidney</c>.</summary>
    public const string LabSetKidney = "Nyrefunksjon";

    /// <summary><c>StrTitleLabsetAnemia</c>.</summary>
    public const string LabSetAnemia = "Anemi";

    /// <summary><c>StrTitleLabsetLipids</c>.</summary>
    public const string LabSetLipids = "Lipider";

    /// <summary><c>StrTitleLabsetDigitalis</c>.</summary>
    public const string LabSetDigitalis = "Digitalis";

    /// <summary><c>StrTitleLabsetLiver</c>.</summary>
    public const string LabSetLiver = "Leverstatus";

    /// <summary><c>StrTitleLabsetThyroid</c>.</summary>
    public const string LabSetThyroid = "Tyreoidea";

    /// <summary><c>StrTitleLabsetGlucose</c>.</summary>
    public const string LabSetGlucose = "Glukose";

    /// <summary><c>StrTitleLabsetInr</c>.</summary>
    public const string LabSetInr = "INR fra labarket";

    /// <summary><c>StrTitleLabsetHyperparatyreoidism</c>.</summary>
    public const string LabSetHyperPara = "Hyperparatyreoidisme";

    /// <summary><c>StrTitleLabsetHeartFailure</c>.</summary>
    public const string LabSetHeartFailure = "Hjertesviktrelaterte labdata";

    /// <summary><c>StrTitleLabsetCrp</c>.</summary>
    public const string LabSetCrp = "CRP";

    /// <summary><c>StrTitleVarsetDiabetes</c> - the lab-set collector reuses this var-set string.</summary>
    public const string LabSetDiabetes = "Diabetes";

    /// <summary>
    /// <c>StrTitleLabsetGeriatry</c>. Contains a colon, so <see cref="CollectorTitle.ForLabSet"/>
    /// leaves it alone - which is why it already carries its own <c>(siste)</c>.
    /// </summary>
    public const string LabSetGeriatric = "GBD: Sentrale labdata (siste)";

    // ---- Lab, verbatim titles -----------------------------------------------------------------

    /// <summary><c>StrTitleLabsetHigh</c>.</summary>
    public const string LabHighTrust = "Labdata: Alle med høy konfidens";

    /// <summary><c>StrTitleLabsetMedium</c>.</summary>
    public const string LabMediumTrust = "Labdata: Alle med middels konfidens";

    /// <summary><c>StrTitleLabsetLow</c>.</summary>
    public const string LabLowTrust = "Labdata: Alle med lav konfidens";

    /// <summary><c>StrTitleLabCount3m</c>.</summary>
    public const string LabCount3M = "Labdata: Antall prøver siste 3 mnd";

    /// <summary><c>StrTitleLabCount6m</c>.</summary>
    public const string LabCount6M = "Labdata: Antall prøver siste 6 mnd";

    /// <summary><c>StrTitleLabCount12m</c>.</summary>
    public const string LabCount12M = "Labdata: Antall prøver siste 12 mnd";

    /// <summary><c>StrTitleLabCount24m</c>.</summary>
    public const string LabCount24M = "Labdata: Antall prøver siste 24 mnd (2 år)";

    /// <summary><c>StrTitleLabCount60m</c>.</summary>
    public const string LabCount60M = "Labdata: Antall prøver siste 60 mnd (5 år)";

    // ---- Anthropometry -------------------------------------------------------------------------

    /// <summary><c>StrTitleAntropometrics</c>.</summary>
    public const string Anthropometrics = "Antropometri: Høyde og vekt";

    // ---- GBD ------------------------------------------------------------------------------------

    /// <summary><c>StrTitleVarSetAgeWeightDays</c>.</summary>
    public const string GbdWeightDays = "GBD: Tid siden siste veiing";

    /// <summary><c>StrTitleGbdTvangsvedtak</c>.</summary>
    public const string GbdTvangsvedtak = "GBD: Aktivt tvangsvedtak";

    /// <summary><c>StrTitleGbdInnleggelser12m</c>.</summary>
    public const string GbdAdmissions12M = "GBD: Innleggelser siste 12 mnd";

    /// <summary><c>StrTitleGbdFormLege3m</c>.</summary>
    public const string GbdDoctorNotes3M = "GBD: Legenotater siste 3 mnd";

    /// <summary><c>StrTitleVarsetGbdScores</c>.</summary>
    public const string GbdScores = "GBD: Viktigste scores";

    /// <summary><c>StrTitleVarsetGbdBloodPressure</c>.</summary>
    public const string GbdBloodPressure = "GBD: Blodtrykk fra kurve";

    /// <summary><c>StrTitleVarSetPrimaryContactDays</c>.</summary>
    public const string GbdPrimaryContact = "GBD: Primærkontakt registrert";

    /// <summary><c>StrTitleGbdWeight2m</c>.</summary>
    public const string GbdWeight2M = "GBD: Vekt fra siste 2 mnd";

    /// <summary><c>StrTitleGbdSbp2m</c>.</summary>
    public const string GbdSystolic2M = "GBD: Blodtrykk fra siste 2 mnd";

    /// <summary><c>StrTitleGbdFlacker12m</c>.</summary>
    public const string GbdFlacker12M = "GBD: Flacker-Kiely siste 12 mnd";

    /// <summary><c>StrTitleGbdFlackerDeath</c>.</summary>
    public const string GbdFlackerDeath = "GBD: Flacker-Kiely og levedager";

    /// <summary><c>StrTitleGbdHulten3m</c>. The missing accent in "Hulten" is upstream.</summary>
    public const string GbdHulten3M = "GBD: Hulten siste 3 mnd";

    /// <summary><c>StrTitleGbdQualid6m</c>.</summary>
    public const string GbdQualid6M = "GBD: Qualid siste 6 mnd";

    /// <summary><c>StrTitleGbdKdv6m</c>.</summary>
    public const string GbdKdv6M = "GBD: KDV siste 6 mnd";

    /// <summary><c>StrTitleGbdBarthel6m</c>.</summary>
    public const string GbdBarthel6M = "GBD: Barthel ADL-Indeks siste 6 mnd";

    /// <summary><c>StrTitleGbdStratify6m</c>.</summary>
    public const string GbdStratify6M = "GBD: Stratify fallrisiko siste 6 mnd";

    /// <summary><c>StrTitleGbdMna6m</c>.</summary>
    public const string GbdMna6M = "GBD: MNA ernæringsvurdering siste 6 mnd";

    /// <summary><c>StrTitleGbdAntiHypertensivesLowBp</c>.</summary>
    public const string GbdAntiHypertensivesLowBp = "GBD: Blodtrykk < 120 og blodtrykksbehandling";

    /// <summary>
    /// <c>StrTitleGbdLowBp</c>. Carries its own <c>(siste)</c>: the collector is a
    /// <c>TCustomDataCollector</c>, so nothing appends one.
    /// </summary>
    public const string GbdLowBp = "GBD: Blodtrykk < 120 (siste)";

    /// <summary><c>StrTitleGbdAceLowGFR</c>.</summary>
    /// <remarks>
    /// <b><c>GFR</c>, not <c>eGFR</c></b>. PORT-PLAN.md §8.5: <c>eGFR</c> is mainline-only wording
    /// that never shipped, and <c>GFR</c> is what all nine candidate baselines carry (9 of 9).
    /// <c>eGFR</c> is the better clinical term - raise it as an improvement, do not apply it
    /// silently.
    /// </remarks>
    public const string GbdAceLowGfr = "GBD: ACE/A2 og GFR < 35";

    /// <summary>
    /// <c>StrTitleGbdMetforminLowGFR</c>. Note the <b>trailing space</b>, and <c>GFR</c> rather than
    /// <c>eGFR</c> for the same reason as <see cref="GbdAceLowGfr"/>.
    /// </summary>
    public const string GbdMetforminLowGfr = "GBD: Metformin og GFR < 50 ";

    /// <summary><c>StrTitleGbdLmg6m</c>.</summary>
    public const string GbdLmg6M = "GBD: Skjema \"Legemiddelgjennomgang\" siste 6 mnd (kompletthet)";

    /// <summary><c>StrTitleGbdBeslutninger6m</c>.</summary>
    public const string GbdBeslutninger6M = "GBD: Skjema \"Beslutninger\" siste 6 mnd (kompletthet)";

    // ---- Diagnosis --------------------------------------------------------------------------------

    /// <summary><c>StrTitleDiagnoseAll1</c>.</summary>
    public const string DiagnoseAll1 = "Diagnoser: Spesifisert med 1 tegn";

    /// <summary><c>StrTitleDiagnoseAll2</c>.</summary>
    public const string DiagnoseAll2 = "Diagnoser: Spesifisert med 2 tegn";

    /// <summary><c>StrTitleDiagnoseAll3</c>.</summary>
    public const string DiagnoseAll3 = "Diagnoser: Spesifisert med 3 tegn";

    /// <summary><c>StrTitleDiagnoseAll4</c>.</summary>
    public const string DiagnoseAll4 = "Diagnoser: Spesifisert med 4 tegn";

    /// <summary><c>StrTitleDiagnoseAll5</c>.</summary>
    public const string DiagnoseAll5 = "Diagnoser: Spesifisert med 5 tegn";

    /// <summary><c>StrTitleDiagnoseMissingE11</c>. Singular "Diagnose", unlike its neighbours.</summary>
    public const string DiagnoseMissingE11 = "Diagnose: Antidiabetika uten diabetesdiagnose";

    /// <summary><c>StrTitleDiagnoseCancer</c>.</summary>
    public const string DiagnoseCancer = "Diagnoser: C - Kreft";

    /// <summary><c>StrTitleDiagnoseThyroid</c>.</summary>
    public const string DiagnoseThyroid = "Diagnoser: E0 - Tyreoidea-sykdommer";

    /// <summary><c>StrTitleDiagnoseDiabetes</c>. Note the <b>trailing space</b>.</summary>
    public const string DiagnoseDiabetes = "Diagnoser: E1[014] - Diabetes Mellitus ";

    /// <summary><c>StrTitleDiagnoseEndocrine</c>. Note the <b>stray closing parenthesis</b>.</summary>
    public const string DiagnoseEndocrine = "Diagnoser: E[23] - Andre endokrine lidelser )";

    /// <summary><c>StrTitleDiagnoseMetabolic</c>.</summary>
    public const string DiagnoseMetabolic = "Diagnoser: E[789] - Metabolske forstyrrelser";

    /// <summary><c>StrTitleDiagnosePsychiatry</c>. Note the <b>doubled space</b> before the dash.</summary>
    public const string DiagnosePsychiatry = "Diagnoser: F[123456789]  - Psykisk lidelser";

    /// <summary><c>StrTitleDiagnoseHypertension</c>.</summary>
    public const string DiagnoseHypertension = "Diagnoser: I1[012345] - Hypertensjon";

    /// <summary><c>StrTitleDiagnoseIschemia</c>.</summary>
    public const string DiagnoseIschemia = "Diagnoser: I2[012345] - Iskemisk hjertesykdom";

    /// <summary><c>StrTitleDiagnoseAtrialFibrillation</c>.</summary>
    public const string DiagnoseAtrialFibrillation = "Diagnoser: I48 - Atrieflimmer/flutter";

    /// <summary><c>StrTitleDiagnoseStroke</c>. Note the <b>unbalanced bracket</b>.</summary>
    public const string DiagnoseStroke = "Diagnoser: I6[01234 - Hjerneslag";

    /// <summary>
    /// <c>StrTitleDiagnoseDementia</c>. The title says <c>F0[123]+G03</c> while the SQL matches
    /// <c>F0[0123]</c> and <c>G30</c>; the SQL is right and the title is what users see.
    /// </summary>
    public const string DiagnoseDementia = "Diagnoser: F0[123]+G03 - Demens + Alzheimer";

    // ---- Drug -------------------------------------------------------------------------------------

    /// <summary><c>TXT_DRUG_A10</c>.</summary>
    public const string DrugA10 = "Medisiner: A10 - Antidiabetika";

    /// <summary><c>TXT_DRUG_A10BA02</c>.</summary>
    public const string DrugA10Ba02 = "Medisiner: A10BA02 - Metformin alene";

    /// <summary><c>TXT_DRUG_A11EA</c>.</summary>
    public const string DrugA11Ea = "Medisiner: A11EA - Vitamin B-kompleks";

    /// <summary><c>TXT_DRUG_B01AA03</c>.</summary>
    public const string DrugB01Aa03 = "Medisiner: B01AA03 - Warfarin";

    /// <summary>
    /// <c>TXT_DRUG_B01AF</c>. Note the <b>letter O</b> where the ATC code has a zero: the ATC
    /// pattern is <c>B01AF%</c> but the title reads <c>BO1AF</c>.
    /// </summary>
    public const string DrugB01Af = "Medisiner: BO1AF - DOAK";

    /// <summary><c>TXT_DRUG_B03BA</c>.</summary>
    public const string DrugB03Ba = "Medisiner: B03BA - Vitamin B12";

    /// <summary><c>TXT_DRUG_B03BA01</c>. "Cyanokoblamin" is upstream; the drug is cyanokobalamin.</summary>
    public const string DrugB03Ba01 = "Medisiner: B03BA01 - Cyanokoblamin";

    /// <summary><c>TXT_DRUG_B03BA03</c>.</summary>
    public const string DrugB03Ba03 = "Medisiner: B03BA03 - Hydroksykobalamin";

    /// <summary><c>TXT_DRUG_C01A</c>.</summary>
    public const string DrugC01A = "Medisiner: C01A - Hjerteglykosider";

    /// <summary><c>TXT_DRUG_C02</c>.</summary>
    public const string DrugC02 = "Medisiner: C02 - Antihypertensiva";

    /// <summary><c>TXT_DRUG_C03</c>.</summary>
    public const string DrugC03 = "Medisiner: C03 - Diuretika";

    /// <summary><c>TXT_DRUG_C07</c>.</summary>
    public const string DrugC07 = "Medisiner: C07 - Betablokkere";

    /// <summary><c>TXT_DRUG_C08</c>.</summary>
    public const string DrugC08 = "Medisiner: C08 - Kalsiumkanalblokkere/CCB";

    /// <summary><c>TXT_DRUG_C08D</c>.</summary>
    public const string DrugC08D = "Medisiner: C08D - CCB med kardiale effekter";

    /// <summary><c>TXT_DRUG_C09</c>.</summary>
    public const string DrugC09 = "Medisiner: C09 - Renin/Angiotensin systemet";

    /// <summary><c>TXT_DRUG_C0x23789</c>.</summary>
    public const string DrugC0X23789 = "Medisiner: C0[23789] - Antihypertensiva vidt definert";

    /// <summary><c>TXT_DRUG_M01A</c>.</summary>
    public const string DrugM01A = "Medisiner: M01A - NSAID";

    /// <summary><c>TXT_DRUG_N04BA</c>.</summary>
    public const string DrugN04Ba = "Medisiner: N04BA - Antiparkinsonmidler";

    /// <summary><c>TXT_DRUG_N02A</c>.</summary>
    public const string DrugN02A = "Medisiner: N02A - Opioider";

    /// <summary><c>TXT_DRUG_N02B</c>.</summary>
    public const string DrugN02B = "Medisiner: N02B - Analgetika/antipyretika";

    /// <summary><c>TXT_DRUG_N05A</c>.</summary>
    public const string DrugN05A = "Medisiner: N05A - Antipsykotika";

    /// <summary><c>TXT_DRUG_N05B</c>.</summary>
    public const string DrugN05B = "Medisiner: N05B - Anxiolytika";

    /// <summary><c>TXT_DRUG_N05C</c>.</summary>
    public const string DrugN05C = "Medisiner: N05C - Hypnotika/sedativa";

    /// <summary><c>TXT_DRUG_N06A</c>.</summary>
    public const string DrugN06A = "Medisiner: N06A - Antidepressiva";

    /// <summary><c>TXT_DRUG_N06D</c>.</summary>
    public const string DrugN06D = "Medisiner: N06D - Antidemensmidler";

    /// <summary><c>StrTitleDruidCountPerLevel</c>.</summary>
    public const string DruidCountPerLevel = "Interaksjoner: Antall per nivå";

    /// <summary><c>StrTitleDruidSpecific</c>.</summary>
    public const string DruidSpecific = "Interaksjoner: Spesifisert i detalj";

    /// <summary><c>StrTitleDrugCountGroup</c>.</summary>
    public const string DrugCountGroup = "Medisin: Antall på utvalgte ATC-grupper";

    /// <summary><c>StrTitleDrugCountNoAtc</c>.</summary>
    public const string DrugCountNoAtc = "Medisin: Antall uten ATC-kode";

    /// <summary><c>StrTitleDrugCountTreatType</c>.</summary>
    public const string DrugCountTreatType = "Medisin: Antall per behandlingstype";

    /// <summary><c>StrTitleDrugMetformin</c>.</summary>
    public const string DrugMetformin = "Medisin: Metformin inkl. kombinasjoner";

    /// <summary><c>StrTitleDrugStrongAnticholinergicsN05A</c>.</summary>
    public const string DrugAnticholinergicN05 = "Medisin: N05A - Nevroleptika med sterk antikolinerg effekt (AB)";

    /// <summary><c>StrTitleDrugStrongAnticholinergics</c>.</summary>
    public const string DrugAnticholinergicAb = "Medisin: Sterke antikolinergika (AB)";

    /// <summary><c>StrTitleDrugAntibioticResistance</c>.</summary>
    /// <remarks>
    /// PORT-PLAN.md §8.4: the shipping lineage renamed this from
    /// <c>Medisin: Resistensdrivende antibiotika</c>, and the port takes the new caption together
    /// with dropping <c>J01FF%</c> from
    /// <see cref="QuickStat.Collectors.Sql.DrugSql.ResistanceDrivingAtcPatterns"/> - the two are one
    /// decision. The missing <c>s</c> in "Resistendrivende" is upstream and is preserved.
    /// </remarks>
    public const string DrugAntibioticResistance = "Antibiotika: Resistendrivende";

    /// <summary>
    /// <c>StrTitleDrugAntibioticIntermediate</c> (<c>EPR.QA.Collector.Names.pas:190</c>).
    /// </summary>
    public const string DrugAntibioticIntermediate = "Antibiotika: Intermediære";

    /// <summary>
    /// <c>StrTitleDrugAntibioticRecommended</c> (<c>EPR.QA.Collector.Names.pas:191</c>).
    /// </summary>
    public const string DrugAntibioticRecommended = "Antibiotika: Anbefalte";

    /// <summary>
    /// <c>StrTitleDrugAntibioticMetenamine</c> (<c>EPR.QA.Collector.Names.pas:192</c>). The spaces
    /// around the slash are upstream.
    /// </summary>
    public const string DrugAntibioticMetenamine = "Antibiotika: Metenamin / Hiprex";

    /// <summary><c>StrTitleDrugNorGeP</c>.</summary>
    public const string DrugNorGeP = "Medisin: NorGeP avvik";

    // ---- NDV / diabetes -------------------------------------------------------------------------

    /// <summary><c>StrTitleVarsetNdvBasicData</c>.</summary>
    public const string NdvBasicData = "NDV: Basisdata";

    /// <summary><c>StrTitleVarsetNdvTreatment</c>.</summary>
    public const string DiabetesTreatment = "Diabetes: Behandling";

    /// <summary><c>StrTitleVarsetNdvCompilcations</c> (the Delphi identifier is itself misspelt).</summary>
    public const string DiabetesComplications = "Diabetes: Komplikasjoner";

    /// <summary><c>StrTitleVarsetInsulin</c>.</summary>
    public const string DiabetesInsulin = "Diabetes: Insulindosering";

    /// <summary><c>StrTitleVarsetHypoglycemia</c>.</summary>
    public const string DiabetesHypoglycemia = "Diabetes: Hypoglykemi";

    /// <summary><c>StrTitleVarsetNdvExercise</c>.</summary>
    public const string DiabetesExercise = "Diabetes: Mosjon";

    /// <summary><c>StrTitleVarsetNdvSocial</c>.</summary>
    public const string DiabetesSocial = "Diabetes: Sosialt";

    // ---- ROAS / GWAS / DOGFOOD -------------------------------------------------------------------
    // These five are string literals in EPR.QA.Collector.Factory.pas, not resourcestrings.

    /// <summary><c>EPR.QA.Collector.Factory.pas:311</c>.</summary>
    public const string RoasGwasBackground = "GWAS Bakgrunn";

    /// <summary><c>EPR.QA.Collector.Factory.pas:313</c>.</summary>
    public const string RoasGwasAutoAntibody = "GWAS Autoantistoffer";

    /// <summary>
    /// <c>EPR.QA.Collector.Factory.pas:315</c>. "spesfikk" is missing an <c>i</c>; upstream.
    /// </summary>
    public const string RoasGwasAps1 = "GWAS APS-I spesfikk";

    /// <summary><c>EPR.QA.Collector.Factory.pas:317</c>.</summary>
    public const string RoasPoiOrdinal = "POI Diagnoser";

    /// <summary><c>EPR.QA.Collector.Factory.pas:319</c>.</summary>
    public const string RoasPoiQuantity = "POI Diagnoseår";

    /// <summary>
    /// <c>EPR.QA.Collector.Factory.pas:321</c>. Registered <b>without</b> <c>' (siste)'</c>, and the
    /// <c>Autommunitet</c> misspelling is deliberate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Commit <c>8a9954c13</c> registered the literal <c>'Autommunitet (siste)'</c> and
    /// <c>08e35bd8d</c> had to strip the suffix back off, because <c>TVarSetCollector</c> appends it
    /// in its own constructor. <see cref="Make.VarSetNumeric"/> does the same, so the displayed
    /// title is <c>Autommunitet (siste)</c> with the suffix appearing exactly once - which is what
    /// <c>CollectorTitleTests</c> asserts.
    /// </para>
    /// <para>
    /// Correct Norwegian would be <c>Autoimmunitet</c>. Preserved because it is what users see on
    /// the shipping build and what a saved package title would match (PORT-PLAN.md §8.3).
    /// </para>
    /// </remarks>
    public const string RoasBase = "Autommunitet";

    /// <summary><c>EPR.QA.Collector.Factory.pas:323</c>.</summary>
    public const string DogfoodDatabaseVersion = "Dogfood: Databaseversjoner";
}
