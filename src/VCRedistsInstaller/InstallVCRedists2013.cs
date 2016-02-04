using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCRedistsInstaller
{
    class InstallVCRedists2013_x86 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\Windows\\CurrentVersion\\Uninstall\\{f65db027-aff3-4070-886a-0d87064aabb1}");
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
            arguments.Add(silent ? "/quiet" : "/passive");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2013_x86", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }

    class InstallVCRedists2013_x64 : IInstaller
    {
        readonly Uri _downloadLocation = new Uri("http://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x64.exe");

        public bool IsInstalled()
        {
            try
            {
                var key = RegistryExtension.Open32Or64BitSoftwareKey("Microsoft\\Windows\\CurrentVersion\\Uninstall\\{050d4fc8-5d48-4b8f-8972-47c82c46020f}");
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
            arguments.Add(silent ? "/quiet" : "/passive");

            DownloadAndRun.Do(_downloadLocation, "VCRedist2013_x64", arguments, progress).Wait();

            // We should probably expect the exit code, to check for restart requirement.
        }
    }
}
