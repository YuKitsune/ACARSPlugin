using System.Collections;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IDialogueRepository
{
    Task Add(Dialogue dialogue, CancellationToken cancellationToken);

    Task<Dialogue?> FindDialogueForMessage(
        string aircraftCallsign,
        int messageId,
        CancellationToken cancellationToken);

    Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken);

    Task<Dialogue[]> All(CancellationToken cancellationToken);

    Task Remove(Dialogue dialogue, CancellationToken cancellationToken);
}
