namespace ACARSPlugin.Model;

public enum DialogueState
{
    Open,              // Active dialogue
    ClosedPending,     // Closed, waiting to transfer to history
    InHistory          // Moved to history
}
