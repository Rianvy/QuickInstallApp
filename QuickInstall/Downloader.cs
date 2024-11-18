namespace QuickInstall
{
    class Downloader
    {
        public async Task DownloadFileAsync(string url, string destination, Action<int> progressCallback)
        {
            try
            {
                Console.WriteLine($"Downloading: {url}...");

                using var httpClient = new HttpClient();

                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength ?? -1L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    if (contentLength != -1)
                    {
                        int progress = (int)((totalBytesRead * 100) / contentLength);
                        progressCallback(progress);
                    }
                }

                Console.WriteLine($"Downloaded to: {destination}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file from {url}: {ex.Message}");
            }
        }
    }
}
