using System.Security.AccessControl;

namespace FolderSyncService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string sourceFolder = Environment.GetEnvironmentVariable("SOURCE_FOLDER");
                string replicaFolder = Environment.GetEnvironmentVariable("REPLICA_FOLDER");
                int syncIntervalSeconds = int.Parse(Environment.GetEnvironmentVariable("SYNC_INTERVAL_SECONDS") ?? "60");
                string logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");


                if (string.IsNullOrEmpty(sourceFolder))
                {
                    sourceFolder = Path.Combine(Directory.GetCurrentDirectory(), "SourceFolder");
                    Directory.CreateDirectory(sourceFolder);
                    Environment.SetEnvironmentVariable("SOURCE_FOLDER", sourceFolder);
                    _logger.LogInformation($"Created default source folder: {sourceFolder}");
                }

                if (string.IsNullOrEmpty(replicaFolder))
                {
                    replicaFolder = Path.Combine(Directory.GetCurrentDirectory(), "ReplicaFolder");
                    Directory.CreateDirectory(replicaFolder);
                    Environment.SetEnvironmentVariable("REPLICA_FOLDER", replicaFolder);
                    _logger.LogInformation($"Created default replica folder: {replicaFolder}");
                }

                if (string.IsNullOrEmpty(logFilePath))
                {
                    logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log_file.log");
                    Environment.SetEnvironmentVariable("LOG_FILE_PATH", logFilePath);
                    _logger.LogInformation($"Created default log file: {logFilePath}");
                }

                try
                {
                    using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                    {
                        logFile.WriteLine($"[{DateTime.Now}] Synchronization started.");

                        SynchronizeFolders(sourceFolder, replicaFolder, logFile);

                        logFile.WriteLine($"[{DateTime.Now}] Synchronization completed.");
                    }

                    Console.WriteLine($"[{DateTime.Now}] Folders synchronized successfully.");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Error copying {sourceFolder}: {ex.Message}");
                    logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log_file.log");
                    Environment.SetEnvironmentVariable("LOG_FILE_PATH", logFilePath);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] An error occurred: {ex.Message}");
                }

            await Task.Delay(60000, stoppingToken);
            }
        }
        /// <summary>
        /// This method recursivly checks the source and replica paths, adding or removing file, perfoming the syncronization.
        /// </summary>
        /// <param name="source">Source path for the sync</param>
        /// <param name="replica">Replica path for the sync</param>
        /// <param name="logFile">The output path for the logs</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void SynchronizeFolders(string source, string replica, StreamWriter logFile)
        {
            if (!Directory.Exists(source))
            {
                throw new DirectoryNotFoundException($"Source folder '{source}' does not exist.");
            }

            if (!Directory.Exists(replica))
            {
                Directory.CreateDirectory(replica);
                logFile.WriteLine($"[{DateTime.Now}] Created folder: {replica}");
                Console.WriteLine($"Created folder: {replica}");
            }

            foreach (string sourceFilePath in Directory.GetFiles(source))
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string replicaFilePath = Path.Combine(replica, fileName);

                if (!File.Exists(replicaFilePath))
                {
                    File.Copy(sourceFilePath, replicaFilePath);
                    logFile.WriteLine($"[{DateTime.Now}] Copied: {sourceFilePath} to {replicaFilePath}");
                    Console.WriteLine($"Copied: {sourceFilePath} to {replicaFilePath}");
                }
            }
            foreach (string replicaFilePath in Directory.GetFiles(replica))
            {
                string fileName = Path.GetFileName(replicaFilePath);
                string sourceFilePath = Path.Combine(source, fileName);

                if (!File.Exists(sourceFilePath))
                {
                    File.Delete(replicaFilePath);
                    logFile.WriteLine($"[{DateTime.Now}] Deleted: {replicaFilePath}");
                    Console.WriteLine($"Deleted: {replicaFilePath}");
                }
            }

            foreach (string subfolder in Directory.GetDirectories(source))
            {
                string subfolderName = Path.GetFileName(subfolder);
                string replicaSubfolder = Path.Combine(replica, subfolderName);

                SynchronizeFolders(subfolder, replicaSubfolder, logFile);
            }
        }
    }
}