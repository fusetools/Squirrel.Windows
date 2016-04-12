using System;

namespace Squirrel
{
    public class ProgressSource
    {
        public event EventHandler<int> Progress;
        public event EventHandler<string> Command;

        public void RaiseCommand(string name)
        {
            Command?.Invoke(this, name);
        }

        public void Raise(int i)
        {
            Progress?.Invoke(this, i);
        }
    }
}