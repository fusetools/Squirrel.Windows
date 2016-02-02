using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Update.GUI
{
    public class InstallerWindow
    {
        public static async Task ShowWindow(TimeSpan initialDelay, CancellationToken token)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() => 
            {
                try
                {
                    var installerWindow = new MainWindow();
                    (new Application()).Run(installerWindow);          
                }
                finally 
                {
                    tcs.TrySetResult(new object());
                }                
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            await tcs.Task;
        }
    }
}
