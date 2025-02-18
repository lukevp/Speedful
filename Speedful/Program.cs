using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

class Program
{
    class FtpConnectionPool
    {
        private readonly string _hostname;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _connectionType;
        private AsyncFtpClient _primaryConnection;
        private HashSet<AsyncFtpClient> _secondaryConnections;

        public FtpConnectionPool(string hostname, int port, string username, string password, string connectionType)
        {
            _hostname = hostname;
            _port = port;
            _username = username;
            _password = password;
            _connectionType = connectionType;

            // Create one connection that is the "info" connection
            // for polling files and listing directories, getting hashes for validations, deleting folders, etc. as a long-running connection
            // Create x other connections as data transfer connections.  Maybe they close after some time?
            _primaryConnection = CreateClient();
            _secondaryConnections = new HashSet<AsyncFtpClient>();
        }

        private AsyncFtpClient CreateClient()
        {
            var client = new AsyncFtpClient(_hostname, new NetworkCredential()
            {
                UserName = _username,
                Password = _password
            }, _port);
            if (_connectionType == "FTP")
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
            }
            else if (_connectionType == "SFTP")
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
                client.Config.SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11;
            }

            return client;
        }
    }

    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("speedful.json", optional: false, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        var configSettings = ConfigSettings.Load(configuration);

        if (configSettings.Servers.TryGetValue("example-host", out var serverDetail) && serverDetail.Enabled && serverDetail.Mode == "SFTP")
        {
            var client = new AsyncFtpClient(serverDetail.Hostname, new NetworkCredential()
            {
                UserName = serverDetail.Username,
                Password = serverDetail.Password
            }, serverDetail.Port);

            try
            {
                await client.Connect();
                foreach (var pair in serverDetail.DirectoryPairs)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        // 
                        // var dir = await client.GetWorkingDirectory();
                        // var list = await client.GetListing();
                        await client.DownloadStream(memoryStream, pair.RemotePath);
                        // Save to local temp path
                        await File.WriteAllBytesAsync(pair.LocalTempPath, memoryStream.ToArray());
                        // Move to local complete path
                        File.Move(pair.LocalTempPath, pair.LocalCompletePath, true);
                    }
                }
            }
            finally
            {
                await client.Disconnect();
            }
        }
    }
}