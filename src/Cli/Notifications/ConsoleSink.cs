using Core.Notifications;

namespace Cli.Notifications;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(string message)
    {
        Console.WriteLine(message);
    }
}