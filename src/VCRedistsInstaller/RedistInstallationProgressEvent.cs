using System;

namespace VCRedistsInstaller
{
    class RedistInstallationProgressEvent
    {
        public readonly int Percentage;

        public RedistInstallationProgressEvent(int percentage)
        {
            Percentage = percentage;
        }
    }

    static class RedistInstallationProgressExtensions
    {
        public static void ReportProgress(this IProgress<RedistInstallationProgressEvent> progress, int percentage)
        {
            progress.Report(new RedistInstallationProgressEvent(percentage));
        }
    }
}