using System;
using System.Reflection;
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
                    var application = new Application();
                    var installerWindow = new MainWindow(factory, token);
                    application.Run(installerWindow);    
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
