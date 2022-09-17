using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace TCN_Fits_Preview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();


            _viewModel = new ViewModel();
            DataContext = _viewModel;

            _viewModel.ActivePath = Settings.Default.ActivePath;
            _viewModel.DestinationPath = Settings.Default.DestinationPath;
            _viewModel.DestinationIP = Settings.Default.DestinationIP;
            passwordBox.Password = Settings.Default.Password;
        }

        private void mBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                _viewModel.ActivePath = dialog.SelectedPath;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            {
                Settings.Default.Password = ((PasswordBox)sender).Password;
                _viewModel.UserPassword = ((PasswordBox)sender).Password;
            }
        }

        private void mActiveBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!_viewModel.IsActive)
            {
                _viewModel.Start();
            }
            else
            {
                _viewModel.Stop();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}
