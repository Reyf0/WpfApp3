using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp3.Data;
using WpfApp3.Models;

namespace WpfApp3
{
    /// <summary>
    /// Логика взаимодействия для ProductEditWindow.xaml
    /// </summary>
    public partial class ProductEditWindow : Window
    {
        private const long MaxImageBytes = 2 * 1024 * 1024; // 2 MB
        private const int MaxImageWidth = 2000;
        private const int MaxImageHeight = 2000;

        private Product _product;
        private string? _newImageFullPath; 
        private string? _oldImageFullPath; 

        private static bool _isOpen = false; 

        public ProductEditWindow(Product? product = null)
        {
            if (_isOpen)
            {
                MessageBox.Show("Окно редактирования уже открыто. Закройте его перед открытием нового.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            _isOpen = true;
            InitializeComponent();

            _product = product ?? new Product();

            if (product != null)
            {
                NameBox.Text = product.Name;
                CategoryBox.Text = product.Category;
                ManufacturerBox.Text = product.Manufacturer;
                PriceBox.Text = product.Price.ToString(CultureInfo.CurrentCulture);
                DiscountBox.Text = product.Discount.ToString(CultureInfo.CurrentCulture);
                QuantityBox.Text = product.Quantity.ToString(CultureInfo.CurrentCulture);

                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    // Сохраним старый путь для возможного удаления
                    _oldImageFullPath = GetFullImagePath(product.ImagePath);
                    if (File.Exists(_oldImageFullPath))
                    {
                        PreviewImage.Source = new BitmapImage(new Uri(_oldImageFullPath));
                    }
                }
            }

            this.Closed += ProductEditWindow_Closed;
        }

        private void ProductEditWindow_Closed(object? sender, EventArgs e)
        {
            _isOpen = false;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                Title = "Выберите изображение товара"
            };

            if (dlg.ShowDialog() != true) return;

            var selectedPath = dlg.FileName;
            FileInfo fi = new FileInfo(selectedPath);

            if (fi.Length > MaxImageBytes)
            {
                ImageInfoText.Text = $"Файл слишком большой ({Math.Round(fi.Length / 1024.0 / 1024.0, 2)} MB). Максимум {MaxImageBytes / (1024 * 1024)} MB.";
                return;
            }

            try
            {
                var src = new BitmapImage();
                src.BeginInit();
                src.CacheOption = BitmapCacheOption.OnLoad;

                src.DecodePixelWidth = MaxImageWidth;
                src.DecodePixelHeight = MaxImageHeight;

                src.UriSource = new Uri(selectedPath, UriKind.Absolute);
                src.EndInit();
                src.Freeze();


                if (src.PixelWidth > MaxImageWidth || src.PixelHeight > MaxImageHeight)
                {
                    // Рассчитываем пропорции
                    double ratio = Math.Min((double)MaxImageWidth / src.PixelWidth, (double)MaxImageHeight / src.PixelHeight);
                    if (ratio < 1)
                    {
                        // ресайз ниже
                    }
                }

                int newW = src.PixelWidth;
                int newH = src.PixelHeight;
                double ratioCalc = Math.Min((double)MaxImageWidth / src.PixelWidth, (double)MaxImageHeight / src.PixelHeight);
                if (ratioCalc < 1)
                {
                    newW = (int)(src.PixelWidth * ratioCalc);
                    newH = (int)(src.PixelHeight * ratioCalc);
                }

                var vb = new System.Windows.Media.DrawingVisual();
                using (var dc = vb.RenderOpen())
                {
                    dc.DrawImage(src, new System.Windows.Rect(0, 0, newW, newH));
                }

                var rtb = new RenderTargetBitmap(newW, newH, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(vb);

                var ext = Path.GetExtension(selectedPath).ToLowerInvariant();
                var fileName = $"img_{Guid.NewGuid():N}{(ext == ".png" ? ".png" : ".jpg")}";
                var imagesDir = EnsureImagesDirectory();
                var destPath = Path.Combine(imagesDir, fileName);

                BitmapEncoder encoder = (ext == ".png") ? (BitmapEncoder)new PngBitmapEncoder() : new JpegBitmapEncoder() { QualityLevel = 90 };
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fs);
                }

                _newImageFullPath = destPath;
                _product.ImagePath = destPath; 

                var preview = new BitmapImage();
                preview.BeginInit();
                preview.CacheOption = BitmapCacheOption.OnLoad;
                preview.UriSource = new Uri(destPath, UriKind.Absolute);
                preview.EndInit();
                preview.Freeze();

                PreviewImage.Source = preview;
                ImageInfoText.Text = "";
            }
            catch (Exception ex)
            {
                ImageInfoText.Text = "Не удалось загрузить изображение: " + ex.Message;
            }
        }


