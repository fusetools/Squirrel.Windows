using System;

namespace VCRedistsInstaller
{
    interface IInstaller
    {
        bool IsInstalled();
        void Install(bool silent, IProgress<RedistInstallationProgressEvent> progress);
    }
}