using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using WpfApp3.Data;
using WpfApp3.Models;

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

            ProductCombo.DisplayMemberPath = "Name";
            ProductCombo.SelectedValuePath = "Id";

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
            var products = db.Products.OrderBy(p => p.Name).ToList();
            ProductCombo.ItemsSource = products;
            if (products.Count > 0)
                ProductCombo.SelectedIndex = 0;
        }

        private void LoadOrder(int id)
        {
            using var db = new AppDbContext();
            var o = db.Orders.Include(x => x.Product).FirstOrDefault(x => x.Id == id);
            if (o == null)
            {
                MessageBox.Show("Заказ не найден.");
                this.DialogResult = false;
                this.Close();
                return;
            }

            ProductCombo.SelectedValue = o.ProductId;

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

            if (o.DeliveryDate == default(DateTime))
                DeliveryDatePicker.SelectedDate = null;
            else
                DeliveryDatePicker.SelectedDate = o.DeliveryDate;
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
            if (string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show("Укажите адрес доставки.");
                return;
            }

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

                order.ProductId = productId;
                order.Status = status;
                order.Address = address;
                order.OrderDate = orderDate;
                order.DeliveryDate = deliveryDate ?? default;
                db.Orders.Update(order);
            }
            else
            {
                var order = new Order
                {
                    ProductId = productId,
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
