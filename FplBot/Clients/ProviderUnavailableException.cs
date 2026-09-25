namespace FplBot.Clients;

// Intentionally excludes URLs, response bodies and inner exceptions containing credentials.
public sealed class ProviderUnavailableException(string provider) : Exception($"{provider} is unavailable.");
