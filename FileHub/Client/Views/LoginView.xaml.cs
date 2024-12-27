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
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameTextBox.Text == "admin" && PasswordBox.Password == "root123")
            {
                Session.UserId = "admin";
                Session.Username = "admin";
                ((MainWindow)Application.Current.MainWindow).ChangeView(new DashboardView());
            }
            else
            {
                MessageBox.Show("Nieprawidłowe dane logowania.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ChangeView(new RegisterView());
        }
    }
}
