using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp3.Data;
using WpfApp3.Models;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для OrdersWindow.xaml
    /// </summary>
    public partial class OrdersWindow : Page
    {
        private readonly User _user;
        private ObservableCollection<Order> _orders = new();

        public OrdersWindow(User user)
        {
            InitializeComponent();
            _user = user;
            RoleLabel.Text = $"Роль: {_user.Role}";
            RightsLabel.Text = (_user.Role == "Администратор" || _user.Role == "Admin")
                ? "Удаление: доступно"
                : "Удаление: запрещено";
            DeleteButton.IsEnabled = (_user.Role == "Администратор" || _user.Role == "Admin");
            AddButton.IsEnabled = !(_user.Role == "Гость" || _user.Role == "Guest");
            EditButton.IsEnabled = !(_user.Role == "Гость" || _user.Role == "Guest");
            OrdersList.SelectionChanged += OrdersList_SelectionChanged;
            LoadOrders();
        }

        private void OrdersList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {

        }

        private void LoadOrders()
        {
            using var db = new AppDbContext();
            var list = db.Orders.Include(o => o.Product).OrderBy(o => o.Id).ToList();
            _orders = new ObservableCollection<Order>(list);
            OrdersList.ItemsSource = _orders;
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
            if (OrdersList.SelectedItem is not Order selected)
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
            if (!(_user.Role == "Администратор" || _user.Role == "Admin"))
            {
                MessageBox.Show("Удаление доступно только администратору!");
                return;
            }

            if (OrdersList.SelectedItem is not Order selected)
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
