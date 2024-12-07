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

namespace Client.Views
{
    /// <summary>
    /// Logika interakcji dla klasy DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
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
        private void SyncFilesButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Synchronizacja plików rozpoczęta!", "Synchronizacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void AdvancedSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Tak wstępnie jakby jakieś miały być :).", "Ustawienia", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
