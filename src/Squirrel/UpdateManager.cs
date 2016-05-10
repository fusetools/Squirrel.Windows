using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using NuGet;
using Splat;
using Squirrel.Shell;

namespace Squirrel
{
    public sealed partial class UpdateManager : IUpdateManager, IEnableLogger
    {
        readonly string rootAppDirectory;
        readonly string applicationName;
        readonly IFileDownloader urlDownloader;
        readonly string updateUrlOrPath;

        IDisposable updateLock;

        public UpdateManager(string urlOrPath, 
            string applicationName = null,
            string rootDirectory = null,
            IFileDownloader urlDownloader = null)
        {
            Contract.Requires(!String.IsNullOrEmpty(urlOrPath));
            Contract.Requires(!String.IsNullOrEmpty(applicationName));

            updateUrlOrPath = urlOrPath;
            this.applicationName = applicationName ?? UpdateManager.getApplicationName();
            this.urlDownloader = urlDownloader ?? new FileDownloader();

            this.rootAppDirectory = rootDirectory ?? Path.Combine(GetLocalAppDataDirectory(), "Fusetools", this.applicationName, "App");
            this.Log().Info(this.rootAppDirectory);
        }

        public async Task<UpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates = false, Action<int> progress = null)
        {
            var checkForUpdate = new CheckForUpdateImpl(rootAppDirectory);

            await acquireUpdateLock();
            return await checkForUpdate.CheckForUpdate(Utility.LocalReleaseFileForAppDir(rootAppDirectory), updateUrlOrPath, ignoreDeltaUpdates, progress, urlDownloader);
        }

        public async Task DownloadReleases(IEnumerable<ReleaseEntry> releasesToDownload, Action<int> progress = null)
        {
            var downloadReleases = new DownloadReleasesImpl(rootAppDirectory);
            await acquireUpdateLock();

            await downloadReleases.DownloadReleases(updateUrlOrPath, releasesToDownload, progress, urlDownloader);
        }

        public async Task<string> ApplyReleases(UpdateInfo updateInfo, Action<int> progress = null)
        {
            var applyReleases = new ApplyReleasesImpl(rootAppDirectory);
            await acquireUpdateLock();

            return await applyReleases.ApplyReleases(updateInfo, false, false, progress);
        }

        public async Task FullInstall(bool silentInstall, ProgressSource progress)
        {
            var partialProgress = new Action<int, int, int>((p, offset, ofTotalProgress) =>
            {
                progress.Raise(offset + (int)((double)p * (double)ofTotalProgress / 100.0));
            });

            progress.RaiseCommand("Uninstalling current version...");
            UninstallOldInstallerIfFound();
            partialProgress(100, 0, 15);

            progress.RaiseCommand("Installing VC redists...");
            InstallRedists();
            partialProgress(100, 15, 15);

            progress.RaiseCommand("Extracting Fuse...");

            var updateInfo = await CheckForUpdate();
            await DownloadReleases(updateInfo.ReleasesToApply);

            var applyReleases = new ApplyReleasesImpl(rootAppDirectory);
            await acquireUpdateLock();

            await applyReleases.ApplyReleases(updateInfo, silentInstall, true, (p) => partialProgress(p, 30, 70));
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        void UninstallOldInstallerIfFound()
        {
            var uninstallCommand = GetUninstallStringForOldInstaller().FirstOrDefault();
            if (uninstallCommand == null)
                return;

            var parts = uninstallCommand.Split('/');
            var exePart = parts.FirstOrDefault();
            if (exePart == null)
                return;

            exePart = exePart.Trim().Trim('"');
            var arguments = parts
                .Skip(1)
                .Select(s => "/" + s)
                .Concat(new[] { "/passive", "/norestart" });

            var p = Process.Start(new ProcessStartInfo()
            {
                FileName = exePart,
                Arguments = string.Join(" ", arguments),
                Verb = "runas"
            });

            if(p == null)
                throw new Exception("Failed to start uninstaller of Fuse. Please uninstall it manually");

            SetParent(p.MainWindowHandle, Process.GetCurrentProcess().MainWindowHandle);

            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                throw new Exception("Failed to uninstall old Fuse. Please uninstall it manually.");
            }
        }

