using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp3.Models;

namespace WpfApp3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private User _user;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            _user = user;
            UserInfo.Text = $"{_user.FullName} ({_user.Role})";
            MainFrame.Navigate(new ProductsWindow(_user));
        }

        private void Products_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new ProductsWindow(_user));

        private void Orders_Click(object sender, RoutedEventArgs e)
            => MainFrame.Navigate(new OrdersWindow(_user));

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }
    }
}