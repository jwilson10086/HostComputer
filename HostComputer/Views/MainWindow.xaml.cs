using HostComputer.Models;
using HostComputer.Common.Services;
using HostComputer.ViewModels;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace HostComputer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private List<MenuItemModel> _menuData;

        public MainWindow()
        {
            InitializeComponent();
            var nav = new NavigationService(PageHost);
            DataContext = new MainViewModel(nav);
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

    }
 
    
}
