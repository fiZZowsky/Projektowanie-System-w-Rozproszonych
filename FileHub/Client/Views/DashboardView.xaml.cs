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
        private MetadataHandler _metadataHandler;
        private ClientService _clientService;
        private WatcherService _folderWatcher;
        private ClientGrpcServer _grpcServer;

        private string computerdId;
        private bool IsStartedAnnouncingActivity = false;
        private CancellationTokenSource _cancellationTokenSource;


        public DashboardView(MetadataHandler metadataHandler, ClientService clientService)
        {
            InitializeComponent();
            _metadataHandler = metadataHandler;
            _clientService = clientService;
            _grpcServer = new ClientGrpcServer(_clientService, _metadataHandler);

            loadMetadata();

            this.DataContext = Session.Instance;
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var response = await _clientService.LogoutUser();
            IsStartedAnnouncingActivity = false;
            StopPingTask();
            await _grpcServer.StopGrpcServer();

            if (response.Success == true)
            {
                ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView(_metadataHandler, _clientService));
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
                    _grpcServer.SetFolderWatcher(_folderWatcher);

                    if (IsStartedAnnouncingActivity == false)
                    {
                        StartPingTask();
                        _grpcServer.StartGrpcServer();
                    }

                    _metadataHandler.SaveMetadata(computerdId,FolderPathTextBox.Text);

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

        private void loadMetadata()
        {
            computerdId = _metadataHandler.GetComputerIp();
            var metadata = _metadataHandler.GetMetadataForComputer(computerdId);

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

                    await Task.Delay(180000); // 3 minuty
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