        private static string EnsureImagesDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var imagesDir = Path.Combine(baseDir, "Images");
            if (!Directory.Exists(imagesDir))
                Directory.CreateDirectory(imagesDir);
            return imagesDir;
        }

        private static string? GetFullImagePath(string relativeOrPath)
        {
            if (string.IsNullOrEmpty(relativeOrPath)) return null;

            
            if (Path.IsPathRooted(relativeOrPath))
                return relativeOrPath;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, relativeOrPath);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out decimal price, out int discount, out int quantity))
                return;

            using var db = new AppDbContext();

            _product.Name = NameBox.Text.Trim();
            _product.Category = CategoryBox.Text.Trim();
            _product.Manufacturer = ManufacturerBox.Text.Trim();
            _product.Price = price;
            _product.Discount = discount;
            _product.Quantity = quantity;

            if (_product.Id == 0)
                db.Products.Add(_product);
            else
                db.Products.Update(_product);

            db.SaveChanges();

            if (!string.IsNullOrEmpty(_oldImageFullPath) && !string.IsNullOrEmpty(_newImageFullPath))
            {
                try
                {
                    var imagesDir = EnsureImagesDirectory();
                    var oldFull = _oldImageFullPath;
                    if (oldFull.StartsWith(imagesDir, StringComparison.InvariantCultureIgnoreCase) && File.Exists(oldFull))
                    {
                        File.Delete(oldFull);
                    }
                }
                catch
                {
                    // Игнорируем 
                }
            }

            NotifyMainWindowToRefresh();

            Close();
        }

        private void NotifyMainWindowToRefresh()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w.GetType().Name == "MainWindow")
                {
                    var mi = w.GetType().GetMethod("RefreshProducts");
                    if (mi != null)
                    {
                        mi.Invoke(w, null);
                        return;
                    }
                }
            }
        }

        private bool ValidateInputs(out decimal price, out int discount, out int quantity)
        {
            price = 0;
            discount = 0;
            quantity = 0;

            ResetControlStyle(NameBox);
            ResetControlStyle(CategoryBox);
            ResetControlStyle(ManufacturerBox);
            ResetControlStyle(PriceBox);
            ResetControlStyle(DiscountBox);
            ResetControlStyle(QuantityBox);

            var errors = new System.Collections.Generic.List<string>();

            var name = NameBox.Text?.Trim() ?? string.Empty;
            var category = CategoryBox.Text?.Trim() ?? string.Empty;
            var manufacturer = ManufacturerBox.Text?.Trim() ?? string.Empty;
            var priceText = PriceBox.Text?.Trim() ?? string.Empty;
            var discountText = DiscountBox.Text?.Trim() ?? string.Empty;
            var quantityText = QuantityBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(name))
            {
                errors.Add("Введите название товара.");
                MarkInvalid(NameBox, "Поле обязательно для заполнения.");
            }

            if (string.IsNullOrEmpty(category))
            {
                errors.Add("Введите категорию.");
                MarkInvalid(CategoryBox, "Поле обязательно для заполнения.");
            }

            if (string.IsNullOrEmpty(manufacturer))
            {
                errors.Add("Введите производителя.");
                MarkInvalid(ManufacturerBox, "Поле обязательно для заполнения.");
            }

            if (!decimal.TryParse(priceText, System.Globalization.NumberStyles.Number, CultureInfo.CurrentCulture, out price) || price < 0)
            {
                errors.Add("Цена должна быть числом (>= 0).");
                MarkInvalid(PriceBox, "Введите корректную цену (например 1999.99).");
            }

            if (!int.TryParse(discountText, System.Globalization.NumberStyles.Integer, CultureInfo.CurrentCulture, out discount) || discount < 0 || discount > 100)
            {
                errors.Add("Скидка должна быть целым числом от 0 до 100.");
                MarkInvalid(DiscountBox, "Введите число от 0 до 100.");
            }

            if (!int.TryParse(quantityText, System.Globalization.NumberStyles.Integer, CultureInfo.CurrentCulture, out quantity) || quantity < 0)
            {
                errors.Add("Количество должно быть целым числом (>= 0).");
                MarkInvalid(QuantityBox, "Введите корректное количество (например 0, 1, 10).");
            }

            if (errors.Any())
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void MarkInvalid(Control control, string tooltip)
        {
            if (control == null) return;
            control.BorderBrush = Brushes.Red;
            control.BorderThickness = new Thickness(1);
            control.ToolTip = tooltip;
        }

        private void ResetControlStyle(Control control)
        {
            if (control == null) return;
            control.ClearValue(Control.BorderBrushProperty);
            control.ClearValue(Control.BorderThicknessProperty);
            control.ToolTip = null;
        }
    }
}
