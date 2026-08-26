using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="UserNotifier"/>, most of them about one property: a confirmation is
/// <see langword="true"/> only when a human said yes.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §7.3 names failing open as the defect to remove. The Delphi's <c>LogYesNo</c>
/// returned yes whenever the severity fell below <c>ThresholdForDialog</c>, and its one reachable
/// QuickStat call site deletes a package (<c>MainQuickStat.pas:894</c>). These tests enumerate every
/// way a caller, a presenter or the container could arrive at a yes that nobody gave.
/// </remarks>
public class UserNotifierTests
{
    private readonly RecordingLogger _log = new();

    public static TheoryData<NotificationSeverity> AllSeverities => new()
    {
        NotificationSeverity.Information,
        NotificationSeverity.Warning,
        NotificationSeverity.Error,
    };

    /// <summary>Every way a presenter can fail to produce an answer.</summary>
    public static TheoryData<string> FailingPresenters => new()
    {
        "throws",
        "throws-operation-cancelled",
        "faulted-task",
        "cancelled-task",
        "null-task",
        "cancelled-after-the-fact",
    };

    [Fact]
    public async Task ConfirmAsyncIsFalseWhenNoPresenterIsWiredUp()
    {
        // The DI default. "Nothing configured" must be the safe state, not an exception and not a
        // silent yes.
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        Assert.False(await notifier.ConfirmAsync("Delete this package?"));
    }

    [Theory]
    [MemberData(nameof(FailingPresenters))]
    public async Task ConfirmAsyncIsFalseWhenThePresenterFails(string failure)
    {
        UserNotifier notifier = Create(new FailingPresenter(failure));

        Assert.False(await notifier.ConfirmAsync("Delete this package?"));
        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Theory]
    [MemberData(nameof(FailingPresenters))]
    public async Task ConfirmAsyncDoesNotPropagateAPresenterFailure(string failure)
    {
        // A question has a safe answer, so there is no reason to turn a broken dialog into a second
        // failure the caller has to handle - and a caller that wrapped this in a try/catch might
        // well treat the catch as "carry on".
        UserNotifier notifier = Create(new FailingPresenter(failure));

        bool answer = await notifier.ConfirmAsync("Delete this package?");

        Assert.False(answer);
    }

    [Theory]
    [MemberData(nameof(AllSeverities))]
    public async Task ConfirmAsyncAsksAtEverySeverity(NotificationSeverity severity)
    {
        // The Delphi compared severity against ThresholdForDialog and skipped the dialog below it.
        // There is no threshold here: every severity reaches the presenter.
        HeadlessNotificationPresenter presenter = new();
        UserNotifier notifier = Create(presenter);

        Assert.False(await notifier.ConfirmAsync("Delete this package?", severity));

        UserNotification asked = Assert.Single(presenter.Notifications);

        Assert.True(asked.IsQuestion);
        Assert.Equal(severity, asked.Severity);
    }

    [Theory]
    [MemberData(nameof(AllSeverities))]
    public async Task ConfirmAsyncIsFalseAtEverySeverityWhenNobodyAnswers(NotificationSeverity severity)
    {
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        Assert.False(await notifier.ConfirmAsync("Delete this package?", severity));
    }

    [Fact]
    public async Task ConfirmAsyncIsTrueOnlyWhenThePresenterSaysYes()
    {
        // The property that makes the guarantee total: the answer equals the presenter's answer,
        // for every sequence of answers. The presenter is the only source of a yes.
        bool[] script = [true, false, false, true, true, false];
        int index = 0;

        UserNotifier notifier = Create(new HeadlessNotificationPresenter(_ => script[index++]));

        foreach (bool expected in script)
        {
            Assert.Equal(expected, await notifier.ConfirmAsync("Delete this package?"));
        }
    }

    [Fact]
    public async Task ConfirmAsyncCarriesNoStateBetweenCalls()
    {
        // The Delphi kept the answer in a shared fModalResult field on the log adapter, so one call
        // could be answered by the previous one - and by another thread's dialog.
        HeadlessNotificationPresenter yes = HeadlessNotificationPresenter.Answering(true);
        UserNotifier notifier = Create(yes);

        Assert.True(await notifier.ConfirmAsync("first"));

        UserNotifier fresh = Create(new HeadlessNotificationPresenter());

        Assert.False(await fresh.ConfirmAsync("second"));
    }

