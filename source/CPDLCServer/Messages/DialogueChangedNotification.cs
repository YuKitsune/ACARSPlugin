using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record DialogueChangedNotification(Dialogue Dialogue) : INotification;