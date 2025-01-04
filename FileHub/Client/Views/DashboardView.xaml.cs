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
using Microsoft.Win32;
using Client.Services;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private WatcherService _folderWatcher;
        private string computerdId;

        public DashboardView()
        {
            InitializeComponent();
            loadMetadata();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView());
        }
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Wybierz folder do synchronizacji",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                FolderPathTextBox.Text = selectedPath;
            }
        }
        private async void SyncFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                var clientService = new ClientService("http://localhost:5000");
                _folderWatcher = new WatcherService(FolderPathTextBox.Text, clientService);

                MetadataHandler.SaveMetadata(new Metadata
                {
                    ComputerId = computerdId,
                    SyncPath = FolderPathTextBox.Text
                });

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

        private void loadMetadata ()
        {
            computerdId = ClientService.GetComputerId();
            var metadata = MetadataHandler.GetMetadataForComputer(computerdId);

            if (metadata != null)
            {
                FolderPathTextBox.Text = metadata.SyncPath;
            }
        }
    }
}
