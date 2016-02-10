using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Update.GUI
{
    /// <summary>
    /// Interaction logic for IntroductionView.xaml
    /// </summary>
    public partial class IntroductionView : UserControl
    {
        public event EventHandler OnInstall;
        public event EventHandler OnCancel;

        public IntroductionView()
        {
            InitializeComponent();
        }

        void CancelClicked(object sender, RoutedEventArgs e)
        {
            OnCancel?.Invoke(sender, e);
        }

        void InstallClicked(object sender, RoutedEventArgs e)
        {
            OnInstall?.Invoke(sender, e);
        }

        void GotoLicensePage(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        void AgreedToLicense(object sender, RoutedEventArgs e)
        {
            var checkBox = ((CheckBox) e.Source);
            if(checkBox.IsChecked.HasValue)
                InstallButton.IsEnabled = checkBox.IsChecked.Value;
        }
    }
}
