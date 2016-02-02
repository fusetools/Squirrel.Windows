using System;
using System.Threading;
using System.Windows;

namespace Update.GUI
{
    public class MainWindowDesignSample
    {
        public object InnerContent { get; private set; }

        public MainWindowDesignSample()
        {
            InnerContent = new ErrorView();
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
        void Start(IProgress<InstallerProgress> progress);
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly IInstallerFactory _installerFactory;

        public MainWindow(IInstallerFactory installerFactory, CancellationToken ct)
        {
            _installerFactory = installerFactory;
            ct.Register(() => Application.Current?.Shutdown());

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
                Application.Current.Shutdown();
            };            

            InnerContent = introductionView;
        }

        static ProgressView StartInstallation(IInstallerFactory installerFactory)
        {            
            var p = new ProgressView();
            installerFactory.Start(p.Progress);
            p.OnCancel += (o, eventArgs) => Application.Current.Shutdown();
            return p;
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