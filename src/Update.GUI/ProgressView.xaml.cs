using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Update.GUI
{
    /// <summary>
    /// Interaction logic for ProgressView.xaml
    /// </summary>
    public partial class ProgressView : UserControl
    {
        public event EventHandler OnCancel;
        public readonly Progress<InstallerProgress> Progress;

        void OnProgress(InstallerProgress installerProgress)
        {
            ProgressBar.Dispatcher.InvokeAsync(() => ProgressBar.Value = installerProgress.Percentage);
        }

        public void UpdateCommand(string command)
        {
            CurrentCommand.Dispatcher.InvokeAsync(() => CurrentCommand.Text = command);
        }

        public ProgressView()
        {
            InitializeComponent();
            Progress = new Progress<InstallerProgress>(OnProgress);            
        }

        void CancelClicked(object sender, RoutedEventArgs e)
        {
            OnCancel?.Invoke(sender, e);
        }
    }
}
