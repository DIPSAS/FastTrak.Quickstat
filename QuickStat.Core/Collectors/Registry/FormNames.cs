namespace QuickStat.Collectors.Registry;

/// <summary>
/// The GBD form-class names the registry hard-codes, from the <c>{$REGION 'GBD Collectors'}</c>
/// block of <c>EPR.QA.Collector.Names.pas</c>.
/// </summary>
/// <remarks>
/// Only the names reachable from a registered collector are here. <c>FORM_NAME_MAREVAN</c>,
/// <c>FORM_NAME_FLACKER_KIELY</c>, <c>FORM_NAME_NEWS2</c> and <c>FORM_NAME_MATKORT</c> feed the
/// <c>FORMAGE.*</c> collectors, which QuickStat never registers and which PORT-PLAN.md §7.1 drops.
/// </remarks>
public static class FormNames
{
    /// <summary><c>FORM_NAME_BARTHEL</c>.</summary>
    public const string Barthel = "BARTHEL";

    /// <summary><c>FORM_NAME_KDV</c>.</summary>
    public const string Kdv = "KDV";

    /// <summary><c>FORM_NAME_HULTEN</c>.</summary>
    public const string Hulten = "HULTEN";

    /// <summary><c>FORM_NAME_LMG</c>.</summary>
    public const string Lmg = "LMG";

    /// <summary><c>FORM_NAME_MNA</c>.</summary>
    public const string Mna = "MNA";

    /// <summary><c>FORM_NAME_QUALID</c>.</summary>
    public const string Qualid = "QUALID";

    /// <summary><c>FORM_NAME_STRATIFY</c>.</summary>
    public const string Stratify = "STRATIFY";

    /// <summary><c>FORM_NAME_BESLUTNINGER</c> (and its twin <c>FORM_NAME_GBD_BESLUTNIGER</c>).</summary>
    public const string Beslutninger = "GBD_BESLUTNINGER";

    /// <summary><c>FORM_NAME_GBD_INNLEGGELSE</c>.</summary>
    public const string GbdInnleggelse = "GBD_INNLEGGELSE";
}
