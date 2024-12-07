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
