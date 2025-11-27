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
using System.Windows.Shapes;
using WpfApp3.Data;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для OrderEditWindow.xaml
    /// </summary>
    public partial class OrderEditWindow : Window
    {
        private int? _orderId; 

        public OrderEditWindow(int? orderId = null)
        {
            InitializeComponent();
            _orderId = orderId;
            LoadProducts();
            if (_orderId.HasValue)
            {
                LoadOrder(_orderId.Value);
            }
            else
            {
                OrderDatePicker.SelectedDate = DateTime.Now;
            }
        }

        private void LoadProducts()
        {
            using var db = new AppDbContext();
            ProductCombo.ItemsSource = db.Products.ToList();
            if (ProductCombo.Items.Count > 0)
                ProductCombo.SelectedIndex = 0;
        }

        private void LoadOrder(int id)
        {
            using var db = new AppDbContext();
            var o = db.Orders.Include(x => x.ProductName).FirstOrDefault(x => x.Id == id);
            if (o == null)
            {
                MessageBox.Show("Заказ не найден.");
                this.DialogResult = false;
                this.Close();
                return;
            }

            ProductCombo.SelectedValue = o.Id;
            foreach (var item in StatusCombo.Items)
            {
                if ((item as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() == o.Status)
                {
                    StatusCombo.SelectedItem = item;
                    break;
                }
            }

            AddressBox.Text = o.Address;
            OrderDatePicker.SelectedDate = o.OrderDate;
            DeliveryDatePicker.SelectedDate = o.DeliveryDate == default ? null : (DateTime?)o.DeliveryDate;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ProductCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар.");
                return;
            }

            if (StatusCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус.");
                return;
            }

            var productId = (int)ProductCombo.SelectedValue;
            var status = (StatusCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Обработка";
            var address = AddressBox.Text.Trim();
            var orderDate = OrderDatePicker.SelectedDate ?? DateTime.Now;
            var deliveryDate = DeliveryDatePicker.SelectedDate;

            using var db = new AppDbContext();

            if (_orderId.HasValue)
            {
                var order = db.Orders.Find(_orderId.Value);
                if (order == null)
                {
                    MessageBox.Show("Заказ не найден в БД.");
                    return;
                }

                order.Id = productId;
                order.Status = status;
                order.Address = address;
                order.OrderDate = orderDate;
                order.DeliveryDate = deliveryDate ?? default;
                db.Orders.Update(order);
            }
            else
            {
                var order = new Models.Order
                {
                    Id = productId,
                    Status = status,
                    Address = address,
                    OrderDate = orderDate,
                    DeliveryDate = deliveryDate ?? default
                };
                db.Orders.Add(order);
            }

            db.SaveChanges();
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
