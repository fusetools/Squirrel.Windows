using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Update.GUI
{
    public class MainWindowDesignSample
    {
        public object InnerContent { get; private set; }

        public MainWindowDesignSample()
        {
            InnerContent = new ErrorView(new Exception("Foo"), () => {});
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
        Task Start(IProgress<InstallerProgress> progress);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : CustomTitlebarWindow
    {
        readonly IInstallerFactory _installerFactory;

        public MainWindow(IInstallerFactory installerFactory, CancellationToken ct)
        {
            _installerFactory = installerFactory;
            ct.Register(Exit);

            InitializeComponent();
            DataContext = this;

            var introductionView = new IntroductionView();
            introductionView.OnInstall += (sender, args) =>
            {
                var progress = StartInstallation(installerFactory);
                InnerContent = progress;
            };

            introductionView.OnCancel += (sender, args) =>
            {
                Exit();
            };            

            InnerContent = introductionView;
        }

        ProgressView StartInstallation(IInstallerFactory installerFactory)
        {            
            var p = new ProgressView();
            installerFactory
                .Start(p.Progress)
                .ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    Dispatcher.Invoke(() =>
                    {
                        InnerContent = new ErrorView(
                            t.IsCanceled 
                            ? new TaskCanceledException("Installation was canceled") 
                            : (Exception)t.Exception,
                            Exit);
                    });
                }
            });
            p.OnCancel += (o, eventArgs) => Application.Current.Shutdown();
            return p;
        }

        void Exit()
        {
            Application.Current?.Dispatcher.Invoke(() => Application.Current?.Shutdown());
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