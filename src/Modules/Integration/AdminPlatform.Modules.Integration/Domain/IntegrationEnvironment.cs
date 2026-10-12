namespace AdminPlatform.Modules.Integration.Domain;

/// <summary>Which side of a provider the connection talks to. Sandbox must never reach real customers or bill money.</summary>
public enum IntegrationEnvironment
{
    Sandbox,
    Live,
}
