using System;
using System.Collections.Generic;

namespace VCRedistsInstaller
{
    class InstallVCRedist2010_x86 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\VisualStudio\\10.0\\VC\\VCRedist\\x86");
                return (int)key.GetValue("Installed", false) == 1;
            }
            catch (FailedToOpenKey)
            {
                return false;
            }
        }

        public void Install(bool silent, IProgress<RedistInstallationProgressEvent> progress)
        {
            var arguments = new List<string>()
            {
                "/norestart",
                "/passive"
            };
            if (silent)
                arguments.Add("/q");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2010_x86", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }

    class InstallVCRedist2010_x64 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\VisualStudio\\10.0\\VC\\VCRedist\\x64");
                return (int) key.GetValue("Installed", false) == 1;
            }
            catch (FailedToOpenKey)
            {
                return false;
            }
        }

        public void Install(bool silent, IProgress<RedistInstallationProgressEvent> progress)
        {
            var arguments = new List<string>()
            {
                "/norestart",
                "/passive"
            };
            if (silent)
                arguments.Add("/q");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2010_x64", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }
}