        public IEnumerable<string> GetUninstallStringForOldInstaller()
        {
            var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var key = Registry.LocalMachine.OpenSubKey(registryKey);
            if (key != null)
            {
                foreach (var subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                {
                    var upgradeCode = subkey.GetValue("BundleUpgradeCode") as IEnumerable<string>;
                    if (upgradeCode != null && Guid.Parse(upgradeCode.First()) == Guid.Parse("{76896271-B971-4FBD-8775-3BE4F72CAEA5}"))
                    {
                        var installLocation = subkey.GetValue("UninstallString") as string;
                        yield return installLocation;
                    }
                }
                key.Close();
            }

            registryKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            key = Registry.LocalMachine.OpenSubKey(registryKey);
            if (key != null)
            {
                foreach (var subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
                {
                    var upgradeCode = subkey.GetValue("BundleUpgradeCode") as IEnumerable<string>;
                    if (upgradeCode != null && Guid.Parse(upgradeCode.First()) == Guid.Parse("{76896271-B971-4FBD-8775-3BE4F72CAEA5}"))
                    {
                        var installLocation = subkey.GetValue("UninstallString") as string;
                        yield return installLocation;
                    }
                }
                key.Close();
            }
        }


        void InstallRedists()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var p = Process.Start(new ProcessStartInfo(Path.Combine(assemblyLocation, "VCRedistsInstaller.exe"))
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if(p == null)
                throw new Exception("Failed to start VC redist installer");

            var outTask = Task.Run(() => p.StandardOutput.ReadToEnd());
            var errTask = Task.Run(() => p.StandardError.ReadToEnd());

            Task.WaitAll(outTask, errTask);

            p.WaitForExit();

            if(p.ExitCode != 0)
                throw new Exception("Failed to install VC redists");
        }

        public async Task FullUninstall()
        {
            var applyReleases = new ApplyReleasesImpl(rootAppDirectory);
            await acquireUpdateLock();

            this.KillAllExecutablesBelongingToPackage();
            await applyReleases.FullUninstall();
        }

        public Task<RegistryKey> CreateUninstallerRegistryEntry(string uninstallCmd, string quietSwitch)
        {
            var installHelpers = new InstallHelperImpl(applicationName, rootAppDirectory);
            return installHelpers.CreateUninstallerRegistryEntry(uninstallCmd, quietSwitch);
        }

        public Task<RegistryKey> CreateUninstallerRegistryEntry()
        {
            var installHelpers = new InstallHelperImpl(applicationName, rootAppDirectory);
            return installHelpers.CreateUninstallerRegistryEntry();
        }

        public void RemoveUninstallerRegistryEntry()
        {
            var installHelpers = new InstallHelperImpl(applicationName, rootAppDirectory);
            installHelpers.RemoveUninstallerRegistryEntry();
        }

        public void CreateShortcutsForExecutable(string exeName, ShortcutLocation locations, bool updateOnly, string programArguments = null, string icon = null)
        {
            var installHelpers = new ApplyReleasesImpl(rootAppDirectory);
            installHelpers.CreateShortcutsForExecutable(exeName, locations, updateOnly, programArguments, icon);
        }

        public Dictionary<ShortcutLocation, ShellLink> GetShortcutsForExecutable(string exeName, ShortcutLocation locations, string programArguments = null)
        {
            var installHelpers = new ApplyReleasesImpl(rootAppDirectory);
            return installHelpers.GetShortcutsForExecutable(exeName, locations, programArguments);
        }


        public void RemoveShortcutsForExecutable(string exeName, ShortcutLocation locations)
        {
            var installHelpers = new ApplyReleasesImpl(rootAppDirectory);
            installHelpers.RemoveShortcutsForExecutable(exeName, locations);
        }

        public SemanticVersion CurrentlyInstalledVersion(string executable = null)
        {
            executable = executable ??
                Path.GetDirectoryName(typeof(UpdateManager).Assembly.Location);

            if (!executable.StartsWith(rootAppDirectory, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            var appDirName = executable.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .FirstOrDefault(x => x.StartsWith("app-", StringComparison.OrdinalIgnoreCase));

            if (appDirName == null) return null;
            return appDirName.ToSemanticVersion();
        }

        public void KillAllExecutablesBelongingToPackage()
        {
            var installHelpers = new InstallHelperImpl(applicationName, rootAppDirectory);
            installHelpers.KillAllProcessesBelongingToPackage();
        }

        public string ApplicationName {
            get { return applicationName; }
        }

        public string RootAppDirectory {
            get { return rootAppDirectory; }
        }

        public bool IsInstalledApp {
            get { return Assembly.GetExecutingAssembly().Location.StartsWith(RootAppDirectory, StringComparison.OrdinalIgnoreCase); }
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref updateLock, null);
            if (disp != null) {
                disp.Dispose();
            }
        }

        static bool exiting = false;
        public static void RestartApp(string exeToStart = null, string arguments = null)
        { 
            // NB: Here's how this method works:
            //
            // 1. We're going to pass the *name* of our EXE and the params to 
            //    Update.exe
            // 2. Update.exe is going to grab our PID (via getting its parent), 
            //    then wait for us to exit.
            // 3. We exit cleanly, dropping any single-instance mutexes or 
            //    whatever.
            // 4. Update.exe unblocks, then we launch the app again, possibly 
            //    launching a different version than we started with (this is why
            //    we take the app's *name* rather than a full path)

            exeToStart = exeToStart ?? Path.GetFileName(Assembly.GetEntryAssembly().Location);
            var argsArg = arguments != null ?
                String.Format("-a \"{0}\"", arguments) : "";

            exiting = true;

            Process.Start(getUpdateExe(), String.Format("--processStartAndWait {0} {1}", exeToStart, argsArg));

            // NB: We have to give update.exe some time to grab our PID, but
            // we can't use WaitForInputIdle because we probably don't have
            // whatever WaitForInputIdle considers a message loop.
            Thread.Sleep(500);
            Environment.Exit(0);
        }

        public static string GetLocalAppDataDirectory(string assemblyLocation = null)
        {
            // Try to divine our our own install location via reading tea leaves
            //
            // * We're Update.exe, running in the app's install folder
            // * We're Update.exe, running on initial install from SquirrelTemp
            // * We're a C# EXE with Squirrel linked in

            var assembly = Assembly.GetEntryAssembly();
            if (assemblyLocation == null && assembly == null) {
                // dunno lol
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            assemblyLocation = assemblyLocation ?? assembly.Location;

            if (Path.GetFileName(assemblyLocation).Equals("update.exe", StringComparison.OrdinalIgnoreCase)) {
                // NB: Both the "SquirrelTemp" case and the "App's folder" case 
                // mean that the root app dir is one up
                var oneFolderUpFromAppFolder = Path.Combine(Path.GetDirectoryName(assemblyLocation), "..");
                return Path.GetFullPath(oneFolderUpFromAppFolder);
            }

            var twoFoldersUpFromAppFolder = Path.Combine(Path.GetDirectoryName(assemblyLocation), "..\\..");
            return Path.GetFullPath(twoFoldersUpFromAppFolder);
        }

        ~UpdateManager()
        {
            if (updateLock != null && !exiting) {
                throw new Exception("You must dispose UpdateManager!");
            }
        }

        Task<IDisposable> acquireUpdateLock()
        {
            if (updateLock != null) return Task.FromResult(updateLock);

            return Task.Run(() => {
                var key = Utility.CalculateStreamSHA1(new MemoryStream(Encoding.UTF8.GetBytes(rootAppDirectory)));

                IDisposable theLock;
                try {
                    theLock = ModeDetector.InUnitTestRunner() ?
                        Disposable.Create(() => {}) : new SingleGlobalInstance(key, TimeSpan.FromMilliseconds(2000));
                } catch (TimeoutException) {
                    throw new TimeoutException("Couldn't acquire update lock, another instance may be running updates");
                }

                var ret = Disposable.Create(() => {
                    theLock.Dispose();
                    updateLock = null;
                });

                updateLock = ret;
                return ret;
            });
        }

        static string getApplicationName()
        {
            var fi = new FileInfo(getUpdateExe());
            return fi.Directory.Name;
        }

        static string getUpdateExe()
        {
            var assembly = Assembly.GetEntryAssembly();

            // Are we update.exe?
            if (assembly != null &&
                Path.GetFileName(assembly.Location).Equals("update.exe", StringComparison.OrdinalIgnoreCase) &&
                assembly.Location.IndexOf("app-", StringComparison.OrdinalIgnoreCase) == -1 &&
                assembly.Location.IndexOf("SquirrelTemp", StringComparison.OrdinalIgnoreCase) == -1) {
                return Path.GetFullPath(assembly.Location);
            }

            assembly = Assembly.GetExecutingAssembly();

            var updateDotExe = Path.Combine(Path.GetDirectoryName(assembly.Location), "..\\Update.exe");
            var target = new FileInfo(updateDotExe);

            if (!target.Exists) throw new Exception("Update.exe not found, not a Squirrel-installed app?");
            return target.FullName;
        }
    }
}
