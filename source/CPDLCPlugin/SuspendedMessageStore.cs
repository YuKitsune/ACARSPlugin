using System.Collections.Concurrent;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin;

public class SuspendedMessageStore
{
    readonly ConcurrentDictionary<string, UplinkMessageElementViewModel[]> _suspendedUplinkMessages = new();

    public void Add(string callsign, UplinkMessageElementViewModel[] messageElements)
    {
        _suspendedUplinkMessages[callsign] = messageElements;
    }

    public bool HasSuspendedMessage(string callsign)
    {
        return _suspendedUplinkMessages.ContainsKey(callsign);
    }

    public bool TryRemove(string callsign, out UplinkMessageElementViewModel[] messageElements)
    {
        return _suspendedUplinkMessages.TryRemove(callsign, out messageElements);
    }
}
