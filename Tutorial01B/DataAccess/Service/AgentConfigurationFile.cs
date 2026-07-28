namespace Tutorial01B.DataAccess.Service
{
    public class AgentConfigurationFile : IAgentConfigurationFile
    {
        public string Name { get; } = "agentsettings.json";

        public string Directory { get; } = Environment.CurrentDirectory; // Configuration

        public string Path => System.IO.Path.Combine(Directory, Name);
    }
}
