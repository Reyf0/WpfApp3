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
using WpfApp3.Models;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для ProductEditWindow.xaml
    /// </summary>
    public partial class ProductEditWindow : Window
    {
        private Product _product;

        public ProductEditWindow(Product? product = null)
        {
            InitializeComponent();
            _product = product ?? new Product();

            if (product != null)
            {
                NameBox.Text = product.Name;
                CategoryBox.Text = product.Category;
                ManufacturerBox.Text = product.Manufacturer;
                PriceBox.Text = product.Price.ToString();
                DiscountBox.Text = product.Discount.ToString();
                QuantityBox.Text = product.Quantity.ToString();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            _product.Name = NameBox.Text;
            _product.Category = CategoryBox.Text;
            _product.Manufacturer = ManufacturerBox.Text;
            _product.Price = decimal.Parse(PriceBox.Text);
            _product.Discount = int.Parse(DiscountBox.Text);
            _product.Quantity = int.Parse(QuantityBox.Text);

            if (_product.Id == 0)
                db.Products.Add(_product);
            else
                db.Products.Update(_product);

            db.SaveChanges();
            Close();
        }
    }
}
