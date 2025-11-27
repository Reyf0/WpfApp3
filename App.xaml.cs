using System.Configuration;
using System.Data;
using System.Windows;
using WpfApp3.Data;

namespace WpfApp3
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();

                if (!db.Users.Any())
                {
                    db.Users.Add(new Models.User
                    {
                        FullName = "Администратор системы",
                        Login = "admin",
                        Password = "12345",
                        Role = "Администратор"
                    });
                    db.SaveChanges();
                }
            }

            var login = new LoginWindow();
            login.Show();
        }
    }

}
