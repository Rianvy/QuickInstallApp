using Newtonsoft.Json;

namespace QuickInstall
{
    class ProgramManager
    {
        public List<ProgramInfo> Programs { get; private set; }
        public HashSet<string> Tags { get; private set; }
        public string SelectedTag { get; set; } = "all";
        public string SelectedArchitecture { get; set; } = "64-bit";
        public bool IsInstallAfterDownload { get; set; } = false;

        private string jsonPath;
        private string downloadDirectory;
        private Downloader downloader;
        private Installer installer;

        public event Action<int> ProgressChanged;
        public event Action<string> StatusChanged;

        public ProgramManager(string jsonPath, string downloadDirectory)
        {
            this.jsonPath = jsonPath;
            this.downloadDirectory = downloadDirectory;
            this.downloader = new Downloader();
            this.installer = new Installer();
            Directory.CreateDirectory(downloadDirectory);

            LoadPrograms();
        }

        private void LoadPrograms()
        {
            Tags = new HashSet<string> { "all" };
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    Programs = JsonConvert.DeserializeObject<List<ProgramInfo>>(json);

                    foreach (var program in Programs)
                    {
                        foreach (var tag in program.Tags)
                        {
                            Tags.Add(tag.ToLower());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading programs: {ex.Message}");
                    Programs = new List<ProgramInfo>();
                }
            }
            else
            {
                Console.WriteLine("Program list JSON not found, creating an empty list.");
                Programs = new List<ProgramInfo>();
            }
        }

        public async Task InstallSelectedProgramsAsync()
        {
            int maxConcurrentDownloads = 3;
            var semaphore = new SemaphoreSlim(maxConcurrentDownloads);

            int completed = 0;
            int total = Programs.Count(p => p.IsSelected);

            Action<int> onProgressUpdate = (increment) =>
            {
                Interlocked.Add(ref completed, increment);
                ProgressChanged?.Invoke((completed * 100) / total);
            };

            var tasks = Programs
                .Where(p => p.IsSelected)
                .Select(program => InstallProgramAsync(program, semaphore, total, onProgressUpdate));

            await Task.WhenAll(tasks);

            StatusChanged?.Invoke(IsInstallAfterDownload ? "Installation complete." : "Download complete.");
        }

        private async Task InstallProgramAsync(ProgramInfo program, SemaphoreSlim semaphore, int total, Action<int> onProgressUpdate)
        {
            await semaphore.WaitAsync();
            try
            {
                if (!program.Architectures.TryGetValue(SelectedArchitecture, out string url))
                {
                    StatusChanged?.Invoke($"[Error] Architecture {SelectedArchitecture} not available for {program.Name}.");
                    return;
                }

                string installerPath = Path.Combine(downloadDirectory, $"{program.Name}_{SelectedArchitecture}.exe");
                StatusChanged?.Invoke($"Downloading {program.Name}...");

                await downloader.DownloadFileAsync(url, installerPath, p =>
                {
                    StatusChanged?.Invoke($"Downloading {program.Name}: {p}%");
                });

                if (IsInstallAfterDownload)
                {
                    StatusChanged?.Invoke($"Installing {program.Name}...");
                    installer.InstallProgram(installerPath, program.Arguments, StatusChanged);
                }

                onProgressUpdate(1);
                StatusChanged?.Invoke($"Completed {program.Name}");
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
