using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace VCRedistsInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            var weAreUACElevated = IsAdmin();
            if (args.Length == 1)
            {
                var log = File.OpenWrite(args[0]);
                var logWriter = new StreamWriter(log);
                Console.SetOut(logWriter);
            }

            var installers = new IInstaller[]
            {
                new InstallVCRedist2010_x86(),
                new InstallVCRedist2010_x64(),

                new InstallVCRedists2012_x86(), 
                new InstallVCRedists2012_x64(), 

                new InstallVCRedists2013_x86(), 
                new InstallVCRedists2013_x64(), 
            };

            try
            {
                foreach (var installer in installers)
                {
                    if (!installer.IsInstalled())
                    {
                        if (!weAreUACElevated)
                        {
                            EvaluateOurself(Path.GetTempFileName());
                            break;
                        }
                        else
                        {
                            installer.Install(true,
                                              new Progress<RedistInstallationProgressEvent>(a =>
                                              {
                                                  Console.WriteLine(a.Percentage);
                                              }));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                Environment.Exit(1);
            }
            finally
            {
                Console.Out.Flush();
            }
        }

        static void EvaluateOurself(string logOutLocation)
        {
            Console.WriteLine(logOutLocation);
            var proc = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Assembly.GetExecutingAssembly().Location,
                Arguments = logOutLocation,
                Verb = "runas"
            };
            try
            {
                var p = Process.Start(proc);
                if(p == null)
                    throw new Exception("Failed to start myself in elevation mode");

                p.WaitForExit();

                Console.Write(File.ReadAllText(logOutLocation));
            }
            catch (Exception)
            {
                throw new Exception("User refused to allow privileges elevation");
            }
        }

        static bool IsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
