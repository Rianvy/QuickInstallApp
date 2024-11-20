using Newtonsoft.Json;

namespace QuickInstall
{
    public class ProgramManager
    {
        public List<ProgramInfo> Programs { get; private set; } = new();
        public HashSet<string> Tags { get; private set; } = new() { "all" };
        public string SelectedTag { get; set; } = "all";
        public string SelectedArchitecture { get; set; } = "64-bit";
        public bool IsInstallAfterDownload { get; set; }

        private readonly string _jsonPath;
        private readonly string _downloadDirectory;
        private readonly Downloader _downloader;
        private readonly Installer _installer;

        public event Action<int>? ProgressChanged;
        public event Action<string>? StatusChanged;

        public ProgramManager(string jsonPath, string downloadDirectory)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ArgumentException("JSON path cannot be null or empty.", nameof(jsonPath));

            if (string.IsNullOrWhiteSpace(downloadDirectory))
                throw new ArgumentException("Download directory cannot be null or empty.", nameof(downloadDirectory));

            _jsonPath = jsonPath;
            _downloadDirectory = downloadDirectory;
            _downloader = new Downloader();
            _installer = new Installer();
            Directory.CreateDirectory(_downloadDirectory);

            LoadPrograms();
        }

        private void LoadPrograms()
        {
            try
            {
                if (!File.Exists(_jsonPath))
                {
                    StatusChanged?.Invoke("Program list JSON not found. Creating an empty program list.");
                    return;
                }

                var json = File.ReadAllText(_jsonPath);
                Programs = JsonConvert.DeserializeObject<List<ProgramInfo>>(json) ?? new List<ProgramInfo>();

                foreach (var program in Programs)
                {
                    foreach (var tag in program.Tags)
                    {
                        Tags.Add(tag.ToLower());
                    }
                }
            }
            catch (JsonException ex)
            {
                StatusChanged?.Invoke($"Error parsing JSON: {ex.Message}");
                Programs = new List<ProgramInfo>();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Unexpected error loading programs: {ex.Message}");
                Programs = new List<ProgramInfo>();
            }
        }

        public async Task InstallSelectedProgramsAsync()
        {
            var selectedPrograms = Programs.Where(p => p.IsSelected).ToList();
            if (!selectedPrograms.Any())
            {
                StatusChanged?.Invoke("No programs selected for installation.");
                return;
            }

            const int maxConcurrentDownloads = 3;
            using var semaphore = new SemaphoreSlim(maxConcurrentDownloads);

            int completed = 0;
            int total = selectedPrograms.Count;

            ProgressChanged?.Invoke(0);

            async Task InstallProgramWrapper(ProgramInfo program)
            {
                await semaphore.WaitAsync();
                try
                {
                    await InstallProgramAsync(program);
                    Interlocked.Increment(ref completed);
                    ProgressChanged?.Invoke((completed * 100) / total);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            var tasks = selectedPrograms.Select(InstallProgramWrapper);

            await Task.WhenAll(tasks);

            StatusChanged?.Invoke(IsInstallAfterDownload ? "Installation complete." : "Download complete.");
        }

        private async Task InstallProgramAsync(ProgramInfo program)
        {
            if (!program.Architectures.TryGetValue(SelectedArchitecture, out var url))
            {
                StatusChanged?.Invoke($"[Error] Architecture {SelectedArchitecture} not available for {program.Name}.");
                return;
            }

            var installerPath = Path.Combine(_downloadDirectory, $"{program.Name}_{SelectedArchitecture}.exe");

            if (File.Exists(installerPath))
            {
                StatusChanged?.Invoke($"[Skipped] {program.Name} is already downloaded.");
                return;
            }

            try
            {
                StatusChanged?.Invoke($"Downloading {program.Name}...");

                await _downloader.DownloadFileAsync(url, installerPath, progress =>
                {
                    StatusChanged?.Invoke($"Downloading {program.Name}: {progress}%");
                });

                if (IsInstallAfterDownload)
                {
                    StatusChanged?.Invoke($"Installing {program.Name}...");
                    _installer.InstallProgram(installerPath, program.Arguments ?? string.Empty, StatusChanged ?? (_ => { }));
                }

                StatusChanged?.Invoke($"Completed {program.Name}");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"[Error] Failed to process {program.Name}: {ex.Message}");
            }
        }
    }
}