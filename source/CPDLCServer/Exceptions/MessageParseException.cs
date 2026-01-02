namespace CPDLCServer.Exceptions;

public class MessageParseException(string message, Exception? innerException = null)
    : Exception(message, innerException);