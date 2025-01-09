using System.Windows;
using System.Windows.Controls;
using Client.Services;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Client.Utils;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private WatcherService _folderWatcher;
        private string computerdId;
        private ClientService _clientService;
        private bool IsStartedAnnouncingActivity = false;
        private CancellationTokenSource _cancellationTokenSource;

        public DashboardView()
        {
            InitializeComponent();
            loadMetadata();

            this.DataContext = Session.Instance;

            _clientService = new ClientService("http://localhost:5001");

        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var response = await _clientService.LogoutUser();
            IsStartedAnnouncingActivity = false;
            StopPingTask();

            if (response.Success == true)
            {
                ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView());
                MessageBox.Show("Pomyślnie wylogowano użytkownika", "Wylogowano", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Wystąpił błąd podczas wylogowywania użytkownika.\n{response.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            try
            {
                if (!string.IsNullOrEmpty(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
                {
                    _folderWatcher = new WatcherService(FolderPathTextBox.Text, _clientService);

                    if (IsStartedAnnouncingActivity == false)
                    {
                        StartPingTask();
                    }

                    MetadataHandler.SaveMetadata(new Metadata
                    {
                        ComputerId = computerdId,
                        SyncPath = FolderPathTextBox.Text
                    });

                    await _clientService.SendPingToServer();
                    MessageBox.Show("Synchronizacja plików rozpoczęta!", "Synchronizacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Wybierz poprawny folder przed synchronizacją.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas synchronizacji plików: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Tak wstępnie jakby jakieś miały być :).", "Ustawienia", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void loadMetadata()
        {
            computerdId = ClientService.GetComputerId();
            var metadata = MetadataHandler.GetMetadataForComputer(computerdId);

            if (metadata != null)
            {
                FolderPathTextBox.Text = metadata.SyncPath;
            }
        }

        private void StartPingTask()
        {
            IsStartedAnnouncingActivity = true;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            new Thread(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await _clientService.SendPingToServer();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Wystąpił błąd podczas synchronizacji danych: {ex.Message}.\nSynchronizuj dane ponownie.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    await Task.Delay(3000);
                }
            })
            { IsBackground = true }.Start();
        }

        public void StopPingTask()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}