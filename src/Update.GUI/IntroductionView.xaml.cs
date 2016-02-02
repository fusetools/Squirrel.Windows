using System;
using System.Windows;
using System.Windows.Controls;

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
    }
}
