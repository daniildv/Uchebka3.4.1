using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DE.Pages
{
    public partial class OrderEditPage : Page
    {
        private Order2 _order;
        private bool _isNew;
        private bool _isAdmin;

        public OrderEditPage(Order2 order = null)
        {
            InitializeComponent();
            this.Title = "Окно изменения заказов";

            _order = order;
            _isNew = order == null;

            // Проверяем роль текущего пользователя
            CheckAdminRole();

            TitleText.Text = _isNew ? "Новый заказ" : "Редактирование заказа";

            LoadAddresses();

            if (!_isNew)
            {
                LoadOrder();

                // Показываем кнопки только для администратора
                if (_isAdmin)
                {
                    EditProductBtn.Visibility = Visibility.Visible;
                    DeleteOrderBtn.Visibility = Visibility.Visible;
                }
            }
            else
            {
                GenerateNextId();
                OrderNumberBox.Text = "";
                OrderDate.SelectedDate = DateTime.Today;
                DeliveryDate.SelectedDate = DateTime.Today.AddDays(7);
                StatusCombo.SelectedIndex = 0;

                // Для нового заказа кнопки не показываем
                EditProductBtn.Visibility = Visibility.Collapsed;
                DeleteOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        // Проверка роли администратора
        private void CheckAdminRole()
        {
            try
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.CurrentUser != null)
                    {
                        // Проверяем, является ли пользователь администратором
                        _isAdmin = mainWindow.CurrentUser.UserRole == "Администратор" ||
                                   mainWindow.CurrentUser.UserRole == "Admin";
                    }
                    else
                    {
                        _isAdmin = false;
                    }
                }
                else
                {
                    _isAdmin = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке роли: {ex.Message}");
                _isAdmin = false;
            }
        }

        // Кнопка редактирования товара
        private void EditProductBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_order == null)
                {
                    MessageBox.Show("Информация о заказе отсутствует",
                                  "Предупреждение",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (_order.ID_Product == null)
                {
                    MessageBox.Show("Информация о товаре отсутствует",
                                  "Предупреждение",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                using (var db = new user33Entities())
                {
                    var product = db.Product2.FirstOrDefault(p => p.ID_Product == _order.ID_Product);

                    if (product != null)
                    {
                        var productEditPage = new ProductEditPage(product);

                        if (NavigationService != null)
                        {
                            NavigationService.Navigate(productEditPage);
                        }
                        else if (Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            mainWindow.MainFrame.Navigate(productEditPage);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Товар не найден в базе данных",
                                      "Предупреждение",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии товара: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // Кнопка удаления заказа
        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isNew)
                {
                    MessageBox.Show("Нельзя удалить еще не сохраненный заказ",
                                  "Предупреждение",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Вы действительно хотите удалить заказ №{_order.OrderNumber}?",
                                            "Подтверждение удаления",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var db = new user33Entities())
                    {
                        var orderToDelete = db.Order2.Find(_order.ID_Order);

                        if (orderToDelete != null)
                        {
                            db.Order2.Remove(orderToDelete);
                            db.SaveChanges();

                            MessageBox.Show("Заказ успешно удален",
                                          "Успешно",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);

                            // Возвращаемся на страницу списка заказов
                            if (NavigationService != null && NavigationService.CanGoBack)
                            {
                                NavigationService.GoBack();
                            }
                            else if (Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                var ordersPage = new OrdersPage();

                                if (mainWindow.CurrentUser == null)
                                    OrdersPage.CurrentUserRole = "Гость";
                                else
                                    OrdersPage.CurrentUserRole = mainWindow.CurrentUser.UserRole;

                                mainWindow.MainFrame.Navigate(ordersPage);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Заказ не найден в базе данных",
                                          "Ошибка",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении заказа: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        //проверка ID из БД для устранения повтора
        private void GenerateNextId()
        {
            try
            {
                using (var db = new user33Entities())
                {
                    int maxId = 0;
                    if (db.Order2.Any())
                    {
                        maxId = db.Order2.Max(o => o.ID_Order);
                    }
                    int nextId = maxId + 1;
                    IdBox.Text = nextId.ToString();
                }
            }
            catch (Exception)
            {
                IdBox.Text = "1";
            }
        }

        //загрузка адреса из БД в выпадающий список
        private void LoadAddresses()
        {
            using (var db = new user33Entities())
            {
                var addresses = db.Address2.ToList();
                AddressCombo.ItemsSource = addresses;
                AddressCombo.DisplayMemberPath = "FullAddress";
                AddressCombo.SelectedValuePath = "ID_Address";
            }
        }

        private void LoadOrder()
        {
            IdBox.Text = _order.ID_Order.ToString();
            OrderNumberBox.Text = _order.OrderNumber.ToString();

            foreach (ComboBoxItem item in StatusCombo.Items)
                if (item.Content.ToString() == _order.Status)
                    StatusCombo.SelectedItem = item;

            AddressCombo.SelectedValue = _order.ID_Address;
            OrderDate.SelectedDate = _order.OrderDate;
            DeliveryDate.SelectedDate = _order.DeliveryDate;
        }

        private void OrderNumberBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(OrderNumberBox.Text, out int num) && num > 0)
            {
                OrderNumberBox.Background = Brushes.White;
            }
            else
            {
                OrderNumberBox.Background = Brushes.LightPink;
            }
        }

        // валидация кнопки сохранения изменения заказов
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OrderNumberBox.Text) ||
                !int.TryParse(OrderNumberBox.Text, out int orderNum) || orderNum <= 0)
            {
                MessageBox.Show("Введите корректный номер заказа (положительное число)");
                OrderNumberBox.Focus();
                return;
            }

            if (StatusCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите статус");
                return;
            }

            if (AddressCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите адрес");
                return;
            }

            if (OrderDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату заказа");
                return;
            }

            if (DeliveryDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату выдачи");
                return;
            }

            try
            {
                using (var db = new user33Entities())
                {
                    if (_isNew)
                    {
                        _order = new Order2();
                        db.Order2.Add(_order);

                        if (db.Order2.Any(o => o.OrderNumber == orderNum))
                        {
                            MessageBox.Show($"Заказ с номером {orderNum} уже существует. Введите другой номер.");
                            return;
                        }

                        _order.RecipientCode = new Random().Next(1000, 9999);
                        _order.Quantity = 1;
                        _order.ID_Product = db.Product2.First().ID_Product;
                        _order.ID_User = db.User2.First().ID_User;

                        if (int.TryParse(IdBox.Text, out int id))
                        {
                            _order.ID_Order = id;
                        }
                    }
                    else
                    {
                        _order = db.Order2.Find(_order.ID_Order);

                        if (db.Order2.Any(o => o.OrderNumber == orderNum && o.ID_Order != _order.ID_Order))
                        {
                            MessageBox.Show($"Заказ с номером {orderNum} уже существует. Введите другой номер.");
                            return;
                        }
                    }

                    _order.OrderNumber = orderNum;
                    _order.Status = ((ComboBoxItem)StatusCombo.SelectedItem).Content.ToString();
                    _order.ID_Address = (int)AddressCombo.SelectedValue;
                    _order.OrderDate = OrderDate.SelectedDate.Value;
                    _order.DeliveryDate = DeliveryDate.SelectedDate.Value;

                    db.SaveChanges();

                    MessageBox.Show("Сохранено");

                    if (Application.Current.MainWindow is MainWindow win)
                        win.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // валидация кнопки отмены
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Вы точно хотите отменить изменения и вернуться?",
                    "Подтверждение отмены",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                if (NavigationService != null && NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    var ordersPage = new OrdersPage();

                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        if (mainWindow.CurrentUser == null)
                            OrdersPage.CurrentUserRole = "Гость";
                        else
                            OrdersPage.CurrentUserRole = mainWindow.CurrentUser.UserRole;
                    }

                    NavigationService?.Navigate(ordersPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате: {ex.Message}");
            }
        }
    }
}