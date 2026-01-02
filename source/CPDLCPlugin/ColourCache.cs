using vatsys.Plugin;

namespace CPDLCPlugin;

/// <summary>
/// Need to cache <see cref="CustomColour"/> to avoid deadlock when accessing <see cref="Theme"/> from non-GUI threads.
/// </summary>
public class ColourCache
{
    public required CustomColour DownlinkBackgroundColour { get; set; }
    public required CustomColour UnableBackgroundColour { get; set; }
    public required CustomColour SuspendedForegroundColour { get; set; }
}
