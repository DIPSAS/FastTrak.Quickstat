using System.Reflection;
using QuickStat.Diagnostics;
using QuickStat.Services;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The presenter half of the notification seam: it marshals, it renders what it was given, and it
/// answers a confirmation with yes only when the user did.
/// </summary>
/// <remarks>
/// The window is substituted here; <c>NotificationDialogTests</c> drives the real one. What is
/// checked is the glue that decides an answer, because that glue is where a never-fail-open rule can
/// be lost to a single operator.
/// </remarks>
public class WpfNotificationPresenterTests
{
    private static readonly UserNotification Question =
        new("Do you really want to delete this package:\n\"x\"?", null, NotificationSeverity.Warning, true);

    private static readonly UserNotification Statement =
        new("Selection was successfully saved.", null, NotificationSeverity.Information, false);

    [Fact]
    public async Task AYesIsTheOnlyYes()
    {
        Assert.True(await Ask(_ => true));
        Assert.False(await Ask(_ => false));

        // The close box leaves DialogResult null, and a null is not an answer.
        Assert.False(await Ask(_ => null));
    }

    [Fact]
    public async Task APresenterThatThrowsStillDoesNotAnswerYes()
    {
        // UserNotifier catches this and logs it; what matters here is that nothing on the way out of
        // AskAsync can turn a failure into a yes.
        WpfNotificationPresenter presenter = new(
            new SpyDispatcher(),
            _ => throw new InvalidTimeZoneException("no window"));

        await Assert.ThrowsAsync<InvalidTimeZoneException>(() => presenter.AskAsync(Question));

        Assert.False(await new UserNotifierProbe(presenter).ConfirmAsync(Question.Message));
    }

    [Fact]
    public async Task EverythingGoesThroughTheDispatcher()
    {
        SpyDispatcher dispatcher = new();
        WpfNotificationPresenter presenter = new(dispatcher, _ => true);

        await presenter.PresentAsync(Statement);
        _ = await presenter.AskAsync(Question);

        // Callers of IUserNotifier are told not to marshal and UserNotifier does not either, so the
        // presenter is the only place it can happen.
        Assert.Equal(2, dispatcher.InvokeAsyncCount);
    }

    [Fact]
    public async Task TheNotificationReachesTheWindowUntouched()
    {
        List<UserNotification> shown = [];

        WpfNotificationPresenter presenter = new(
            new SpyDispatcher(),
            notification =>
            {
                shown.Add(notification);

                return true;
            });

        await presenter.PresentAsync(Statement);
        _ = await presenter.AskAsync(Question);

        Assert.Equal([Statement, Question], shown);
    }

    [Fact]
    public void ThePresenterIsNotAnotherUserNotifier()
    {
        // 07-ui-contracts.md and PORT-PLAN.md §5: severity mapping, PII redaction and the
        // never-fail-open rule stay in QuickStat.Core.  Core's own test asserts UserNotifier is the
        // only implementation there; this asserts the shell did not add a second one.
        List<Type> notifiers =
        [
            .. typeof(WpfNotificationPresenter).Assembly
                .GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false })
                .Where(type => typeof(IUserNotifier).IsAssignableFrom(type)),
        ];

        Assert.Empty(notifiers);
    }

    [Fact]
    public void TheDefaultTitleIsTheApplicationName() =>
        Assert.Equal("QuickStat", WpfNotificationPresenter.DefaultTitle);

    [Fact]
    public void TheContainerCannotPickTheTestSeam()
    {
        // The four-argument constructor is internal precisely so the container never sees it; a
        // public one would make the composition ambiguous.
        ConstructorInfo[] constructors = typeof(WpfNotificationPresenter)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Single(constructors);
        Assert.Single(constructors[0].GetParameters());
    }

    private static async Task<bool> Ask(Func<UserNotification, bool?> answer) =>
        await new WpfNotificationPresenter(new SpyDispatcher(), answer).AskAsync(Question);

    /// <summary>Runs inline, and counts.</summary>
    private sealed class SpyDispatcher : IUiDispatcher
    {
        internal int InvokeAsyncCount { get; private set; }

        public bool IsOnUiThread => true;

        public void Invoke(Action action) => action();

        public void Post(Action action) => action();

        public Task InvokeAsync(Action action)
        {
            InvokeAsyncCount++;

            action();

            return Task.CompletedTask;
        }
    }

    /// <summary>The real <see cref="UserNotifier"/> over a presenter, with logging discarded.</summary>
    private sealed class UserNotifierProbe(IUserNotificationPresenter presenter)
    {
        private readonly UserNotifier _notifier =
            new(presenter, Microsoft.Extensions.Logging.Abstractions.NullLogger<UserNotifier>.Instance);

        internal Task<bool> ConfirmAsync(string message) => _notifier.ConfirmAsync(message);
    }
}
