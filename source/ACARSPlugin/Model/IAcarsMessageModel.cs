namespace ACARSPlugin.Model;

public interface IAcarsMessageModel
{
    int Id { get; }
    string Content { get; }
    DateTimeOffset Time { get; }
    bool IsAcknowledged { get; }
}