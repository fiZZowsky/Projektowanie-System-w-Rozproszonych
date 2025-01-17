using Client.Services;
using Common;
using System.Windows;
using System.Windows.Controls;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MetadataHandler _metadataHandler;
        private ClientService _clientService;

        public MainWindow()
        {
            InitializeComponent();
            _metadataHandler = new MetadataHandler();
            _clientService = new ClientService(AppSettings.DefaultServerAddress, _metadataHandler);

            ContentPresenter contentPresenter = this.FindName("ContentPresenter") as ContentPresenter;
            if (contentPresenter != null)
            {
                contentPresenter.Content = new Views.LoginView(_metadataHandler, _clientService);
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
