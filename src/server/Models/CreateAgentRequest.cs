namespace server.Models;

public record CreateAgentRequest(string Hostname, string PairingCode, string? Name = null);

