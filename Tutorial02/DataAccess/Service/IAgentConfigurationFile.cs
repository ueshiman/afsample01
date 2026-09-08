namespace Tutorial02.DataAccess.Service;

public interface IAgentConfigurationFile
{
    string Name { get; }
    string Directory { get; } // Configuration
    string Path { get; }
}