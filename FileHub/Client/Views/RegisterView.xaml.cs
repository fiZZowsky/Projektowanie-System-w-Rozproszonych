using Client.Services;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        private MetadataHandler _metadataHandler;
        private ClientService _clientService;
        public RegisterView(MetadataHandler metadataHandler, ClientService clientService)
        {
            InitializeComponent();
            _metadataHandler = metadataHandler;
            _clientService = clientService;
        }
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Hasła nie są takie same.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var response = await _clientService.RegisterUserAsync(UsernameTextBox.Text, PasswordBox.Password);

            if (response.Success)
            {
                MessageBox.Show("Rejestracja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView(_metadataHandler, _clientService));
            }
            else
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView(_metadataHandler, _clientService));
        }
    }
}
