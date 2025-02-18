using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public class ConfigSettings
{
    public LoggingSettings Logging { get; set; }
    public Dictionary<string, ServerDetail> Servers { get; set; }

    public class LoggingSettings
    {
        public LogLevelSettings LogLevel { get; set; }

        public class LogLevelSettings
        {
            public string Default { get; set; }
            public string System { get; set; }
            public string Microsoft { get; set; }
        }
    }

    public class ServerDetail
    {
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Enabled { get; set; }
        public string Mode { get; set; }
        public int Port { get; set; }
        public List<DirectoryPair> DirectoryPairs { get; set; }
    }

    public class DirectoryPair
    {
        public string RemotePath { get; set; }
        public string LocalCompletePath { get; set; }
        public string LocalTempPath { get; set; }
    }

    public static ConfigSettings Load(IConfiguration configuration)
    {
        var configSettings = new ConfigSettings
        {
            Logging = new LoggingSettings
            {
                LogLevel = new LoggingSettings.LogLevelSettings
                {
                    Default = configuration["Logging:LogLevel:Default"],
                    System = configuration["Logging:LogLevel:System"],
                    Microsoft = configuration["Logging:LogLevel:Microsoft"]
                }
            },
            Servers = new Dictionary<string, ServerDetail>()
        };

        foreach (var server in configuration.GetSection("Servers").GetChildren())
        {
            var directoryPairs = new List<DirectoryPair>();
            foreach (var pair in server.GetSection("DirectoryPairs").GetChildren())
            {
                directoryPairs.Add(new DirectoryPair
                {
                    RemotePath = pair["RemotePath"],
                    LocalCompletePath = pair["LocalCompletePath"],
                    LocalTempPath = pair["LocalTempPath"]
                });
            }

            configSettings.Servers[server.Key] = new ServerDetail
            {
                Hostname = server["Hostname"],
                Username = server["Username"],
                Password = server["Password"],
                Enabled = bool.Parse(server["Enabled"]),
                Mode = server["Mode"],
                Port = int.Parse(server["Port"]),
                DirectoryPairs = directoryPairs
            };
        }

        return configSettings;
    }
}
