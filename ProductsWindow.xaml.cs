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
using WpfApp3.Models;
using WpfApp3.Data;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для ProductsWindow.xaml
    /// </summary>
    public partial class ProductsWindow : Page
    {
        public User _user;

        public ProductsWindow(User user)
        {
            InitializeComponent();
            _user = user;
            LoadProducts();
        }

        private void LoadProducts(string filter = "")
        {
            using var db = new AppDbContext();
            var products = db.Products.ToList();

            if (!string.IsNullOrEmpty(filter))
                products = products.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                               p.Manufacturer.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                                               p.Category.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            ProductsGrid.ItemsSource = products;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => LoadProducts(SearchBox.Text);

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role == "Guest")
            {
                MessageBox.Show("Гостю нельзя добавлять товары!");
                return;
            }

            new ProductEditWindow().ShowDialog();
            LoadProducts();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is Product p)
            {
                new ProductEditWindow(p).ShowDialog();
                LoadProducts();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not Product p) return;

            using var db = new AppDbContext();
            var existing = db.Products.Find(p.Id);
            if (existing != null)
            {
                db.Products.Remove(existing);
                db.SaveChanges();
                LoadProducts();
            }
        }
    }
}
