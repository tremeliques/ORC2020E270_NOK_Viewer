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
using System.IO;
using MaterialDesignThemes.Wpf;

using uBix.Utilities;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<SQLView> QueryResults { get; set; }
        public List<string> shiftList { get; set; }

        #region Settings

        private const String settingsIniFile = @".\Config\Settings.ini";

        private uint queryRefreshTime = 30;
        private log4net.ILog log = log4net.LogManager.GetLogger("");

        #endregion Settings

        public MainWindow()
        {
            var rnd = new Random();
            QueryResults = new List<SQLView>();

            for (int i = 0; i < 100; i++)
            {
                QueryResults.Add(new SQLView() { ID = i, Ref = "abc", OkNOk = (short)rnd.Next(0,3) });
            }

            //list = new List<string>() { "Shift 1", "Shift 2", "Shift 3", "Custom" };
            shiftList = new List<string>();
            shiftList.Clear();
            InitializeComponent();
        }

        private void UIElement_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //until we had a StaysOpen flag to Drawer, this will help with scroll bars
            var dependencyObject = Mouse.Captured as DependencyObject;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            shiftList.Add("Shift 1");
            shiftList.Add("Shift 2");
            shiftList.Add("Shift 3");
            shiftList.Add("Custom");
        }

        private async void ShowMsg(String msg)
        {
            var msgPopup = new IconPopup(msg, IconPopupType.Accept);
            await DialogHost.Show(msgPopup, "RootDialog");
        }

        private void bCustomShowData_Click(object sender, RoutedEventArgs e)
        {
            ShowMsg("_olá mundo_");
        }

        /// <summary>
        /// Load settings from settings.ini file
        /// </summary>
        private void LoadSettings()
        {
            // check if settings files exists
            if (!File.Exists(settingsIniFile))
            {
                var s = new DialogHost();
                s.ShowDialog(this);
            }

            String sTmp = "";
            IniFile settings = new IniFile(settingsIniFile);



            Logger.XmlLoggerConfiguration(sTmp);
            log = Logger.GetLogger("Application");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var s = new DialogHost();
            s.Content = "ola mundo";
            s.ShowDialog(this);
        }
    }    
}
