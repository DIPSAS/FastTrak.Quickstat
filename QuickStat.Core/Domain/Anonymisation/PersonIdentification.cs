namespace QuickStat.Domain.Anonymisation;

/// <summary>
/// How much of a patient's identity leaves the application - on screen and in the file alike.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPersonIdentification</c> (<c>EPR.QA.Matrix.pas:26</c>), chosen by three radio
/// buttons under <c>Export options</c>. <see cref="PersonIdOnly"/> is the default.
/// </para>
/// <para>
/// The two non-full modes <b>omit</b> the date-of-birth, national-id and name columns
/// <em>entirely</em>: no field and no separator, so header and data rows stay aligned
/// (<c>EPR.QA.Matrix.pas:469-470</c> is an empty <c>then</c> branch). They are not blanked and they
/// are not written empty. Derive the column set through
/// <see cref="IdentificationColumns.For"/> and never re-derive it by hand.
/// </para>
/// </remarks>
public enum PersonIdentification
{
    /// <summary>
    /// All four identity columns. Delphi <c>pgiFull</c>, radio <c>Fully identified patients</c>.
    /// </summary>
    /// <remarks>
    /// Produces no national ids in <em>this</em> repository, because the call that fetches them is
    /// commented out (<c>MainQuickStat.pas:537-539</c>). Phase 4 restores it.
    /// </remarks>
    Full = 0,

    /// <summary>
    /// Real person id, nothing else. Delphi <c>pgiPersonIdOnly</c>, radio
    /// <c>Identified with PID only</c>. The default.
    /// </summary>
    PersonIdOnly = 1,

    /// <summary>
    /// Pseudonymous person id, nothing else. Delphi <c>pgiRandomPersonId</c>, radio
    /// <c>Generate new random PIDs</c>.
    /// </summary>
    /// <remarks>
    /// The pseudonym is the one field the Delphi writes <b>unquoted</b> in the CSV, because it goes
    /// through <c>Write(F, &lt;integer&gt;)</c> rather than <c>AnsiQuotedStr</c>. Byte parity
    /// depends on reproducing that.
    /// </remarks>
    RandomPersonId = 2,
}
