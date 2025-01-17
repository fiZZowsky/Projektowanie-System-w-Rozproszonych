using Client.Services;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        private MetadataHandler _metadataHandler;
        private ClientService _clientService;
        public LoginView(MetadataHandler metadataHandler, ClientService clientService)
        {
            InitializeComponent();
            _metadataHandler = metadataHandler;
            _clientService = clientService;
        }
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var response = await _clientService.LoginUserAsync(UsernameTextBox.Text, PasswordBox.Password);

            if (response.Success)
            {
                ((MainWindow)Application.Current.MainWindow).ChangeView(new DashboardView(_metadataHandler, _clientService));
            }
            else
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new RegisterView(_metadataHandler, _clientService));
        }
    }
}
