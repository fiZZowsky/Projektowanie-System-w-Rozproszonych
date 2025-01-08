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
        public RegisterView()
        {
            InitializeComponent();
        }
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Hasła nie są takie same.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var clientService = new ClientService("http://localhost:5000");
            var response = await clientService.RegisterUserAsync(UsernameTextBox.Text, PasswordBox.Password);

            if (response.Success)
            {
                MessageBox.Show("Rejestracja zakończona sukcesem!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);

                ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView());
            }
            else
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new LoginView());
        }
    }
}
