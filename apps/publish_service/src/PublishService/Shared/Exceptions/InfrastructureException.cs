namespace PublishService.Shared.Exceptions;

public class InfrastructureException(string message, Exception? innerException = null)
    : Exception(message, innerException);