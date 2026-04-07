using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;

namespace DE.Pages
{
    public partial class ProductsPage : Page
    {
        private List<Product2> allProducts;
        private string currentSearch = "";
        private string currentSupplierFilter = "Все поставщики";
        private string currentSort = "none";

        public static string CurrentUserRole { get; set; } = "Гость";

        public bool IsAdmin => CurrentUserRole == "Администратор";
        public bool IsManager => CurrentUserRole == "Менеджер";
        public bool IsClient => CurrentUserRole == "Авторизированный клиент" || CurrentUserRole == "Авторизованный клиент";
        public bool IsGuest => CurrentUserRole == "Гость";
        public bool CanInteract => IsAdmin || IsManager;
        public bool IsReadOnly => !CanInteract;
        public bool Guesr_Client => CurrentUserRole == "Гость" || CurrentUserRole == "Клиент";



        public ProductsPage()
        {
            try
            {
                InitializeComponent();

                Debug.WriteLine($"ProductsPage создана с ролью: {CurrentUserRole}");
                Debug.WriteLine($"IsAdmin: {IsAdmin}, IsManager: {IsManager}, CanInteract: {CanInteract}");

                LoadProducts();
                
                if (CanInteract)
                {
                    LoadSupplierFilter();
                    
                    if (SortComboBox != null && SortComboBox.Items.Count > 0)
                    {
                        SortComboBox.SelectedIndex = 0;
                    }
                }

                UpdateControlsState();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки",
                    $"Не удалось загрузить страницу товаров: {ex.Message}");
            }
        }
        public class RoleToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is User2 user && user != null)
                {
                    // Показываем только для администратора и менеджера
                    return (user.UserRole == "Администратор" || user.UserRole == "Менеджер")
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ProductsPage: Page_Loaded вызван");
            LoadProducts();
            
            if (CanInteract)
            {
                LoadSupplierFilter();
            }

            UpdateControlsState();
        }

        private void UpdateControlsState()
        {
            if (CanInteract)
            {
                SearchTextBox.Visibility = Visibility.Visible;
                SortComboBox.Visibility = Visibility.Visible;
                SupplierFilterComboBox.Visibility = Visibility.Visible;
                
                SearchTextBox.IsEnabled = true;
                SortComboBox.IsEnabled = true;
                SupplierFilterComboBox.IsEnabled = true;

                SearchTextBox.ToolTip = "Введите текст для поиска по названию, описанию, производителю или поставщику";
                SortComboBox.ToolTip = "Выберите тип сортировки";
                SupplierFilterComboBox.ToolTip = "Выберите поставщика для фильтрации";
                
                Debug.WriteLine("Фильтры включены для администратора/менеджера");
            }
            else
            {
                SearchTextBox.Visibility = Visibility.Collapsed;
                SortComboBox.Visibility = Visibility.Collapsed;
                SupplierFilterComboBox.Visibility = Visibility.Collapsed;
                
                Debug.WriteLine("Фильтры скрыты для клиента/гостя");
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (var db = new user33Entities())
                {
                    allProducts = db.Product2.ToList();

                    Debug.WriteLine($"Загружено товаров: {allProducts.Count}");

                    ApplyFiltersAndSort();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки товаров",
                    $"Не удалось загрузить список товаров из базы данных: {ex.Message}");
            }
        }

        private void LoadSupplierFilter()
        {
            try
            {
                using (var db = new user33Entities())
                {
                    var suppliers = db.Product2
                        .Select(p => p.Supplier)
                        .Where(s => s != null && s.Trim() != "")
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();

                    Debug.WriteLine($"Найдено поставщиков в БД: {suppliers.Count}");

                    SupplierFilterComboBox.Items.Clear();
                    SupplierFilterComboBox.Items.Add("Все поставщики");

                    foreach (var supplier in suppliers)
                    {
                        SupplierFilterComboBox.Items.Add(supplier);
                    }

                    if (SupplierFilterComboBox.Items.Count > 0)
                    {
                        SupplierFilterComboBox.SelectedIndex = 0;
                        currentSupplierFilter = "Все поставщики";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка загрузки фильтров",
                    $"Не удалось загрузить список поставщиков: {ex.Message}");
            }
        }

        // валидация применения фильтрации
        private void ApplyFiltersAndSort()
        {
            try
            {
                if (allProducts == null)
                {
                    Debug.WriteLine("allProducts is null");
                    return;
                }

                Debug.WriteLine($"Применение фильтров. Текущий фильтр по поставщику: '{currentSupplierFilter}'");

                var filtered = allProducts.AsEnumerable();

                if (CanInteract && !string.IsNullOrWhiteSpace(currentSearch))
                {
                    string searchLower = currentSearch.ToLower().Trim();
                    Debug.WriteLine($"Поиск: '{searchLower}'");

                    filtered = filtered.Where(p =>
                        (p.Name != null && p.Name.ToLower().Contains(searchLower)) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                        (p. Manufacturer != null && p.Manufacturer.ToLower().Contains(searchLower)) ||
                        (p.Supplier != null && p.Supplier.ToLower().Contains(searchLower)) ||
                        (p.Category != null && p.Category.ToLower().Contains(searchLower)));
                }

                if (CanInteract && currentSupplierFilter != "Все поставщики" && !string.IsNullOrEmpty(currentSupplierFilter))
                {
                    Debug.WriteLine($"Применяем фильтр по поставщику: '{currentSupplierFilter}'");
                    filtered = filtered.Where(p => p.Supplier == currentSupplierFilter);
                }

                if (CanInteract)
                {
                    switch (currentSort)
                    {
                        case "quantity_asc":
                            filtered = filtered.OrderBy(p => p.Quantity);
                            Debug.WriteLine("Сортировка: по количеству (возрастание)");
                            break;
                        case "quantity_desc":
                            filtered = filtered.OrderByDescending(p => p.Quantity);
                            Debug.WriteLine("Сортировка: по количеству (убывание)");
                            break;
                        case "price_asc":
                            filtered = filtered.OrderBy(p => p.Price);
                            Debug.WriteLine("Сортировка: по цене (возрастание)");
                            break;
                        case "price_desc":
                            filtered = filtered.OrderByDescending(p => p.Price);
                            Debug.WriteLine("Сортировка: по цене (убывание)");
                            break;
                        case "name_asc":
                            filtered = filtered.OrderBy(p => p.Name);
                            Debug.WriteLine("Сортировка: по названию (А-Я)");
                            break;
                        case "name_desc":
                            filtered = filtered.OrderByDescending(p => p.Name);
                            Debug.WriteLine("Сортировка: по названию (Я-А)");
                            break;
                        default:
                            filtered = filtered.OrderBy(p => p.ID_Product);
                            Debug.WriteLine("Сортировка: по умолчанию (ID)");
                            break;
                    }
                }
                else
                {
                    // для клиентов и гостей сортировка выкл
                    filtered = filtered.OrderBy(p => p.ID_Product);
                }

                var resultList = filtered.ToList();
                ProductsList.ItemsSource = resultList;

                Debug.WriteLine($"Итоговое количество товаров: {resultList.Count}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка фильтрации",
                    $"Произошла ошибка при применении фильтров: {ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!CanInteract) return;
                
                currentSearch = SearchTextBox.Text;
                Debug.WriteLine($"Поиск изменен: '{currentSearch}'");
                ApplyFiltersAndSort();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка поиска",
                    $"Не удалось выполнить поиск: {ex.Message}");
            }
        }

        private void SupplierFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!CanInteract) return;
                
                Debug.WriteLine("SupplierFilterComboBox_SelectionChanged вызван");

                if (SupplierFilterComboBox.SelectedItem != null)
                {
                    currentSupplierFilter = SupplierFilterComboBox.SelectedItem.ToString();
                    Debug.WriteLine($"Фильтр по поставщику изменен на: '{currentSupplierFilter}'");
                    ApplyFiltersAndSort();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в SupplierFilterComboBox_SelectionChanged: {ex.Message}");
                ShowErrorMessage("Ошибка фильтрации",
                    $"Не удалось применить фильтр по поставщику: {ex.Message}");
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!CanInteract) return;
                
                if (SortComboBox.SelectedItem != null)
                {
                    var selectedItem = SortComboBox.SelectedItem as ComboBoxItem;
                    if (selectedItem?.Tag != null)
                    {
                        currentSort = selectedItem.Tag.ToString();
                        Debug.WriteLine($"Сортировка изменена: '{currentSort}'");
                    }
                    else
                    {
                        currentSort = "none";
                    }
                    ApplyFiltersAndSort();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка сортировки",
                    $"Не удалось применить сортировку: {ex.Message}");
            }
        }

        // валидация применения выпадающих списков
        private void Product_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Product_Click вызван!");

                var button = sender as Button;
                if (button == null)
                {
                    System.Diagnostics.Debug.WriteLine("button is null");
                    return;
                }

                var product = button.CommandParameter as Product2;
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine("product is null");
                    System.Diagnostics.Debug.WriteLine($"CommandParameter type: {button.CommandParameter?.GetType()}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Товар: {product.Name}, ID: {product.ID_Product}");
                System.Diagnostics.Debug.WriteLine($"IsAdmin: {IsAdmin}");

                if (IsAdmin)
                {
                    OpenEditPage(product);
                }
                else
                {
                    ShowProducttails(product);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}");
                ShowErrorMessage("Ошибка", $"Не удалось открыть информацию о товаре: {ex.Message}");
            }
        }

        //кнопка добавления товаров
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("AddProductButton_Click вызван");

                if (!IsAdmin)
                {
                    ShowWarningMessage("Доступ запрещен",
                        "Только администратор может добавлять новые товары.");
                    return;
                }

                var mainWindow = Application.Current.MainWindow;

                if (mainWindow is MainWindow window)
                {
                    window.NavigateToPage(new ProductEditPage(null));
                    this.Title = "Изменение товаров";

                }
                else
                {
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win is MainWindow mainWin)
                        {
                            mainWin.NavigateToPage(new ProductEditPage(null));
                            this.Title = "Изменение товаров";
                            return;
                        }
                    }

                    ShowErrorMessage("Ошибка", "Не удалось найти главное окно приложения");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка",
                    $"Не удалось открыть форму добавления товара: {ex.Message}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var product = button?.CommandParameter as Product2;

                if (product == null || !IsAdmin) return;

                using (var db = new user33Entities())
                {
                    bool hasOrders = db.Order2.Any(o => o.ID_Product == product.ID_Product);

                    if (hasOrders)
                    {
                        ShowWarningMessage("Невозможно удалить",
                            $"Товар '{product.Name}' нельзя удалить, так как он присутствует в заказах.");
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Вы действительно хотите удалить товар '{product.Name}'?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!string.IsNullOrEmpty(product.Photo) &&
                            !product.Photo.Contains("picture.png") &&
                            File.Exists(product.Photo))
                        {
                            try
                            {
                                File.Delete(product.Photo);
                            }
                            catch (Exception ex)
                            {
                                ShowWarningMessage("Предупреждение",
                                    $"Не удалось удалить файл изображения: {ex.Message}");
                            }
                        }

                        db.Product2.Attach(product);
                        db.Product2.Remove(product);
                        db.SaveChanges();

                        ShowInfoMessage("Успешно", "Товар успешно удален");
                        LoadProducts();
                        LoadSupplierFilter();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка удаления",
                    $"Не удалось удалить товар: {ex.Message}");
            }
        }

        private void OpenEditPage(Product2 product)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"OpenEditPage вызван для товара ID: {product.ID_Product}, Name: {product.Name}");

                System.Diagnostics.Debug.WriteLine($"Application.Current: {Application.Current}");

                if (Application.Current == null)
                {
                    System.Diagnostics.Debug.WriteLine("Application.Current is null!");
                    ShowErrorMessage("Ошибка", "Application.Current is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Application.Current.MainWindow: {Application.Current.MainWindow}");
                System.Diagnostics.Debug.WriteLine($"Application.Current.MainWindow type: {Application.Current.MainWindow?.GetType()}");

                var mainWindow = Application.Current.MainWindow as MainWindow;

                if (mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("mainWindow is null! Ищем среди открытых окон...");

                    foreach (Window win in Application.Current.Windows)
                    {
                        System.Diagnostics.Debug.WriteLine($"Найдено окно: {win.GetType().Name}");
                        if (win is MainWindow mainWin)
                        {
                            System.Diagnostics.Debug.WriteLine("Нашли MainWindow среди открытых окон!");
                            mainWindow = mainWin;
                            break;
                        }
                    }
                }

                if (mainWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow найден! Создаем ProductEditPage для товара ID: {product.ID_Product}");
                    var editPage = new ProductEditPage(product);
                    System.Diagnostics.Debug.WriteLine($"ProductEditPage создана, вызываем NavigateToPage");
                    mainWindow.NavigateToPage(editPage);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow НЕ НАЙДЕН!");
                    ShowErrorMessage("Ошибка", "Не удалось найти главное окно приложения");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в OpenEditPage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ShowErrorMessage("Ошибка навигации",
                    $"Не удалось открыть страницу редактирования: {ex.Message}");
            }
        }

        private void ShowProducttails(Product2 product)
        {
            // Рассчитываем цену со скидкой
            decimal discountPrice = product.Price;
            if (product.Discount > 0)
            {
                discountPrice = product.Price * (1 - (decimal)product.Discount / 100);
            }

            string details = $"Артикул: {product.Article}\n" +
                           $"Наименование: {product.Name}\n" +
                           $"Категория: {product.Category}\n" +
                           $"Производитель: {product.Manufacturer}\n" +
                           $"Поставщик: {product.Supplier}\n" +
                           $"Цена: {product.Price:N2} руб.\n" +
                           $"Скидка: {product.Discount}%\n" +
                           $"Цена со скидкой: {discountPrice:N2} руб.\n" +
                           $"На складе: {product.Quantity} {product.Unit}\n" +
                           $"Описание: {product.Description ?? "Нет описания"}";

            MessageBox.Show(details, "Информация о товаре",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowInfoMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarningMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}