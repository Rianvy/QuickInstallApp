using System.Diagnostics;

namespace QuickInstall
{
    public class Installer
    {
        public void InstallProgram(string installerPath, string arguments, Action<string> statusCallback)
        {
            if (string.IsNullOrWhiteSpace(installerPath))
                throw new ArgumentException("Installer path cannot be null or empty.", nameof(installerPath));

            if (statusCallback == null)
                throw new ArgumentNullException(nameof(statusCallback), "Status callback cannot be null.");

            try
            {
                if (!File.Exists(installerPath))
                {
                    throw new FileNotFoundException($"Installer not found at: {installerPath}");
                }

                string fileName = Path.GetFileName(installerPath);
                statusCallback($"Starting installation: {fileName}...");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = arguments,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    },
                    EnableRaisingEvents = true
                };

                process.Exited += (sender, e) =>
                {
                    statusCallback($"Installation completed: {fileName} (Exit code: {process.ExitCode})");
                    process.Dispose();
                };

                process.Start();
                process.WaitForExit();
            }
            catch (FileNotFoundException ex)
            {
                statusCallback($"File error: {ex.Message}");
                throw;
            }
            catch (InvalidOperationException ex)
            {
                statusCallback($"Process error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                statusCallback($"Unexpected error during installation: {ex.Message}");
                throw;
            }
        }
    }
}