using System;
using Microsoft.Win32;

namespace VCRedistsInstaller
{
    class FailedToOpenKey : Exception
    {
        public FailedToOpenKey(string key) : base("Failed to open key: " + key)
        {
        }
    }

    static class RegistryExtension
    {
        ///<exception cref="FailedToOpenKey"></exception>
        public static RegistryKey Open32Or64BitSoftwareKey(string name)
        {
            var keyPath = "SOFTWARE\\" + name;
            var subKey = Registry.LocalMachine.OpenSubKey(keyPath);
            if (subKey == null)
            {
                keyPath = "SOFTWARE\\Wow6432Node\\" + name;
                subKey = Registry.LocalMachine.OpenSubKey(keyPath);
            }

            if (subKey == null)
                throw new FailedToOpenKey(keyPath);

            return subKey;
        }
    }
}