namespace ACARSPlugin.Model;

public enum MessageState
{
    Normal,               // Default state
    WaitingForResponse,   // Sent uplink, awaiting pilot response
    PilotAnswerLate,      // Pilot response timeout exceeded
    ControllerLate,       // Controller response timeout exceeded
    TransmissionFailure,  // Aircraft unreachable
    Urgent,               // Urgent/emergency message
    Closed                // Dialogue closed
}
