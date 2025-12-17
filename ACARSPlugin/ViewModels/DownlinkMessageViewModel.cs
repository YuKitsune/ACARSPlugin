namespace ACARSPlugin.ViewModels;

public class DownlinkMessageViewModel
{
    public DateTimeOffset Received { get; set; }
    public bool StandbySent { get; set; }
    public bool Deferred { get; set; }
    public string Message { get; set; }
    public bool Selected { get; set; }
}