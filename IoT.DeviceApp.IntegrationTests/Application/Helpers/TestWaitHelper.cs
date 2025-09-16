namespace IoT.DeviceApp.IntegrationTests.Application.Helpers;

public static class TestWaitHelper
{
    /// <summary>
    /// Polls until the given condition is true or the cancellation token is triggered.
    /// </summary>
    public static async Task WaitUntilAsync(
        Func<bool> condition,
        CancellationToken cancellationToken = default,
        int pollIntervalMs = 10)
    {
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(pollIntervalMs, cancellationToken);
        }
    }
}
