using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace DE
{
    public partial class MainWindow : Window
    {
        private User2 currentUser;
        private Stack<Page> navigationHistory = new Stack<Page>();

        public event NavigatedEventHandler Navigated;

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(User2 user)
        {
            InitializeComponent();
            try
            {
                currentUser = user;
                UserNameText.Text = currentUser?.FullName ?? "Гость";

             
                UpdateNavigationButtons();

                NavigateToStartPage();

                string roleMessage = currentUser == null ?
                    "Вы вошли как гость, некоторые функции будут недоступны." :
                    $"Добро пожаловать, {currentUser.FullName}! Ваша роль: {currentUser.UserRole}";

                ShowInfoMessage("Авторизация", roleMessage);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка инициализации",
                    $"Не удалось загрузить начальную страницу: {ex.Message}\n" +
                    "Пожалуйста, перезапустите приложение.");
            }
        }


        private void UpdateNavigationButtons()
        {
            ProductsNavButton.Visibility = Visibility.Visible;

            if (currentUser != null &&
                (currentUser.UserRole == "Администратор" || currentUser.UserRole == "Менеджер"))
            {
                OrdersNavButton.Visibility = Visibility.Visible;
            }
            else
            {
                OrdersNavButton.Visibility = Visibility.Collapsed;
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

        private void ShowWarningMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void NavigateToStartPage()
        {
            try
            {
            
                var mainPage = new Pages.MainPage();

                Pages.MainPage.CurrentUserRole = currentUser?.UserRole ?? "Гость";
                Pages.MainPage.CurrentUserName = currentUser?.FullName ?? "Гость";

                navigationHistory.Clear();
                MainFrame.Navigate(mainPage);
                PageTitleText.Text = "Главная";
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка навигации",
                    $"Не удалось перейти на страницу: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (navigationHistory.Count > 0)
                {
                    Page previousPage = navigationHistory.Pop();
                    MainFrame.Navigate(previousPage);
                    UpdatePageTitle(previousPage);
                }
                UpdateBackButton();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка навигации",
                    $"Не удалось вернуться на предыдущую страницу: {ex.Message}");
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            try
            {
                if (MainFrame.Content is Page currentPage)
                {
                    UpdatePageTitle(currentPage);
                }

                UpdateBackButton();
                Navigated?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка обновления интерфейса",
                    $"Произошла ошибка при обновлении страницы: {ex.Message}");
            }
        }

        private void UpdatePageTitle(Page page)
        {
            if (page is Pages.MainPage)
                PageTitleText.Text = "Главная";
            else if (page is Pages.ProductsPage)
                PageTitleText.Text = "Каталог товаров";
            else if (page is Pages.OrdersPage)
                PageTitleText.Text = "Управление заказами";
            else if (page is Pages.ClientPage)
                PageTitleText.Text = "Личный кабинет клиента";
            else if (page is Pages.ManagerPage)
                PageTitleText.Text = "Панель менеджера";
            else if (page is Pages.AdminPage)
                PageTitleText.Text = "Панель администратора";
        }

        private void UpdateBackButton()
        {
            BackButton.IsEnabled = navigationHistory.Count > 0;
        }

        public void NavigateToPage(Page page)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"NavigateToPage вызван для страницы: {page.GetType().Name}");

                if (MainFrame.Content is Page currentPage)
                {
                    System.Diagnostics.Debug.WriteLine($"Сохраняем в историю текущую страницу: {currentPage.GetType().Name}");
                    navigationHistory.Push(currentPage);
                }

            
                if (page is Pages.OrdersPage)
                {
                    Pages.OrdersPage.CurrentUserRole = currentUser?.UserRole ?? "Гость";
                }
                else if (page is Pages.ProductsPage)
                {
                    Pages.ProductsPage.CurrentUserRole = currentUser?.UserRole ?? "Гость";
                }

                MainFrame.Navigate(page);
                UpdatePageTitle(page);
                UpdateBackButton();

                System.Diagnostics.Debug.WriteLine($"Навигация выполнена, история содержит {navigationHistory.Count} страниц");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка навигации: {ex.Message}");
                ShowErrorMessage("Ошибка навигации",
                    $"Не удалось перейти на страницу: {ex.Message}");
            }
        }

        public void GoBack()
        {
            try
            {
                if (navigationHistory.Count > 0)
                {
                    Page previousPage = navigationHistory.Pop();
                    MainFrame.Navigate(previousPage);
                    UpdatePageTitle(previousPage);
                    UpdateBackButton();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка навигации",
                    $"Не удалось вернуться на предыдущую страницу: {ex.Message}");
            }
        }

        private void ProductsNavButton_Click(object sender, RoutedEventArgs e)
        {
            var productsPage = new Pages.ProductsPage();
            Pages.ProductsPage.CurrentUserRole = currentUser?.UserRole ?? "Гость";
            this.Title = "Товары";
            NavigateToPage(productsPage);
        }

        // заказы закрыты только для гостя
        private void OrdersNavButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null || currentUser.UserRole == "Гость")
            {
                OrdersNavButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                OrdersNavButton.Visibility = Visibility.Visible;
            }
            var ordersPage = new Pages.OrdersPage();
            Pages.OrdersPage.CurrentUserRole = currentUser?.UserRole ?? "Гость";
            this.Title = "Заказы";
            NavigateToPage(ordersPage);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы действительно хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
             
                    Pages.MainPage.CurrentUserRole = "Гость";
                    Pages.ProductsPage.CurrentUserRole = "Гость";
                    Pages.OrdersPage.CurrentUserRole = "Гость";

                    LoginWIndow loginWindow = new LoginWIndow();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка выхода",
                    $"Не удалось выполнить выход из системы: {ex.Message}");
            }
        }

        public User2 CurrentUser => currentUser;
    }
}