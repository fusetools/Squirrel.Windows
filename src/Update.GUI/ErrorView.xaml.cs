using System;
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
        readonly Action _onOkClicked;

        public ErrorView(Exception exception, Action onOkClicked)
        {
            _onOkClicked = onOkClicked;
            InitializeComponent();

            if (exception != null)
            {
                var sb = new StringBuilder();
                UnpackException(sb, exception);
                ErrorMessage.Text = sb.ToString();
            }                
            else
                ErrorMessage.Text = "Something went wrong during installation.";
        }

        void UnpackException(StringBuilder sb, Exception exception)
        {
            if (exception is AggregateException)
            {
                foreach (var innerException in ((AggregateException)exception).InnerExceptions)
                {
                    UnpackException(sb, innerException);
                }                
            }
            else
            {
                sb.AppendLine(exception.Message);
            }
        }

        void OkClicked(object sender, RoutedEventArgs e)
        {
            _onOkClicked();
        }
    }
}
