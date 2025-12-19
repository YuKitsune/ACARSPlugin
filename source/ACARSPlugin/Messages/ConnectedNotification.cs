using MediatR;

namespace ACARSPlugin.Messages;

public class ConnectedNotification(string StationId) : INotification;