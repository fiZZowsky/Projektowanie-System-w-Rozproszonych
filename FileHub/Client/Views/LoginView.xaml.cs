using Client.Services;
using Client.Utils;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }
        private async Task LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var clientService = new ClientService("http://localhost:5000");
            var response = await clientService.LoginUserAsync(UsernameTextBox.Text, PasswordBox.Password);

            if (response.Success)
            {
                ((MainWindow)Application.Current.MainWindow).ChangeView(new DashboardView());
            }
            else
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new RegisterView());
        }
    }
}
