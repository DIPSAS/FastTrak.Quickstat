using System.ComponentModel;
using QuickStat.Diagnostics;

namespace QuickStat.Services;

/// <summary>
/// The banner's Progress block, and the application-wide busy flag, as one observable service.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TfrmQuickStat</c> implemented <c>IStatus</c> and <c>IProgress</c> itself and handed
/// <c>Self</c> to every long-running object (<c>MainQuickStat.pas:65</c>). The port cannot do that,
/// because <see cref="QuickStat.ViewModels.MainViewModel"/> composes the tab view-models and several of those need to
/// report progress: constructor-injecting <see cref="QuickStat.ViewModels.MainViewModel"/> into its own children is a
/// dependency cycle the container refuses to resolve. So the state lives here, in a singleton that
/// everyone can take, and the view-model observes it.
/// </para>
/// <para>
/// Registered as <see cref="IShellProgress"/> <em>and</em> as
/// <see cref="IProgress{T}"/> of <see cref="OperationProgress"/>, so a wave-2 step can inject
/// whichever it needs; both resolve to the same instance.
/// </para>
/// <para>
/// <c>Report</c> is safe to call from any thread - it marshals to the user interface itself,
/// which is what <see cref="IProgress{T}"/> callers are entitled to assume.
/// </para>
/// </remarks>
public interface IShellProgress : IProgress<OperationProgress>, INotifyPropertyChanged
{
    /// <summary>The heading above the progress line. Always <c>Progress</c> in practice.</summary>
    /// <remarks>
    /// <c>05-ui-spec.md</c> §G.6: <c>IStatus.SetHeader</c> exists and nothing calls it, so treat this
    /// as a static label. <c>Report</c> only overwrites it when the report carries a
    /// non-empty header, so a caller that leaves <see cref="OperationProgress.Header"/> blank cannot
    /// blank the label by accident.
    /// </remarks>
    string Header { get; }

    /// <summary>The line that changes. Starts at <c>Program is idle</c>.</summary>
    string Info { get; }

    /// <summary>Completion, 0 to 100.</summary>
    double Percent { get; }

    /// <summary>Whether <see cref="Info"/> should be shown as an error.</summary>
    /// <remarks>
    /// §G.2: any exception while building the data hint turns the status label red and shows the
    /// message. Cleared by the next successful <c>Report</c>, <see cref="SetInfo"/> or
    /// <see cref="Done"/>.
    /// </remarks>
    bool IsError { get; }

    /// <summary>Whether a long-running operation is in flight.</summary>
    /// <remarks>
    /// Replaces <c>Screen.Cursor := crSqlWait</c> (§G.3). It drives the wait cursor, the busy
    /// overlay and command gating, so nested operations must not clear it early - use
    /// <see cref="BeginOperation(string)"/> rather than assigning.
    /// </remarks>
    bool IsBusy { get; }

    /// <summary>Whether anything in flight has offered a way to stop it.</summary>
    /// <remarks>
    /// The only thing that shows the overlay's Cancel button
    /// (<see cref="QuickStat.ViewModels.BusyOverlayViewModel.IsCancelOffered"/>). It is false
    /// whenever every scope in flight took <see cref="BeginOperation(string)"/>, which is most of
    /// them: a save, a delete or a caption load is one round trip, and a button that cannot stop
    /// anything is worse than no button.
    /// </remarks>
    bool IsCancellable { get; }

    /// <summary>Sets <see cref="Info"/> without touching the percentage.</summary>
    /// <param name="info">The new status line.</param>
    void SetInfo(string info);

    /// <summary>Shows a failure: <paramref name="message"/> as the status line, in the error colour.</summary>
    /// <param name="message">What went wrong, already fit to show a user.</param>
    void Fail(string message);

    /// <summary>Progress to 100 % and the status line to <c>Task completed</c>.</summary>
    /// <remarks>Delphi <c>Done</c> (<c>MainQuickStat.pas:426-431</c>), called after connect and after a collect run.</remarks>
    void Done();

    /// <summary>Back to <c>Program is idle</c> at 0 %.</summary>
    void Reset();

    /// <summary>Signals every cancellation source currently offered.</summary>
    /// <remarks>
    /// <para>
    /// Signals, and nothing else. <see cref="IsBusy"/> is deliberately untouched: only the
    /// operation's own scope knows when the work has actually stopped, which is the same reason the
    /// Delphi saved and restored <c>Screen.Cursor</c> rather than assigning <c>crDefault</c> (§G.3).
    /// </para>
    /// <para>
    /// Signals <em>all</em> of them, not the innermost: the user is cancelling the operation they can
    /// see, and a collect running inside a package replay is part of it. A source whose owner has
    /// already disposed it is skipped rather than thrown on - the operation it belonged to is over.
    /// </para>
    /// </remarks>
    void RequestCancellation();

    /// <summary>
    /// Marks the shell busy and sets the status line, until the returned token is disposed.
    /// </summary>
    /// <param name="info">The status line for the duration.</param>
    /// <returns>A token that clears the busy flag - and only then, if it is the outermost one.</returns>
    /// <remarks>
    /// The Delphi's pattern is <c>crSaved := Screen.Cursor; Screen.Cursor := crSqlWait; try … finally
    /// Screen.Cursor := crSaved</c>, and it saves and restores precisely because these operations
    /// nest: the package replay calls the collect action from inside its own wait cursor. This
    /// counts, so the inner scope does not clear the outer one.
    /// </remarks>
    IDisposable BeginOperation(string info);

    /// <summary>
    /// As <see cref="BeginOperation(string)"/>, and offers the user a way to stop this operation for
    /// as long as the scope lives.
    /// </summary>
    /// <param name="info">The status line for the duration.</param>
    /// <param name="cancellation">
    /// The source <see cref="RequestCancellation"/> signals. Owned by the caller and not disposed
    /// here; the scope only withdraws the offer.
    /// </param>
    /// <returns>A token that withdraws the offer and then clears the busy flag.</returns>
    /// <remarks>
    /// <para>
    /// <b>This is an addition, not parity.</b> The Delphi has no Cancel on the main form at all - the
    /// product's only two <c>bkCancel</c> buttons are on modal dialogs
    /// (<c>Emetra.VclForm.Period.dfm:169</c>, <c>Emetra.VclForm.EditAndMemo.dfm:994</c>) - and
    /// <c>actCollectDataExecute</c> could not be interrupted once it had started. The port needs the
    /// affordance for a reason the Delphi did not have: §G.3's wait cursor became a busy flag because
    /// the work now happens off the user-interface thread, so a run that will take minutes leaves a
    /// window that is alive and unusable rather than one that is visibly wedged.
    /// </para>
    /// <para>
    /// <b>Only pass a source the operation genuinely honours.</b> The offer is per call site rather
    /// than a property of every scope precisely so that the button appears only where cancelling
    /// works: <see cref="IsCancellable"/> is what shows it. PORT-PLAN.md §8.10 (c).
    /// </para>
    /// </remarks>
    IDisposable BeginOperation(string info, CancellationTokenSource cancellation);
}
