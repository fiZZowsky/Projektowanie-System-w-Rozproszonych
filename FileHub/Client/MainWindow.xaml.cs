using System.Windows;
using System.Windows.Controls;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentPresenter contentPresenter = this.FindName("ContentPresenter") as ContentPresenter;
            if (contentPresenter != null)
            {
                contentPresenter.Content = new Views.LoginView();
            }
        }
        public void ChangeView(UserControl newView)
        {
            ContentPresenter contentPresenter = this.FindName("ContentPresenter") as ContentPresenter;
            if (contentPresenter != null)
            {
                contentPresenter.Content = newView;
            }
        }
    }
}