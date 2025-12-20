namespace ACARSPlugin;

public interface IClock
{
    DateTimeOffset UtcNow();
}