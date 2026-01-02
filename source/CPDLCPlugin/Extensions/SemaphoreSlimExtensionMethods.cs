namespace CPDLCPlugin.Extensions;

public static class SemaphoreSlimExtensionMethods
{
    public static async Task<IDisposable> Lock(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return new Releaser(semaphoreSlim);
    }

    class Releaser(SemaphoreSlim semaphoreSlim) : IDisposable
    {
        public void Dispose() => semaphoreSlim.Release();
    }
}
