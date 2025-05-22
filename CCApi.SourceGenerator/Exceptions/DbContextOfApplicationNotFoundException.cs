namespace CCApi.SourceGenerator.Exceptions;

public class DbContextOfApplicationNotFoundException(string message = $"The DbContext was not found during compilation.")
    : Exception(message);