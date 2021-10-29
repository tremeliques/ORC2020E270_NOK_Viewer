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
using MaterialDesignThemes.Wpf;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<SQLView> QueryResults { get; set; }
        public List<string> list { get; set; }

        public MainWindow()
        {
            var rnd = new Random();
            QueryResults = new List<SQLView>()
            {              
               //new SQLView() { ID = 1, Ref = "abc", OkNOk = 1 },
               //new SQLView() { ID = 2, Ref = "dfy", OkNOk = 2 },
               //new SQLView() { ID = 3, Ref = "zxb", OkNOk = 0 },
            };
            for (int i = 0; i < 100; i++)
            {
                QueryResults.Add(new SQLView() { ID = i, Ref = "abc", OkNOk = (short)rnd.Next(0,3) });
            }
            list = new List<string>() { "Shift 1", "Shift 2", "Shift 3", "Custom" };
            InitializeComponent();
        }
        private void UIElement_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //until we had a StaysOpen glag to Drawer, this will help with scroll bars
            var dependencyObject = Mouse.Captured as DependencyObject;

            //while (dependencyObject != null)
            //{
            //    if (dependencyObject is ScrollBar) return;
            //    dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            //}

            MenuToggleButton.IsChecked = false;
        }
        private void ModifyTheme(object sender, RoutedEventArgs e)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(DarkModeToggleButton.IsChecked == true ? Theme.Dark : Theme.Light);
            paletteHelper.SetTheme(theme);
        }
        public class SQLView
        {
            public int ID { get; set; }
            public string Ref { get; set; }
            public short OkNOk { get; set; }
        }
    }    
}
