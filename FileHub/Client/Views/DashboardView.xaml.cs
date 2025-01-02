using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Client.Services;
using System.IO;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private WatcherService _folderWatcher;
        public DashboardView()
        {
            InitializeComponent();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView());
        }
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Wybierz folder",
                Filter = "Foldery|.",
                CheckFileExists = false,
                FileName = "Wybierz ten folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                FolderPathTextBox.Text = selectedPath;
            }
        }
        private async void SyncFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                var clientService = new ClientService("http://localhost:5000");
                _folderWatcher = new WatcherService(FolderPathTextBox.Text, clientService);

                MessageBox.Show("Synchronizacja plików rozpoczęta!", "Synchronizacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Wybierz poprawny folder przed synchronizacją.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Tak wstępnie jakby jakieś miały być :).", "Ustawienia", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
