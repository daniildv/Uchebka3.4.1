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
   
    public partial class ManagerPage : Page
    {
        public ManagerPage()
        {
            // проверка на роль менеджера из БД и разблокировка функций 
            InitializeComponent();
            this.Title = "Окно менеджера";
            ProductsPage.CurrentUserRole = "Менеджер";
            MainFrame.Navigate(new ProductsPage());
        }
    }
}
