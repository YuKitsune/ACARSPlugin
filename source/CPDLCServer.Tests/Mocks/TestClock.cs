using CPDLCServer.Infrastructure;

namespace CPDLCServer.Tests.Mocks;

public class TestClock : IClock
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public DateTimeOffset UtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset dateTime)
    {
        _utcNow = dateTime;
    }
}
