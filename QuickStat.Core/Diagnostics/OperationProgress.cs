namespace QuickStat.Diagnostics;

/// <summary>
/// One progress report from a long-running operation: connect, load population, collect data.
/// </summary>
/// <param name="Header">
/// The banner's fixed heading, e.g. <c>Progress</c>. Delphi: <c>IStatus.SetHeader</c>.
/// </param>
/// <param name="Info">
/// The line that changes, e.g. <c>Connecting to Testdatabase (NDV) ...</c>. Delphi:
/// <c>IStatus.SetInfo</c>, whose idle text is <c>Program is idle</c>.
/// </param>
/// <param name="Percent">
/// Completion 0-100, or <see langword="null"/> for indeterminate.
/// </param>
/// <remarks>
/// <para>
/// Replaces <c>IStatus</c> / <c>IProgress</c> (<c>Emetra.Progress.Interfaces.pas:10-28</c>), which
/// the main form implemented directly. Reported through <see cref="IProgress{T}"/>, so an instance
/// constructed on the UI thread marshals back automatically and no view-model code needs a
/// dispatcher call.
/// </para>
/// <para>
/// Lives in <c>Diagnostics</c> - owned by step 2.7 - because steps 2.2 and 2.4 both report through
/// it and neither can own it. See <c>Docs/Port/06-contracts.md</c>.
/// </para>
/// </remarks>
public readonly record struct OperationProgress(string Header, string Info, double? Percent);
