using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DE.Pages
{
    public partial class ProductEditPage : Page
    {
        private Product2 currentProduct;
        private bool isNewProduct;
        private string currentPhotoFullPath;
        private static bool isEditWindowOpen = false;
        private ImageConverter imageConverter = new ImageConverter();

        //отслеживание триггера для скидки
        private bool discountTriggerActivated = false;

        //отслеживание триггера нулевого количества
        private bool zeroQuantityTriggerActivated = false;

        public ProductEditPage() : this(null) { }

        public ProductEditPage(Product2 product)
        {
            try
            {
                if (isEditWindowOpen)
                {
                    MessageBox.Show("Окно редактирования уже открыто. Сначала закройте его.",
                                  "Предупреждение",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.GoBack();
                    }
                    return;
                }

                InitializeComponent();
                this.Title = "Окно изменения товаров";
                isEditWindowOpen = true;
                currentProduct = product;
                isNewProduct = product == null;

                PageHeader.Text = isNewProduct ? "Добавление нового товара" : "Редактирование товара";

                this.Unloaded += Page_Unloaded;

                LoadData();
                LoadComboBoxes();

                if (isNewProduct)
                {
                    GenerateNextId();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка инициализации",
                    $"Не удалось загрузить форму: {ex.Message}");
                isEditWindowOpen = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            isEditWindowOpen = false;
            this.Unloaded -= Page_Unloaded;
        }

        //логика автоматического ID в строке
        private void GenerateNextId()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GenerateNextId вызван");

                using (var db = new user33Entities())
                {
                    int maxId = 0;

                    if (db.Product2.Any())
                    {
                        maxId = db.Product2.Max(p => p.ID_Product);
                    }

                    int nextId = maxId + 1;

                    IdTextBox.Text = nextId.ToString();

                    System.Diagnostics.Debug.WriteLine($"Сгенерирован следующий ID по порядку: {nextId} (предыдущий максимальный: {maxId})");

                    IdTextBox.Background = new SolidColorBrush(Colors.LightBlue);

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, args) =>
                    {
                        IdTextBox.Background = Brushes.White;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при генерации ID: {ex.Message}");
                ShowErrorMessage("Ошибка при генерации ID",
                    $"Не удалось сгенерировать следующий ID: {ex.Message}");

                IdTextBox.Text = "1";
            }
        }

        private void RegenerateId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RegenerateId_Click вызван");

                using (var db = new user33Entities())
                {
                    var existingIds = db.Product2.Select(p => p.ID_Product).ToHashSet();

                    if (!isNewProduct && currentProduct != null && currentProduct.ID_Product > 0)
                    {
                        existingIds.Remove(currentProduct.ID_Product);
                        System.Diagnostics.Debug.WriteLine($"Исключаем текущий ID {currentProduct.ID_Product} из проверки");
                    }

                    int maxId = existingIds.Any() ? existingIds.Max() : 0;

                    int nextId = maxId + 1;

                    while (existingIds.Contains(nextId))
                    {
                        nextId++;
                    }

                    IdTextBox.Text = nextId.ToString();

                    System.Diagnostics.Debug.WriteLine($"Сгенерирован новый ID по порядку: {nextId}");

                    IdTextBox.Background = new SolidColorBrush(Colors.LightGreen);

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, args) =>
                    {
                        IdTextBox.Background = Brushes.White;
                        timer.Stop();
                    };
                    timer.Start();

                    ShowInfoMessage("Успешно",
                        $"Сгенерирован новый ID по порядку: {nextId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при генерации ID: {ex.Message}");
                ShowErrorMessage("Ошибка при генерации ID",
                    $"Не удалось сгенерировать ID: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                if (!isNewProduct && currentProduct != null)
                {
                    IdTextBox.Text = currentProduct.ID_Product.ToString();
                    ArticleTextBox.Text = currentProduct.Article;
                    NameTextBox.Text = currentProduct.Name;
                    DescriptionTextBox.Text = currentProduct.Description;
                    PriceTextBox.Text = currentProduct.Price.ToString("F2");
                    QuantityTextBox.Text = currentProduct.Quantity.ToString();
                    DiscountTextBox.Text = currentProduct.Discount.ToString();

                    currentPhotoFullPath = currentProduct.Photo;

                    if (!string.IsNullOrEmpty(currentPhotoFullPath))
                    {
                        ProductImage.Source = imageConverter.Convert(currentPhotoFullPath, typeof(ImageSource), null, null) as ImageSource;
                    }
                    else
                    {
                        ProductImage.Source = imageConverter.Convert(null, typeof(ImageSource), null, null) as ImageSource;
                    }
                }
                else
                {
                    DiscountTextBox.Text = "0";
                    QuantityTextBox.Text = "0";
                    currentPhotoFullPath = null;

                    ProductImage.Source = imageConverter.Convert(null, typeof(ImageSource), null, null) as ImageSource;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки данных",
                    $"Не удалось загрузить данные товара: {ex.Message}");
            }
        }

        //логика комбобоксов в изменении каталога товаров
        private void LoadComboBoxes()
        {
            try
            {
                using (var db = new user33Entities())
                {
                    var categories = db.Product2
                        .Select(p => p.Category)
                        .Where(c => c != null && c.Trim() != "")
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    CategoryComboBox.Items.Clear();
                    foreach (var category in categories)
                    {
                        CategoryComboBox.Items.Add(category);
                    }

                    var manufacturers = db.Product2
                        .Select(p => p.Manufacturer)
                        .Where(m => m != null && m.Trim() != "")
                        .Distinct()
                        .OrderBy(m => m)
                        .ToList();

                    ManufacturerComboBox.Items.Clear();
                    foreach (var manufacturer in manufacturers)
                    {
                        ManufacturerComboBox.Items.Add(manufacturer);
                    }

                    var suppliers = db.Product2
                        .Select(p => p.Supplier)
                        .Where(s => s != null && s.Trim() != "")
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    SupplierComboBox.Items.Clear();
                    foreach (var supplier in suppliers)
                    {
                        SupplierComboBox.Items.Add(supplier);
                    }

                    var units = db.Product2
                        .Select(p => p.Unit)
                        .Where(u => u != null && u.Trim() != "")
                        .Distinct()
                        .OrderBy(u => u)
                        .ToList();

                    UnitComboBox.Items.Clear();

                    if (units.Count == 0)
                    {
                        string[] defaultUnits = { "шт", "кг", "л", "м", "упак", "компл", "пар" };
                        foreach (var unit in defaultUnits)
                        {
                            UnitComboBox.Items.Add(unit);
                        }
                    }
                    else
                    {
                        foreach (var unit in units)
                        {
                            UnitComboBox.Items.Add(unit);
                        }
                    }
                }

                if (!isNewProduct && currentProduct != null)
                {
                    CategoryComboBox.SelectedItem = currentProduct.Category;
                    ManufacturerComboBox.SelectedItem = currentProduct.Manufacturer;
                    SupplierComboBox.SelectedItem = currentProduct.Supplier;
                    UnitComboBox.SelectedItem = currentProduct.Unit;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки справочников",
                    $"Не удалось загрузить списки: {ex.Message}");
            }
        }

        private void ChangeImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Выберите изображение для товара";

                if (openFileDialog.ShowDialog() == true)
                {
                    string resourcesFolder = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Resources");

                    if (!Directory.Exists(resourcesFolder))
                    {
                        Directory.CreateDirectory(resourcesFolder);
                    }

                    string originalFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    string savePath = System.IO.Path.Combine(resourcesFolder, originalFileName);

                    // Проверка на дубликат имени файла
                    if (File.Exists(savePath))
                    {
                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
                        string extension = System.IO.Path.GetExtension(originalFileName);
                        int counter = 1;

                        while (File.Exists(savePath))
                        {
                            string newFileName = $"{fileNameWithoutExt}_{counter}{extension}";
                            savePath = System.IO.Path.Combine(resourcesFolder, newFileName);
                            originalFileName = newFileName;
                            counter++;
                        }
                    }

                    // Копируем файл
                    File.Copy(openFileDialog.FileName, savePath);

                    // Обновляем путь к фото
                    currentPhotoFullPath = originalFileName;

                    // Обновляем изображение на форме
                    ProductImage.Source = imageConverter.Convert(currentPhotoFullPath, typeof(ImageSource), null, null) as ImageSource;

                    // Удаляем старое фото, если оно существует и не является заглушкой
                    if (!string.IsNullOrEmpty(currentPhotoFullPath) &&
                        !currentPhotoFullPath.Contains("picture.png") &&
                        File.Exists(savePath))
                    {
                        // Здесь можно добавить логику удаления старого файла если нужно
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки изображения",
                    $"Не удалось загрузить изображение: {ex.Message}");
            }
        }


        private (int Width, int Height) GetImageInfo(string imagePath)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(imagePath);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                return (image.PixelWidth, image.PixelHeight);
            }
            catch
            {
                return (0, 0);
            }
        }

        //логика подстановки заглушки после удаления изображения
        private void DeleteOldPhoto(string fullPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullPath) || fullPath.Contains("picture.png"))
                    return;

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    System.Diagnostics.Debug.WriteLine($"Удалено старое фото: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении фото: {ex.Message}");
            }
        }

        //проверка изображения через битмап на размеры
        private (bool IsValid, string ErrorMessage) ValidateImageSize(string imagePath)
        {
            try
            {

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(imagePath);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                int width = image.PixelWidth;
                int height = image.PixelHeight;

                if (width > 300 && height > 300)
                {
                    return (false, $"Изображение слишком большое: {width}×{height} пикселей.\n" +
                                  "Максимально допустимый размер: 300×300 пикселей.");
                }
                else if (width > 300)
                {
                    return (false, $"Ширина изображения ({width} пикселей) превышает максимально допустимую (300).");
                }
                else if (height > 300)
                {
                    return (false, $"Высота изображения ({height} пикселей) превышает максимально допустимую (300).");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Не удалось прочитать изображение: {ex.Message}");
            }
        }

        private void RequiredField_TextChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(PriceTextBox.Text, out decimal price))
            {
                if (price < 0)
                {
                    PriceTextBox.Background = Brushes.LightPink;
                    PriceTextBox.ToolTip = "Цена не может быть отрицательной";
                }
                else
                {
                    PriceTextBox.Background = Brushes.White;
                    PriceTextBox.ToolTip = "Введите цену";
                }
            }
            else if (!string.IsNullOrEmpty(PriceTextBox.Text))
            {
                PriceTextBox.Background = Brushes.LightPink;
                PriceTextBox.ToolTip = "Введите корректное число";
            }
            else
            {
                PriceTextBox.Background = Brushes.White;
            }
            ValidateForm();
        }

        // пустой триггер для нулевого количества
        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(QuantityTextBox.Text, out int quantity))
            {
                if (quantity < 0)
                {
                    QuantityTextBox.Background = Brushes.LightPink;
                    QuantityTextBox.ToolTip = "Количество не может быть отрицательным";

                    // сброс триггера при отрицательном значении
                    zeroQuantityTriggerActivated = false;
                }
                else
                {
                    QuantityTextBox.Background = Brushes.White;
                    QuantityTextBox.ToolTip = "Введите количество";

                    // пустой триггер для нулевого количества 
                    if (quantity == 0)
                    {
                        // проверка на активирован триггер
                        if (!zeroQuantityTriggerActivated)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ТРИГГЕР] Количество товара равно 0");

                            // установка флага активного триггера
                            zeroQuantityTriggerActivated = true;
                        }
                    }
                    else
                    {
                        // сброс флага когда количество стало больше 0
                        zeroQuantityTriggerActivated = false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(QuantityTextBox.Text))
            {
                QuantityTextBox.Background = Brushes.LightPink;
                QuantityTextBox.ToolTip = "Введите целое число";

                // сброс флага триггера при некорректном значении
                zeroQuantityTriggerActivated = false;
            }
            else
            {
                QuantityTextBox.Background = Brushes.White;

                // сброс флага триггера при пустом значении
                zeroQuantityTriggerActivated = false;
            }
            ValidateForm();
        }

        // пустой триггер для скидки более 20%
        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(DiscountTextBox.Text, out int discount))
            {
                if (discount < 0 || discount > 100)
                {
                    DiscountTextBox.Background = Brushes.LightPink;
                    DiscountTextBox.ToolTip = "Скидка должна быть от 0 до 100";

                    // сброс флага триггера при некорректном значении
                    discountTriggerActivated = false;
                }
                else
                {
                    DiscountTextBox.Background = Brushes.White;
                    DiscountTextBox.ToolTip = "Введите скидку";

                    // пустой триггер для скидки более 20%
                    if (discount > 20)
                    {
                        // проверка на активирован триггер
                        if (!discountTriggerActivated)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ТРИГГЕР] Скидка {discount}% превышает 20%");

                            // установка флага активного триггера
                            discountTriggerActivated = true;
                        }
                    }
                    else
                    {
                        // сброс флага триггера когда скидка снова стала <= 20%
                        discountTriggerActivated = false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(DiscountTextBox.Text))
            {
                DiscountTextBox.Background = Brushes.LightPink;
                DiscountTextBox.ToolTip = "Введите целое число от 0 до 100";

                // сброс флага триггера при некорректном значении
                discountTriggerActivated = false;
            }
            else
            {
                DiscountTextBox.Background = Brushes.White;

                // сброс флага триггера при пустом значении
                discountTriggerActivated = false;
            }
            ValidateForm();
        }

        //валидация скидки
        private void ValidateForm()
        {
            try
            {
                bool isValid = false;

                if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
                {
                    CategoryComboBox.Background = Brushes.LightPink;
                    isValid = false;
                }
                else
                {
                    CategoryComboBox.Background = Brushes.White;
                }

                if (string.IsNullOrWhiteSpace(ManufacturerComboBox.Text))
                {
                    ManufacturerComboBox.Background = Brushes.LightPink;
                    isValid = false;
                }
                else
                {
                    ManufacturerComboBox.Background = Brushes.White;
                }

                if (string.IsNullOrWhiteSpace(SupplierComboBox.Text))
                {
                    SupplierComboBox.Background = Brushes.LightPink;
                    isValid = false;
                }
                else
                {
                    SupplierComboBox.Background = Brushes.White;
                }

                if (string.IsNullOrWhiteSpace(UnitComboBox.Text))
                {
                    UnitComboBox.Background = Brushes.LightPink;
                    isValid = false;
                }
                else
                {
                    UnitComboBox.Background = Brushes.White;
                }

                if (string.IsNullOrWhiteSpace(PriceTextBox.Text) ||
                    !decimal.TryParse(PriceTextBox.Text, out decimal price) ||
                    price < 0)
                {
                    isValid = false;
                }

                if (string.IsNullOrWhiteSpace(QuantityTextBox.Text) ||
                    !int.TryParse(QuantityTextBox.Text, out int quantity) ||
                    quantity < 0)
                {
                    isValid = false;
                }

                if (!string.IsNullOrEmpty(DiscountTextBox.Text))
                {
                    if (!int.TryParse(DiscountTextBox.Text, out int discount) ||
                        discount < 0 || discount > 100)
                    {
                        isValid = true;
                    }
                }

            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка валидации", ex.Message);
            }
        }

        // валидация кнопки сохранения изменений продуктов
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите артикул");
                ArticleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите наименование товара");
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите категорию");
                CategoryComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ManufacturerComboBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите производителя");
                ManufacturerComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(SupplierComboBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите поставщика");
                SupplierComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PriceTextBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите цену");
                PriceTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                ShowErrorMessage("Ошибка", "Введите корректную цену (положительное число)");
                PriceTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(QuantityTextBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите количество");
                QuantityTextBox.Focus();
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
            {
                ShowErrorMessage("Ошибка", "Введите корректное количество (неотрицательное число)");
                QuantityTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(DiscountTextBox.Text))
            {
                if (!int.TryParse(DiscountTextBox.Text, out int discount) || discount < 0 || discount > 100)
                {
                    ShowErrorMessage("Ошибка", "Скидка должна быть числом от 0 до 100");
                    DiscountTextBox.Focus();
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(UnitComboBox.Text))
            {
                ShowErrorMessage("Ошибка", "Введите единицу измерения");
                UnitComboBox.Focus();
                return;
            }

            if (!int.TryParse(IdTextBox.Text, out int productId) || productId == 0)
            {
                ShowErrorMessage("Ошибка",
                    "ID товара не может быть равен 0. " +
                    (isNewProduct ? "Произошла ошибка при автоматической генерации ID." : ""));
                if (isNewProduct)
                {
                    GenerateNextId();
                }
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    isNewProduct ? "Добавить новый товар?" : "Сохранить изменения?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                using (var db = new user33Entities())
                {
                    if (isNewProduct)
                    {
                        int newId = int.Parse(IdTextBox.Text);
                        if (db.Product2.Any(p => p.ID_Product == newId))
                        {
                            ShowErrorMessage("Ошибка",
                                $"Товар с ID {newId} уже существует. Генерируем следующий свободный ID...");
                            GenerateNextId();
                            return;
                        }

                        currentProduct = new Product2();
                        currentProduct.ID_Product = newId;
                        db.Product2.Add(currentProduct);
                    }
                    else
                    {
                        currentProduct = db.Product2.Find(currentProduct.ID_Product);
                    }

                    if (currentProduct == null)
                    {
                        throw new Exception("Товар не найден в базе данных");
                    }

                    currentProduct.Article = ArticleTextBox.Text.Trim();
                    currentProduct.Name = NameTextBox.Text.Trim();
                    currentProduct.Category = CategoryComboBox.Text.Trim();
                    currentProduct.Description = DescriptionTextBox.Text?.Trim();
                    currentProduct.Manufacturer = ManufacturerComboBox.Text.Trim();
                    currentProduct.Supplier = SupplierComboBox.Text.Trim();
                    currentProduct.Price = decimal.Parse(PriceTextBox.Text);
                    currentProduct.Quantity = int.Parse(QuantityTextBox.Text);
                    currentProduct.Discount = string.IsNullOrEmpty(DiscountTextBox.Text) ? 0 : int.Parse(DiscountTextBox.Text);
                    currentProduct.Unit = UnitComboBox.Text.Trim();

                    currentProduct.Photo = currentPhotoFullPath;

                    await db.SaveChangesAsync();

                    ShowInfoMessage("Успешно",
                        isNewProduct ? "Товар успешно добавлен" : "Изменения сохранены");

                    isEditWindowOpen = false;

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка сохранения", ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Отменить изменения и вернуться?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (isNewProduct && !string.IsNullOrEmpty(currentPhotoFullPath) &&
                        !currentPhotoFullPath.Contains("picture.png"))
                    {
                        DeleteOldPhoto(currentPhotoFullPath);
                    }

                    isEditWindowOpen = false;

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.MainFrame.CanGoBack)
                        {
                            mainWindow.MainFrame.GoBack();
                        }
                        else
                        {
                            var productsPage = new ProductsPage();

                            if (mainWindow.CurrentUser == null)
                                ProductsPage.CurrentUserRole = "Гость";
                            else
                                ProductsPage.CurrentUserRole = mainWindow.CurrentUser.UserRole;

                            mainWindow.MainFrame.Navigate(productsPage);
                        }
                    }
                    else
                    {
                        NavigationService?.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка", ex.Message);
            }
        }

        private void ShowInfoMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}