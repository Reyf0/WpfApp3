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
using System.Windows.Shapes;
using WpfApp3.Data;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            if (!db.Users.Any())
            {
                db.Users.Add(new Models.User { Login = "admin", Password = "123", FullName = "Администратор", Role = "Admin" });
                db.SaveChanges();
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Login == LoginTextBox.Text && u.Password == PasswordBox.Password);

            if (user == null)
            {
                MessageBox.Show("Неверный логин или пароль!");
                return;
            }

            var main = new MainWindow(user);
            main.Show();
            Close();
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            var user = new Models.User { FullName = "Гость", Role = "Guest" };
            var main = new MainWindow(user);
            main.Show();
            Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var reg = new RegisterWindow();
            reg.ShowDialog();
        }
    }
}
