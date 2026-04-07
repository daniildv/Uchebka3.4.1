using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DE.Pages
{
    public partial class OrdersPage : Page
    {
        private List<Order2> allOrders;
        private string currentSearch = "";
        private string currentStatusFilter = "Все статусы";
        private string currentSort = "none";

        public static string CurrentUserRole { get; set; } = "Гость";
        public bool IsAdmin => CurrentUserRole == "Администратор";
        public bool IsManager => CurrentUserRole == "Менеджер";
        public bool CanInteract => IsAdmin || IsManager;

        public OrdersPage()
        {
            try
            {
                InitializeComponent();
                this.Title = "Заказы";
                Debug.WriteLine($"OrdersPage создана с ролью: {CurrentUserRole}");
                LoadOrders();

                if (SortComboBox != null && SortComboBox.Items.Count > 0)
                    SortComboBox.SelectedIndex = 0;

                if (StatusFilterComboBox != null && StatusFilterComboBox.Items.Count > 0)
                    StatusFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (var db = new user33Entities())
                {
                   
                    allOrders = db.Order2
                        .Include("Address2")  
                        .ToList();

                    Debug.WriteLine($"Загружено заказов: {allOrders.Count}");
                    ApplyFiltersAndSort();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void ApplyFiltersAndSort()
        {
            if (allOrders == null) return;

            var filtered = allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(currentSearch))
            {
                string searchLower = currentSearch.ToLower().Trim();
                filtered = filtered.Where(o =>
                    o.OrderNumber.ToString().Contains(searchLower));
            }

            if (currentStatusFilter != "Все статусы")
            {
                filtered = filtered.Where(o => o.Status == currentStatusFilter);
            }

            switch (currentSort)
            {
                case "date_desc":
                    filtered = filtered.OrderByDescending(o => o.OrderDate);
                    break;
                case "date_asc":
                    filtered = filtered.OrderBy(o => o.OrderDate);
                    break;
                case "number_asc":
                    filtered = filtered.OrderBy(o => o.OrderNumber);
                    break;
                case "number_desc":
                    filtered = filtered.OrderByDescending(o => o.OrderNumber);
                    break;
                default:
                    filtered = filtered.OrderByDescending(o => o.OrderDate);
                    break;
            }

            OrdersList.ItemsSource = filtered.ToList();
        }

        //поиск
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentSearch = SearchTextBox.Text;
            ApplyFiltersAndSort();
        }

        // выпадающий список со статусами
        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                currentStatusFilter = item.Tag.ToString() == "all" ? "Все статусы" : item.Tag.ToString();
                ApplyFiltersAndSort();
            }
        }

        // первый пустой из выпадающего списка
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                currentSort = item.Tag.ToString();
                ApplyFiltersAndSort();
            }
        }

        // валидация добавления заказа
        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsAdmin)
                {
                    MessageBox.Show("Только администратор может добавлять заказы");
                    return;
                }

                NavigationService.Navigate(new OrderEditPage(null));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        //валидация изменения заказа
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsAdmin) return;

                var button = sender as Button;
                var order = button?.CommandParameter as Order2;

                if (order != null)
                {
                    NavigationService.Navigate(new OrderEditPage(order));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        //валидация удаления заказа
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) return;

            var btn = sender as Button;
            var order = btn?.CommandParameter as Order2;

            if (order == null) return;

            var result = MessageBox.Show($"Удалить заказ №{order.OrderNumber}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                using (var db = new user33Entities())
                {
                    var del = db.Order2.Find(order.ID_Order);
                    if (del != null)
                    {
                        db.Order2.Remove(del);
                        db.SaveChanges();
                    }
                }
                LoadOrders();
            }
        }
    }
}