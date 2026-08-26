using System.Globalization;
using QuickStat.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Diagnostics;

/// <summary>
/// The default presenter: what happens when there is no user interface at all.
/// </summary>
public class HeadlessNotificationPresenterTests
{
    [Fact]
    public async Task ThePlainConstructorAnswersNo()
    {
        HeadlessNotificationPresenter presenter = new();

        Assert.False(await presenter.AskAsync(Question()));
    }

    [Fact]
    public async Task AnAnswerHasToBeAskedForExplicitly()
    {
        Assert.True(await HeadlessNotificationPresenter.Answering(true).AskAsync(Question()));
        Assert.False(await HeadlessNotificationPresenter.Answering(false).AskAsync(Question()));
    }

    [Fact]
    public async Task AnAnswerCanDependOnTheQuestion()
    {
        HeadlessNotificationPresenter presenter = new(notification => notification.Message.Contains("yes", StringComparison.Ordinal));

        Assert.True(await presenter.AskAsync(Question("say yes")));
        Assert.False(await presenter.AskAsync(Question("say nothing")));
    }

    [Fact]
    public void ThereIsNoWayToMakeAnInstanceOptimisticAfterTheFact()
    {
        // A settable property would be a configuration switch by another name, and the container
        // hands this instance to everyone.
        Assert.DoesNotContain(typeof(HeadlessNotificationPresenter).GetProperties(), property => property.CanWrite);
    }

    [Fact]
    public async Task EverythingShownIsRecordedInOrder()
    {
        HeadlessNotificationPresenter presenter = new();

        await presenter.PresentAsync(Statement("first"));
        await presenter.AskAsync(Question("second"));
        await presenter.PresentAsync(Statement("third"));

        Assert.Equal(
            ["first", "second", "third"],
            presenter.Notifications.Select(notification => notification.Message).ToArray());
    }

    [Fact]
    public async Task TheRecordedListIsASnapshot()
    {
        HeadlessNotificationPresenter presenter = new();

        IReadOnlyList<UserNotification> before = presenter.Notifications;

        await presenter.PresentAsync(Statement("later"));

        Assert.Empty(before);
        Assert.Single(presenter.Notifications);
    }

    [Fact]
    public async Task ManyThreadsCanReportAtOnce()
    {
        HeadlessNotificationPresenter presenter = new();

        await Parallel.ForAsync(0, 500, async (index, _) =>
            await presenter.PresentAsync(Statement(index.ToString(CultureInfo.InvariantCulture))));

        Assert.Equal(500, presenter.Notifications.Count);
    }

    [Fact]
    public void ANullAnswerFunctionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new HeadlessNotificationPresenter(null!); });
    }

    private static UserNotification Question(string message = "Delete this package?")
        => new(message, null, NotificationSeverity.Warning, IsQuestion: true);

    private static UserNotification Statement(string message)
        => new(message, null, NotificationSeverity.Information, IsQuestion: false);
}
