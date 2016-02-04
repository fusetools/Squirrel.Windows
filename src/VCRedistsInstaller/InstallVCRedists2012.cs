using System;
using System.Collections.Generic;

namespace VCRedistsInstaller
{
    class InstallVCRedists2012_x86 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\VisualStudio\\11.0\\VC\\Runtimes\\x86");
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
                "/norestart"
            };
            arguments.Add(silent ? "/q" : "/passive");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2012_x86", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }

    class InstallVCRedists2012_x64 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\VisualStudio\\11.0\\VC\\Runtimes\\x64");
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
                "/norestart"
            };
            arguments.Add(silent ? "/q" : "/passive");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2012_x64", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }
}
