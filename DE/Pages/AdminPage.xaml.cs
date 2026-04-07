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

namespace DE.Pages
{
    
    public partial class AdminPage : Page
    {
        public AdminPage()
        {
            // проверка на роль админа из БД и разблокировка функций роли
            InitializeComponent();
            this.Title = "Окно администратора";
            ProductsPage.CurrentUserRole = "Администратор";
            MainFrame.Navigate(new ProductsPage());
        }
    }
}
