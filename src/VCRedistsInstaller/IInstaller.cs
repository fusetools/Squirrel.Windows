using System;

namespace VCRedistsInstaller
{
    interface IInstaller
    {
        bool IsInstalled();
        void Install(bool silent, IProgress<RedistInstallationProgressEvent> progress);
    }

    class ExitWithCode : Exception
    {
        public readonly int ExitCode;

        public ExitWithCode(int exitCode)
        {
            ExitCode = exitCode;
        }
    }
}