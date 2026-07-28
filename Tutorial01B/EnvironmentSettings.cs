using static System.IO.Path;

namespace Tutorial01B
{
    public class EnvironmentSettings
    {
        public string SettingsFilePath { get; }

        public EnvironmentSettings(IWebHostEnvironment environment)
        {
            SettingsFilePath = Combine(environment.ContentRootPath, "agentsettings.json");
        }
    }
}
