using System;
using System.Collections.Generic;
using System.Reflection;
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
        private string dbConnectionString = "";
        private string shiftsIniFile = "";
        private log4net.ILog log = log4net.LogManager.GetLogger("");

        #endregion Settings

        private class Shifts
        {
            public String Name { private set; get; }
            public DateTime StartDateTime { private set; get; }
            public DateTime EndDateTime { private set; get; }
            public int ShitfDuration { private set; get; }
            public String ShiftWeekPattern { private get; set; }
            public bool IsSunday { private set; get; }
            public bool IsMonday { private set; get; }
            public bool IsTuesday { private set; get; }
            public bool IsWednesday { private set; get; }
            public bool IsThursday { private set; get; }
            public bool IsFriday { private set; get; }
            public bool IsSaturday { private set; get; }

            private String startTime;

            public Shifts(String name, String startTime, int shiftDuration, String shiftWeekPattern)
            {
                ResetFields();
                this.Name = name;
                this.ShitfDuration = shiftDuration;
                this.ShiftWeekPattern = shiftWeekPattern;
                this.startTime = startTime;

                StartDateTime = Convert.ToDateTime(startTime);
                EndDateTime = StartDateTime.AddMinutes(shiftDuration);
                ParsingShiftWeekPattern();
            }

            /// <summary>
            /// Reset object fields
            /// </summary>
            private void ResetFields()
            {
                Name = "";
                StartDateTime = DateTime.MinValue;
                EndDateTime = DateTime.MinValue;
                ShitfDuration = 0;
                ShiftWeekPattern = "";
                IsSunday = false;
                IsMonday = false;
                IsTuesday = false;
                IsWednesday = false;
                IsThursday = false;
                IsFriday = false;
                IsSaturday = false;
            }

            /// <summary>
            /// Parsing shift week pattern string
            /// </summary>
            private void ParsingShiftWeekPattern()
            {
                String[] separator = { "," };
                String[] pars = ShiftWeekPattern.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                if (pars.Length == 0) return;

                if (pars.Contains("1")) IsSunday = true;
                if (pars.Contains("2")) IsMonday = true;
                if (pars.Contains("3")) IsTuesday = true;
                if (pars.Contains("4")) IsWednesday = true;
                if (pars.Contains("5")) IsThursday = true;
                if (pars.Contains("6")) IsFriday = true;
                if (pars.Contains("7")) IsSaturday = true;
            }

            /// <summary>
            /// Update start and end date time
            /// </summary>
            public void UpdateDateAndTime()
            {
                DateTime now = DateTime.Now;
                if ((now >= StartDateTime) && (now <= EndDateTime)) return;
                else
                {
                    StartDateTime = Convert.ToDateTime(startTime);
                    EndDateTime = StartDateTime.AddMinutes(ShitfDuration);
                }
            }
        }
        private List<Shifts> shiftObjList = new List<Shifts>();

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
            LoadSettings();
            LoadShiftList();
        }

        private void bCustomShowData_Click(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Load settings from settings.ini file
        /// </summary>
        private void LoadSettings()
        {
            // check if settings files exists
            if (!File.Exists(settingsIniFile))
            {
                IconPopup.ShowDialog(String.Format("File {0} does not exist! Application will be closed.", settingsIniFile), IconPopupType.Error);
                System.Windows.Application.Current.Shutdown();
            }

            String sTmp = "";
            IniFile settings = new IniFile(settingsIniFile);

            // load application logger name
            if (!settings.readString("Global", "LoggerName", out sTmp)) sTmp = "";

            // get logger
            Logger.XmlLoggerConfiguration(sTmp);
            log = Logger.GetLogger("Application");

            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Loading application settings...");

            if (!settings.readUInt("Global", "RefreshTime", out queryRefreshTime)) queryRefreshTime = 30;
            if (!settings.readString("Global", "ShiftsIniFile", out shiftsIniFile)) shiftsIniFile = @".\Config\Shifts.ini";

            // reading database connection string
            settings.RemoveCommentSign(";");
            if (!settings.readString("Database", "ConnectionString", out dbConnectionString))
            {
                sTmp = "There is no connection string parameters under Database header";
                log.FatalFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, sTmp);
                IconPopup.ShowDialog(sTmp + ". Application will be closed", IconPopupType.Critical);
                System.Windows.Application.Current.Shutdown();
            }
            settings.AddCommentSign(";");

            // debug load settings
            log.DebugFormat("[{0}]\t[{1}] {2} = {3}", MethodBase.GetCurrentMethod().Name, "Global", "RefreshTime", queryRefreshTime);
            log.DebugFormat("[{0}]\t[{1}] {2} = {3}", MethodBase.GetCurrentMethod().Name, "Global", "ShiftsIniFile", shiftsIniFile);
            log.DebugFormat("[{0}]\t[{1}] {2} = {3}", MethodBase.GetCurrentMethod().Name, "GloDatabasebal", "ConnectionString", dbConnectionString);

            settings.Dispose();
        }

        /// <summary>
        /// Load shift list from ini file
        /// </summary>
        private void LoadShiftList()
        {
            shiftObjList.Clear();
            shiftList.Clear();

            // check if shifts files exists
            if (!File.Exists(shiftsIniFile))
            {
                IconPopup.ShowDialog(String.Format("File {0} does not exist!", shiftsIniFile), IconPopupType.Error);
            }
            else
            {
                using (IniFile shifts = new IniFile(shiftsIniFile))
                {
                    String[] headers = shifts.GetNodesNames();
                    if (headers.Length > 0)
                    {
                        shiftObjList.Clear();
                        String name;
                        String startTime;
                        int shitDuration;
                        String shiftWeekPattern;

                        foreach(String header in headers)
                        {
                            if (!shifts.readString(header, "Name", out name)) name = "";
                            if (!shifts.readInt(header, "ShiftDuration", out shitDuration)) shitDuration = 0;
                            if (!shifts.readString(header, "StartTime", out startTime)) startTime = "";
                            if (!shifts.readString(header, "ShiftWeekPattern", out shiftWeekPattern)) shiftWeekPattern = "";

                            if ((name != "") && (startTime != ""))
                                shiftObjList.Add(new Shifts(name, startTime, shitDuration, shiftWeekPattern));
                        }
                    }
                }
            }

            if (shiftObjList.Count > 0)
            {
                foreach(Shifts shift in shiftObjList)
                {
                    shiftList.Add(shift.Name);
                }
            }
            shiftList.Add("Custom");
        }
    }    
}
