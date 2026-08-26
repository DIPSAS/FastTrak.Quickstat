namespace QuickStat.Diagnostics;

/// <summary>
/// The presenter used when there is no user interface: unit tests, and <c>QuickStat.Core</c> hosted
/// without WPF. It records what it was asked to show and answers every confirmation with no.
/// </summary>
/// <remarks>
/// <para>
/// This is what <c>AddQuickStatDiagnostics</c> registers by default, which is what makes "no
/// notifier wired up" a safe state rather than a null reference: the question is still logged by
/// <see cref="UserNotifier"/>, and the answer is still no.
/// </para>
/// <para>
/// A test that needs a yes must say so at construction - <see cref="Answering"/> or the
/// <see cref="HeadlessNotificationPresenter(Func{UserNotification, bool})"/> constructor. There is
/// deliberately no settable property: an instance cannot become optimistic after the container has
/// handed it out, and the parameterless constructor the container uses can only answer no.
/// </para>
/// </remarks>
public sealed class HeadlessNotificationPresenter : IUserNotificationPresenter
{
    private readonly Func<UserNotification, bool>? _answer;
    private readonly Lock _gate = new();
    private readonly List<UserNotification> _notifications = [];

    /// <summary>Creates a presenter that answers every confirmation with no.</summary>
    public HeadlessNotificationPresenter()
    {
    }

    /// <summary>Creates a presenter that answers confirmations with a supplied decision.</summary>
    /// <param name="answerConfirmation">Decides each question. For tests only.</param>
    /// <exception cref="ArgumentNullException"><paramref name="answerConfirmation"/> is <see langword="null"/>.</exception>
    public HeadlessNotificationPresenter(Func<UserNotification, bool> answerConfirmation)
    {
        ArgumentNullException.ThrowIfNull(answerConfirmation);

        _answer = answerConfirmation;
    }

    /// <summary>Everything this presenter has been asked to show, in order.</summary>
    public IReadOnlyList<UserNotification> Notifications
    {
        get
        {
            lock (_gate)
            {
                return _notifications.ToArray();
            }
        }
    }

    /// <summary>Creates a presenter that answers every confirmation the same way. For tests only.</summary>
    /// <param name="answer">The answer to give.</param>
    /// <returns>A presenter that stands in for a user who always answers <paramref name="answer"/>.</returns>
    public static HeadlessNotificationPresenter Answering(bool answer) => new(_ => answer);

    /// <inheritdoc />
    public Task PresentAsync(UserNotification notification)
    {
        Add(notification);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> AskAsync(UserNotification notification)
    {
        Add(notification);

        // No configured answer means no answer, which means no.
        return Task.FromResult(_answer is not null && _answer(notification));
    }

    private void Add(UserNotification notification)
    {
        lock (_gate)
        {
            _notifications.Add(notification);
        }
    }
}
