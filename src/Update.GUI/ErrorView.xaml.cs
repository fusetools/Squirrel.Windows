using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Update.GUI
{
    /// <summary>
    /// Interaction logic for ErrorView.xaml
    /// </summary>
    public partial class ErrorView : UserControl
    {
        readonly string _logPath;
        readonly Action _onOkClicked;

        public ErrorView(string logPath, Action onOkClicked)
        {
            _logPath = logPath;
            _onOkClicked = onOkClicked;
            InitializeComponent();
            ErrorMessage.Text = "Something went wrong during installation.";
        }        

        void OkClicked(object sender, RoutedEventArgs e)
        {
            _onOkClicked();
        }

        void OnShowLog(object sender, RoutedEventArgs e)
        {
            Process.Start(_logPath);
        }
    }
}
