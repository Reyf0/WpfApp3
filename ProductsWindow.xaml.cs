using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WpfApp3.Data;
using WpfApp3.Models;

namespace WpfApp3
{
    public partial class ProductsWindow : Page
    {
        private readonly User _user;
        private ObservableCollection<Product> _products = [];
        private ICollectionView? _collectionView;
        private bool _sortAscending = true;
        private const int MaxImageWidth = 300;
        private const int MaxImageHeight = 200;
        private readonly string ImagesFolder;

        public ProductsWindow(User user)
        {
            InitializeComponent();
            _user = user;

            
            ImagesFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
            if (!Directory.Exists(ImagesFolder)) Directory.CreateDirectory(ImagesFolder);

            ProductsListView.MouseDoubleClick += ProductsListView_MouseDoubleClick;

            LoadProducts();
        }

        private void LoadProducts(string filter = "")
        {
            using var db = new AppDbContext();
            var productsFromDb = db.Products.ToList();

            foreach (var p in productsFromDb)
            {
                if (string.IsNullOrWhiteSpace(p.ImagePath) || !File.Exists(p.ImagePath))
                {
                    p.ImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "placeholder.png");
                }
            }

            _products = new ObservableCollection<Product>(productsFromDb);
            _collectionView = CollectionViewSource.GetDefaultView(_products);
            _collectionView.Filter = item =>
            {
                if (item is not Product p)
                    return false;

                string search = SearchBox.Text ?? "";
                string supplier = SupplierFilter.SelectedItem as string ?? "";

                return FilterPredicate(p, search, supplier);
            };




            ProductsListView.ItemsSource = _collectionView;

            var suppliers = productsFromDb.Select(p => p.Manufacturer).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            suppliers.Insert(0, "Все поставщики");
            SupplierFilter.ItemsSource = suppliers;
            SupplierFilter.SelectedIndex = 0;

            ApplySort();
        }

        private bool FilterPredicate(Product p, string? search, string? supplier)
        {
            if (p == null) return false;

            if (!string.IsNullOrEmpty(supplier) && supplier != "Все поставщики")
            {
                if (!string.Equals(p.Manufacturer, supplier, StringComparison.OrdinalIgnoreCase)) return false;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowered = search.ToLowerInvariant();
                if (!((p.Name ?? "").ToLowerInvariant().Contains(lowered)
                    || (p.Category ?? "").ToLowerInvariant().Contains(lowered)
                    || (p.Manufacturer ?? "").ToLowerInvariant().Contains(lowered)))
                {
                    return false;
                }
            }

            return true;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _collectionView?.Refresh();
        }

        private void SupplierFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _collectionView?.Refresh();
        }

        private void ToggleSort_Click(object sender, RoutedEventArgs e)
        {
            _sortAscending = !_sortAscending;
            ApplySort();
        }

        private void ApplySort()
        {
            if (_collectionView == null) return;

            using (_collectionView.DeferRefresh())
            {
                _collectionView.SortDescriptions.Clear();
                var direction = _sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending;
                _collectionView.SortDescriptions.Add(new SortDescription(nameof(Product.Quantity), direction));
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role == "Guest")
            {
                MessageBox.Show("Гостю нельзя добавлять товары!");
                return;
            }

            new ProductEditWindow().ShowDialog();
            ReloadFromDb();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role == "Guest")
            {
                MessageBox.Show("Гостю нельзя изменять товары!");
                return;
            }

            if (ProductsListView.SelectedItem is Product p)
            {
                new ProductEditWindow(p).ShowDialog();
                ReloadFromDb();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role == "Guest")
            {
                MessageBox.Show("Гостю нельзя удалять товары!");
                return;
            }

            if (ProductsListView.SelectedItem is not Product p) return;

            var result = MessageBox.Show($"Удалить товар \"{p.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var existing = db.Products.Find(p.Id);
            if (existing != null)
            {
                db.Products.Remove(existing);
                db.SaveChanges();
            }

            ReloadFromDb();
        }

        private void ReloadFromDb()
        {
            var supplier = SupplierFilter.SelectedItem as string;
            var search = SearchBox.Text;
            var sortAsc = _sortAscending;

            LoadProducts();
            SupplierFilter.SelectedItem = supplier ?? "Все поставщики";
            SearchBox.Text = search;
            _sortAscending = sortAsc;
            ApplySort();
        }

        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (_user.Role == "Guest")
            {
                MessageBox.Show("Гостю нельзя загружать изображения!");
                return;
            }

            if (ProductsListView.SelectedItem is not Product p)
            {
                MessageBox.Show("Выберите товар, к которому хотите загрузить фото.");
                return;
            }

            var ofd = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                var savedRelative = SaveResizedImageAndGetPath(ofd.FileName);
                if (string.IsNullOrEmpty(savedRelative)) throw new Exception("Не удалось сохранить изображение.");

                using var db = new AppDbContext();
                var existing = db.Products.Find(p.Id);
                if (existing != null)
                {
                    existing.ImagePath = savedRelative;
                    db.SaveChanges();
                }

                p.ImagePath = savedRelative;
                _collectionView?.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обработке изображения: " + ex.Message);
            }
        }

        private string SaveResizedImageAndGetPath(string sourcePath)
        {
            // Загружаем BitmapImage с декодированием нужных пикселей (для ускорения)
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(sourcePath);
            // Устанавливаем декод-пиксели, сохранив пропорции
            // Сначала пробуем по ширине
            bmp.DecodePixelWidth = MaxImageWidth;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            // Рассчитать размеры с сохранением пропорций
            double ratio = Math.Min((double)MaxImageWidth / bmp.PixelWidth, (double)MaxImageHeight / bmp.PixelHeight);
            if (ratio > 1) ratio = 1; // не увеличиваем изображение
            int newW = (int)(bmp.PixelWidth * ratio);
            int newH = (int)(bmp.PixelHeight * ratio);

            // Создаём RenderTargetBitmap для перерисовки в нужный размер
            var vb = new System.Windows.Media.DrawingVisual();
            using (var dc = vb.RenderOpen())
            {
                dc.DrawImage(bmp, new System.Windows.Rect(0, 0, newW, newH));
            }

            var rtb = new RenderTargetBitmap(newW, newH, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(vb);

            // Сохраняем как PNG/JPG
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            var fileName = $"img_{Guid.NewGuid():N}{(ext == ".png" ? ".png" : ".jpg")}";
            var outPath = Path.Combine(ImagesFolder, fileName);

            BitmapEncoder encoder = (ext == ".png") ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder() { QualityLevel = 90 };
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                encoder.Save(fs);
            }

            return outPath;
        }

        private void ProductsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProductsListView.SelectedItem is Product p)
            {
                new ProductEditWindow(p).ShowDialog();
                ReloadFromDb();
            }
        }
    }

   
}
