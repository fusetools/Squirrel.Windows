using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Update.GUI
{
    public class MainWindowDesignSample
    {
        public object InnerContent { get; private set; }

        public MainWindowDesignSample()
        {
            InnerContent = new ErrorView("C:\\Foo", () => {});
        }
    }

    public class InstallerProgress
    {
        public readonly int Percentage;

        public InstallerProgress(int percentage)
        {
            Percentage = percentage;
        }
    }

    public interface IInstallerFactory
    {
        Task Start(IProgress<InstallerProgress> progress, Action<string> commandChanged, CancellationToken ct);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : CustomTitlebarWindow
    {
        readonly IInstallerFactory _installerFactory;
        readonly Action _exit;
        readonly string _logPath;

        public MainWindow(IInstallerFactory installerFactory, Version version, Action exit, string logPath)
        {
            _installerFactory = installerFactory;
            _exit = exit;
            _logPath = logPath;

            InitializeComponent();
            Title += " " + version.ToString(3) + " beta installer";
            DataContext = this;

            var introductionView = new IntroductionView();
            introductionView.OnInstall += (sender, args) =>
            {
                var progress = StartInstallation(installerFactory);
                InnerContent = progress;
            };

            introductionView.OnCancel += (sender, args) =>
            {
                Cancel();
            };

            InnerContent = introductionView;
        }

        ProgressView StartInstallation(IInstallerFactory installerFactory)
        {            
            var ctSource = new CancellationTokenSource();
            var p = new ProgressView();
            installerFactory
                .Start(p.Progress, p.UpdateCommand, ctSource.Token)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception?.InnerException is OperationCanceledException)
                        {
                            Exit();
                            return;
                        }

                        Dispatcher.Invoke(() =>
                        {
                            InnerContent = new ErrorView(_logPath, Exit);
                        });
                    }
                    else
                    {
                        Exit();
                    }
                });
            
            p.OnCancel += (o, eventArgs) => ctSource.Cancel();

            return p;
        }

        void Cancel()
        {
            Exit();    
        }

        void Exit()
        {
            _exit();
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register(
                                                        "InnerContent",
                                                        typeof (object),
                                                        typeof (MainWindow),
                                                        new PropertyMetadata(default(object)));

        public object InnerContent
        {
            get { return (object) GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }
    }
}