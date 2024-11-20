using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuickInstall
{
    public class Downloader
    {
        public async Task DownloadFileAsync(string url, string destination, Action<int> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));

            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination path cannot be null or empty.", nameof(destination));

            try
            {
                Console.WriteLine($"Starting download: {url}");

                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength ?? -1L;
                if (contentLength == -1)
                {
                    Console.WriteLine("Warning: Content length is unknown. Progress tracking may be inaccurate.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid destination path."));

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;
                int lastProgress = 0;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    if (contentLength > 0)
                    {
                        int progress = (int)((totalBytesRead * 100) / contentLength);
                        if (progress > lastProgress)
                        {
                            lastProgress = progress;
                            progressCallback(progress);
                        }
                    }
                }

                Console.WriteLine($"Download completed successfully: {destination}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error while downloading {url}: {ex.Message}");
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File I/O error for destination {destination}: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during download from {url}: {ex.Message}");
                throw;
            }
        }
    }
}
