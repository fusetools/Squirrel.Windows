using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Update.GUI
{
    public class InstallerWindow
    {
        public static async Task ShowWindow(CancellationToken token, IInstallerFactory factory)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() => 
            {
                try
                {
                    var installerWindow = new MainWindow(factory, token);
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
