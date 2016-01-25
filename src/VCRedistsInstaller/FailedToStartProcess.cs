using System;

namespace VCRedistsInstaller
{
    class FailedToStartProcess : Exception
    {
        public FailedToStartProcess(string name) : base("Failed to start process " + name)
        {            
        }
    }
}