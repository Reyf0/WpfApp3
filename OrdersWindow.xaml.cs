using Microsoft.EntityFrameworkCore;
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
using WpfApp3.Data;
using WpfApp3.Models;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для OrdersWindow.xaml
    /// </summary>
    public partial class OrdersWindow : Page
    {
        private Models.User _user;

        public OrdersWindow(Models.User user)
        {
            InitializeComponent();
            _user = user;
            RoleLabel.Text = $"Роль: {_user.Role}";
            LoadOrders();
        }

        private void LoadOrders()
        {
            using var db = new AppDbContext();
            var list = db.Orders.Include(o => o.Product).ToList();
            OrdersGrid.ItemsSource = list;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
                this.NavigationService.GoBack();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoForward == true)
                this.NavigationService.GoForward();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var w = new OrderEditWindow();
            var res = w.ShowDialog();
            if (res == true) LoadOrders();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not Models.Order selected)
            {
                MessageBox.Show("Выберите заказ для редактирования.");
                return;
            }

            var w = new OrderEditWindow(selected.Id); 
            var res = w.ShowDialog();
            if (res == true) LoadOrders();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role != "Администратор" && _user.Role != "Admin")
            {
                MessageBox.Show("Удаление доступно только администратору!");
                return;
            }

            if (OrdersGrid.SelectedItem is not Models.Order selected)
            {
                MessageBox.Show("Выберите заказ для удаления.");
                return;
            }

            var msg = $"Вы действительно хотите удалить заказ #{selected.Id}?\nТовар: {selected.Product?.Name ?? "(неизвестно)"}\nСтатус: {selected.Status}\nАдрес: {selected.Address}";
            var confirm = MessageBox.Show(msg, "Подтвердите удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var orderInDb = db.Orders.Find(selected.Id);
            if (orderInDb == null)
            {
                MessageBox.Show("Заказ не найден в базе (возможно, уже удалён).");
                LoadOrders();
                return;
            }

            db.Orders.Remove(orderInDb);
            db.SaveChanges();

            MessageBox.Show("Заказ удалён.");
            LoadOrders();
        }
    }
}