    [Fact]
    public void UserNotifierExposesNothingThatCouldChangeItsAnswer()
    {
        // Structural, not aspirational: no properties, no fields, no options object - so there is
        // no threshold to lower, no default button to set and nothing to misconfigure.
        Assert.Empty(typeof(UserNotifier).GetProperties());
        Assert.Empty(typeof(UserNotifier).GetFields());

        string[] declaredMethods = typeof(UserNotifier)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] expected = ["ConfirmAsync", "ErrorAsync", "InformAsync", "WarnAsync"];

        Assert.Equal(expected, declaredMethods);
    }

    [Fact]
    public void UserNotifierIsTheOnlyNotifierInTheCoreAssembly()
    {
        // If a later step adds a second IUserNotifier - a "silent" one for batch mode, say - the
        // guarantee above stops being a guarantee about the application. This fails if that happens.
        Type[] implementations = typeof(IUserNotifier).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IUserNotifier).IsAssignableFrom(type))
            .ToArray();

        Assert.Equal(typeof(UserNotifier), Assert.Single(implementations));
    }

    [Fact]
    public async Task ConfirmAsyncLogsTheQuestionAndTheAnswer()
    {
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        await notifier.ConfirmAsync("Delete this package?", NotificationSeverity.Warning);

        Assert.Contains(_log.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Delete this package?", StringComparison.Ordinal));
        Assert.Contains(_log.Entries, entry => entry.Message.Contains("answered no", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(AllSeverities))]
    public async Task NotificationsAreLoggedAtTheMappedLevel(NotificationSeverity severity)
    {
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        Task shown = severity switch
        {
            NotificationSeverity.Information => notifier.InformAsync("hello"),
            NotificationSeverity.Warning => notifier.WarnAsync("hello"),
            _ => notifier.ErrorAsync("hello"),
        };

        await shown;

        LogLevel expected = severity switch
        {
            NotificationSeverity.Information => LogLevel.Information,
            NotificationSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Error,
        };

        Assert.Contains(_log.Entries, entry => entry.Level == expected && entry.Message.Contains("hello", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AMarkedIdentifierIsShownToTheUserButRedactedFromTheLog()
    {
        // "{{ }}" means: the clinician may see it, the file may not.
        HeadlessNotificationPresenter presenter = new();
        UserNotifier notifier = Create(presenter);

        await notifier.InformAsync($"Pasient {{{{{PiiRedactorTests.ValidFodselsnummer}}}}} lagret.");

        Assert.Contains(PiiRedactorTests.ValidFodselsnummer, Assert.Single(presenter.Notifications).Message, StringComparison.Ordinal);
        Assert.All(_log.Entries, entry => Assert.DoesNotContain(PiiRedactorTests.ValidFodselsnummer, entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnUnmarkedIdentifierIsAlsoKeptOutOfTheLog()
    {
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        await notifier.WarnAsync($"Fant {PiiRedactorTests.AnotherFodselsnummer} to ganger.");
        await notifier.ConfirmAsync($"Slette {PiiRedactorTests.AnotherFodselsnummer}?");

        Assert.NotEmpty(_log.Entries);
        Assert.All(_log.Entries, entry => Assert.DoesNotContain(PiiRedactorTests.AnotherFodselsnummer, entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task LiteralNewlineEscapesAreExpandedForTheDialog()
    {
        // MainQuickStat.pas:226, via PrepareForDialog.
        HeadlessNotificationPresenter presenter = new();
        UserNotifier notifier = Create(presenter);

        await notifier.ConfirmAsync("Do you really want to delete this package:\\n\"Utvalg A\"?");

        Assert.Equal(
            "Do you really want to delete this package:\n\"Utvalg A\"?",
            Assert.Single(presenter.Notifications).Message);
    }

    [Fact]
    public async Task LoggingNeverWaitsForThePresenter()
    {
        // The Delphi showed the dialog from inside TPlainTextLog.Event while holding the log lock:
        // logging blocked on a user, and every other thread blocked behind the dialog. Here the log
        // entry exists before the presenter has been entered, and nothing is held while it runs.
        using ManualResetEventSlim presenterEntered = new(initialState: false);
        using ManualResetEventSlim releasePresenter = new(initialState: false);

        BlockingPresenter presenter = new(presenterEntered, releasePresenter);
        UserNotifier notifier = Create(presenter);

        Task<bool> pending = Task.Run(() => notifier.ConfirmAsync("Blocking question?"));

        Assert.True(presenterEntered.Wait(TimeSpan.FromSeconds(10)), "The presenter should have been entered.");

        // The question is already durable while the "dialog" is still on screen, and a second
        // thread can log freely.
        Assert.Contains(_log.Entries, entry => entry.Message.Contains("Blocking question?", StringComparison.Ordinal));

        releasePresenter.Set();

        Assert.False(await pending);
    }

    [Fact]
    public async Task NotificationsSurviveAFailingPresenter()
    {
        // Failing to draw a dialog must not take down the operation that wanted to report something,
        // least of all when that operation is itself reporting a failure.
        UserNotifier notifier = Create(new FailingPresenter("throws"));

        await notifier.InformAsync("a");
        await notifier.WarnAsync("b");
        await notifier.ErrorAsync("c");

        Assert.Contains(_log.Entries, entry => entry.Message.Contains("Notifying user: a", StringComparison.Ordinal));
        Assert.Contains(_log.Entries, entry => entry.Message.Contains("Notifying user: b", StringComparison.Ordinal));
        Assert.Contains(_log.Entries, entry => entry.Message.Contains("Notifying user: c", StringComparison.Ordinal));
    }

    [Fact]
    public void ConstructorRejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => new UserNotifier(null!, NullLogger<UserNotifier>.Instance));
        Assert.Throws<ArgumentNullException>(() => new UserNotifier(new HeadlessNotificationPresenter(), null!));
    }

    [Fact]
    public async Task ANullMessageIsRejectedRatherThanShown()
    {
        UserNotifier notifier = Create(new HeadlessNotificationPresenter());

        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.ConfirmAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => notifier.InformAsync(null!));
    }

    private UserNotifier Create(IUserNotificationPresenter presenter) => new(presenter, _log);

    /// <summary>Every shape of presenter failure that could be mistaken for an answer.</summary>
    private sealed class FailingPresenter(string failure) : IUserNotificationPresenter
    {
        public Task PresentAsync(UserNotification notification) => Fail();

        public Task<bool> AskAsync(UserNotification notification) => Fail();

        private Task<bool> Fail()
        {
            switch (failure)
            {
                case "throws":
                    throw new InvalidOperationException("No dispatcher.");

                case "throws-operation-cancelled":
                    throw new OperationCanceledException();

                case "faulted-task":
                    return Task.FromException<bool>(new InvalidOperationException("No dispatcher."));

                case "cancelled-task":
                    return Task.FromCanceled<bool>(new CancellationToken(canceled: true));

                case "null-task":
                    // A presenter that forgot to return anything. "await null" throws, and the
                    // catch turns it into a no.
                    return null!;

                case "cancelled-after-the-fact":
                    {
                        // A dialog that was opened and then abandoned - the window closed under it,
                        // the shell shut down, the operation was cancelled while it was on screen.
                        TaskCompletionSource<bool> pending = new();

                        pending.SetCanceled();

                        return pending.Task;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(failure));
            }
        }
    }

    /// <summary>Stands in for a modal dialog that is still on screen.</summary>
    private sealed class BlockingPresenter(ManualResetEventSlim entered, ManualResetEventSlim release)
        : IUserNotificationPresenter
    {
        public Task PresentAsync(UserNotification notification) => AskAsync(notification);

        public Task<bool> AskAsync(UserNotification notification)
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(10));

            return Task.FromResult(false);
        }
    }

    /// <summary>Captures what reached the log, so tests can assert on level and content.</summary>
    private sealed class RecordingLogger : ILogger<UserNotifier>
    {
        private readonly Lock _gate = new();
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_gate)
                {
                    return _entries.ToArray();
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_gate)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
