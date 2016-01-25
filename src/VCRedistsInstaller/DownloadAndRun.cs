using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace VCRedistsInstaller
{
    class DownloadAndRun
    {
        public static async Task<int> Do(Uri applicationLocation, string name, IEnumerable<string> arguments, IProgress<RedistInstallationProgressEvent> progress)
        {
            var downloadExpectedPercentOfTotal = 0.7;

            var tempExeLocation = Path.Combine(Path.GetTempPath(), name + ".exe");
            using (var webClient = new WebClient())
            {
                var threadResult = new TaskCompletionSource<object>();
                webClient.DownloadFileCompleted += (sender, args) =>
                {
                    if(args.Error != null)
                        threadResult.TrySetException(args.Error);
                    else if(args.Cancelled)
                        threadResult.TrySetCanceled();
                    else
                        threadResult.SetResult(args.UserState);
                };

                webClient.DownloadProgressChanged += (sender, args) =>
                {
                    progress.ReportProgress((int) (args.ProgressPercentage * downloadExpectedPercentOfTotal));
                };

                webClient.DownloadFileAsync(applicationLocation, tempExeLocation);
                await threadResult.Task;
            }

            var startInfo = new ProcessStartInfo(tempExeLocation, string.Join(" ", arguments))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Verb = "runas",
            };

            var process = Process.Start(startInfo);
            if (process == null)
                throw new FailedToStartProcess(tempExeLocation);

            var output = process.StandardOutput.ReadToEnd();
            var errors = process.StandardError.ReadToEnd();

            process.WaitForExit();
            progress.ReportProgress(100);

            return await Task.FromResult(process.ExitCode);
        }
    }
}