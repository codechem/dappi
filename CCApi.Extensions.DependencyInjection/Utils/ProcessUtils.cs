using System.Diagnostics;

namespace CCApi.Extensions.DependencyInjection.Utils
{
    public static class ProcessUtils
    {
        public static async Task KillProcessesUsingUrls(string urls)
        {
            var ports = new HashSet<int>();
            var urlList = urls.Split(';');

            foreach (var url in urlList)
            {
                if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                {
                    ports.Add(uri.Port);
                }
            }

            foreach (var port in ports)
            {
                await KillProcessUsingPort(port);
            }
        }

        private static async Task KillProcessUsingPort(int port)
        {
            try
            {
                Console.WriteLine($"Checking for processes using port {port}...");

                var pids = await GetProcessIdsUsingPort(port);

                if (pids.Any())
                {
                    foreach (var pid in pids)
                    {
                        if (pid != Environment.ProcessId)
                        {
                            await KillProcessById(pid, port);
                        }
                        else
                        {
                            Console.WriteLine($"Skipping current process {pid} on port {port}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No processes found using port {port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking port {port}: {ex.Message}");
            }
        }

        private static async Task<List<int>> GetProcessIdsUsingPort(int port)
        {
            var pids = new List<int>();

            var processInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "bash",
                Arguments = OperatingSystem.IsWindows()
                    ? $"/c netstat -ano | findstr :{port}"
                    : $"-c \"lsof -ti:{port}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrEmpty(output)) return pids;

            if (OperatingSystem.IsWindows())
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && int.TryParse(parts[^1], out int pid))
                    {
                        pids.Add(pid);
                    }
                }
            }
            else
            {
                var pidStrings = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pidStr in pidStrings)
                {
                    if (int.TryParse(pidStr.Trim(), out int pid))
                    {
                        pids.Add(pid);
                    }
                }
            }

            return pids.Distinct().ToList();
        }

        private static async Task KillProcessById(int pid, int port)
        {
            try
            {
                var processToKill = Process.GetProcessById(pid);
                processToKill.Kill();
                await processToKill.WaitForExitAsync();
                Console.WriteLine($"✓ Killed process {pid} using port {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to kill process {pid}: {ex.Message}");
            }
        }
    }
}