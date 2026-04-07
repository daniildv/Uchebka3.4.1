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

namespace DE
{
    public partial class LoginWIndow : Window
    {
        private user33Entities db = new user33Entities();
        public LoginWIndow()
        {
            InitializeComponent();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = LoginTextBox.Text.Trim();
                string password = PasswordBox.Password;

                // валидация авторизации
                if (string.IsNullOrWhiteSpace(login))
                {
                    ShowValidationError("Введите логин");
                    LoginTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowValidationError("Введите пароль");
                    PasswordBox.Focus();
                    return;
                }

                // поиск пользователя в БД
                using (var db = new user33Entities())
                {
                    var user = db.User2
                        .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user == null)
                    {
                        ShowErrorMessage("Ошибка авторизации",
                            "Неверный логин или пароль.\n" +
                            "Проверьте правильность ввода и попробуйте снова.");
                        PasswordBox.Password = "";
                        PasswordBox.Focus();
                        return;
                    }

                    MainWindow mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка подключения",
                    $"Не удалось подключиться к базе данных: {ex.Message}\n" +
                    "Проверьте подключение к серверу и повторите попытку.");
            }
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow mainWindow = new MainWindow(null);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка",
                    $"Не удалось войти как гость: {ex.Message}");
            }
        }
        private void ShowValidationError(string message)
        {
            MessageBox.Show(message, "Ошибка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfoMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
