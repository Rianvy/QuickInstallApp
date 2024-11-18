using System.Diagnostics;

namespace QuickInstall
{
    class Installer
    {
        public void InstallProgram(string installerPath, string arguments, Action<string> statusCallback)
        {
            try
            {
                statusCallback($"Installing {Path.GetFileName(installerPath)}...");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = arguments,
                        UseShellExecute = true
                    }
                };
                process.Start();
                process.WaitForExit();
                statusCallback($"{Path.GetFileName(installerPath)} installed.");
            }
            catch (Exception ex)
            {
                statusCallback($"Error installing {Path.GetFileName(installerPath)}: {ex.Message}");
            }
        }
    }
}
