using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
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
        private const String customQueryName = "Custom";

        private SqlConnection dbCon = new SqlConnection();
        private bool isShiftSelected = false;
        private Shift selectedShift = null;

        #region BindingElements

        public MenuSettings menuSettings { get; private set; } = new MenuSettings();
        
        public List<String> shiftList { get; set; }

        public DataSet queryDataSet = new DataSet("Shift_NOK_Counter");


        //public String SelectedShiftName { get; set; }

        //public Boolean CustomBarEnable { get; set; } = false;
        //public DateTime StartDate { get; set; } = DateTime.Now;
        //public DateTime StartTime { get; set; } = DateTime.Now;
        //public DateTime EndDate { get; set; } = DateTime.Now;
        //public DateTime EndTime { get; set; } = DateTime.Now;

        #endregion BindingElements

        #region Settings

        private const String settingsIniFile = @".\Config\Settings.ini";

        private uint queryRefreshTime = 30;
        private string dbConnectionString = "";
        private string shiftsIniFile = "";
        private log4net.ILog log = log4net.LogManager.GetLogger("");

        #endregion Settings

        public class Shift
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
            private int[] iWeekParsing = new int[7];

            public Shift(String name, String startTime, int shiftDuration, String shiftWeekPattern)
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

                for (byte i = 0; i < 7; i++)
                    iWeekParsing[i] = -1;
            }

            /// <summary>
            /// Parsing shift week pattern string
            /// </summary>
            private void ParsingShiftWeekPattern()
            {
                String[] separator = { "," };
                String[] pars = ShiftWeekPattern.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                if (pars.Length == 0)
                {
                    //check if it is a numeric value
                    int iTmp;
                    if (int.TryParse(ShiftWeekPattern, out iTmp))
                    {
                        iWeekParsing[0] = iTmp;
                    }
                }
                else
                {
                    for (byte i = 0; i < pars.Length; i++)
                    {
                        if (!int.TryParse(pars[i], out iWeekParsing[i])) iWeekParsing[i] = -1;
                    }
                }

                if (iWeekParsing.Contains((int)DayOfWeek.Sunday)) IsSunday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Monday)) IsMonday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Tuesday)) IsTuesday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Wednesday)) IsWednesday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Thursday)) IsThursday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Friday)) IsFriday = true;
                if (iWeekParsing.Contains((int)DayOfWeek.Saturday)) IsSaturday = true;
            }

            /// <summary>
            /// Update start and end date time
            /// </summary>
            public void UpdateDateAndTime()
            {
                DateTime now = DateTime.Now;
                if ((now >= StartDateTime) && (now <= EndDateTime)) return;

                StartDateTime = Convert.ToDateTime(startTime);
                EndDateTime = StartDateTime.AddMinutes(ShitfDuration);

                // check if was elapsed
                if (StartDateTime > DateTime.Now)
                {
                    StartDateTime = StartDateTime.AddDays(-1);
                    EndDateTime = EndDateTime.AddDays(-1);
                }
            }

            /// <summary>
            /// Check if this shift is the current running shift
            /// </summary>
            /// <returns>Returns true if it is the current running shift</returns>
            public bool IsCurrentShift()
            {
                DateTime now = DateTime.Now;
                int weekDay = (int)now.DayOfWeek;

                // check if current day is matching with week pattern
                if (!iWeekParsing.Contains(weekDay)) return false;

                // check if current date time is inside of shift start/end time
                if ((now >= StartDateTime) && (now <= EndDateTime)) return true;
                else return false;
            }
        }
        private List<Shift> shiftObjList = new List<Shift>();
        
        /// <summary>
        /// Menu settings - Search text box
        /// </summary>
        public class MenuSettings
        {
            public UIElement UISearchElement { get; private set; }

            public MenuSettings()
            {
                UISearchElement = new UIElement();
                SetToDefault();
            }

            public void SetToDefault()
            {
                this.UISearchElement.IsEnabled = true;
                this.UISearchElement.Visibility = Visibility.Visible;
            }
        }

        public MainWindow()
        {
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

        /// <summary>
        /// Event to switch between dark and light theme
        /// </summary>
        /// <param name="sender">Object sender</param>
        /// <param name="e">Event Arguments</param>
        private void ModifyTheme(object sender, RoutedEventArgs e)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();

            theme.SetBaseTheme(DarkModeToggleButton.IsChecked == true ? Theme.Dark : Theme.Light);
            paletteHelper.SetTheme(theme);
        }

        /// <summary>
        /// Load menu settings
        /// </summary>
        /// <param name="searchEnable">Enable or disable search text box</param>
        private void LoadMenuSettings(bool searchEnable = true)
        {
            menuSettings.UISearchElement.IsEnabled = searchEnable;
            menuSettings.UISearchElement.Visibility = menuSettings.UISearchElement.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
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

            // set logger file path
            if (!settings.readString("Global", "LogConfigFile", out sTmp)) sTmp = "";
            Logger.XmlLoggerConfiguration(sTmp);

            // load application logger name
            if (!settings.readString("Global", "LoggerName", out sTmp)) sTmp = "";

            log = Logger.GetLogger(sTmp);

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
                                shiftObjList.Add(new Shift(name, startTime, shitDuration, shiftWeekPattern));
                        }
                    }
                }
            }

            if (shiftObjList.Count > 0)
            {
                foreach(Shift shift in shiftObjList)
                {
                    shiftList.Add(shift.Name);
                }
            }
            shiftList.Add(customQueryName);
        }

        /// <summary>
        /// Set current shift after app startup
        /// If no shift matching, data grid will be set to empty
        /// </summary>
        private void SetStartupSift()
        {
            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Setting startup shift...");
            if (shiftObjList.Count == 0) return;

            // check what is the first shift is meeting current date time
            for (int i = 0; i < shiftObjList.Count; i++)
            {
                if (shiftObjList[i].IsCurrentShift())
                {
                    SelectShift(i);
                    if (isShiftSelected)
                    {
                        log.InfoFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name,
                            String.Format("Startup selected shift (index: {0}) {1}", i, selectedShift.Name));
                    }
                    else
                    {
                        log.InfoFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Shift was not selected");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Select shift based on selected index
        /// </summary>
        /// <param name="index">Selected index</param>
        private void SelectShift(int index)
        {
            // check if selected index is outside of the shiftObjList count
            if ((index < 0) || (index > shiftObjList.Count))
            {
                IconPopup.ShowDialog("Selected shift index is outside of the range", IconPopupType.Error);
                tShiftName.Text = "Please select manually the time interval";
                stCusomToolBar.IsEnabled = true;
                isShiftSelected = false;
                return;
            }

            // check if was selected custom filter
            if (index == shiftObjList.Count)
            {
                tShiftName.Text = customQueryName;
                stCusomToolBar.IsEnabled = true;
                isShiftSelected = false;
                return;
            }

            // shift was selected
            // load settings

            selectedShift = shiftObjList[index];
            selectedShift.UpdateDateAndTime();

            dpStartDate.SelectedDate = selectedShift.StartDateTime;
            tpStartTime.SelectedTime = selectedShift.StartDateTime;
            dpEndDate.SelectedDate = selectedShift.EndDateTime;
            tpEndTime.SelectedTime = selectedShift.EndDateTime;
            tShiftName.Text = selectedShift.Name;

            stCusomToolBar.IsEnabled = false;
            isShiftSelected = true;
        }

        /// <summary>
        /// Check SQL database connections.
        /// If connection is closed try open it
        /// </summary>
        /// <returns></returns>
        private bool CheckDataBaseConnection()
        {
            // check if database connection is open
            if (dbCon.State == ConnectionState.Open) return true;


            try
            {
                // database connection is closed or broken
                // try close it first
                log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Database connection is closed or broken");
                dbCon.Close();
            }
            catch (Exception ex)
            {
                log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
            }

            try
            {
                // open connection string
                log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Try to open database connection");
                log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, 
                    String.Format("Connection string: {0}", dbConnectionString));

                dbCon.ConnectionString = dbConnectionString;
                dbCon.Open();
            }
            catch(Exception ex)
            {
                log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
                IconPopup.ShowDialog("Open Database: " + ex.Message, IconPopupType.Critical);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update query data set
        /// </summary>
        private void UpdateDataSet()
        {
            /*
             string connectionString = "Data Source=.;Initial Catalog=pubs;Integrated Security=True";
            string sql = "SELECT * FROM Authors";
            SqlConnection connection = new SqlConnection(connectionString);
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, connection);
            DataSet ds = new DataSet();
            connection.Open();
            dataadapter.Fill(ds, "Authors_table");
            connection.Close();
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "Authors_table";             
             */

            if (selectedShift == null) return;
            if (!isShiftSelected) return;

            // check database connection state
            if (!CheckDataBaseConnection()) return;

            // build SQL query
            String sqlQuery = "SELECT Product_Error_Events.PSN, " +
                                     "Product_Error_Events.Event_DateTime, " +
                                     "Product_Quality.Huf_Part_Number, " +
                                     "Product_Quality.BMW_part_number_finish_good, " +
                                     "Product_Quality.Description, " +
                                     "Product_Quality.Part_serial_number, " +
                                     "Product_Error_Events.Error_Code, " +
                                     "Product_Error_Events.Error_Description, " +
                                     "Product_Error_Events.Test_Result, " +
                                     "Product_Error_Events.Test_Limit " +
                                "FROM Product_Error_Events INNER JOIN Product_Quality ON Product_Error_Events.PSN = Product_Quality.PSN " +
                                "WHERE ([Product_Error_Events.Event_DateTime] >= @startDateTime) AND ([Product_Error_Events.Event_DateTime] < @endDateTime)";

            try
            {
                using (SqlCommand sqlCmd = new SqlCommand(sqlQuery, dbCon))
                {
                    sqlCmd.Parameters.Add("startDateTime", SqlDbType.DateTime).Value = selectedShift.StartDateTime;
                    sqlCmd.Parameters.Add("endDateTime", SqlDbType.DateTime).Value = selectedShift.EndDateTime;

                    using (SqlDataAdapter sqlData = new SqlDataAdapter(sqlCmd))
                    {
                        sqlData.Fill(queryDataSet);
                    }
                }

                //dgNokListView
            }
            catch (Exception ex)
            {
                IconPopup.ShowDialog("Update dataset: " + ex.Message, IconPopupType.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadShiftList();
            LoadMenuSettings(searchEnable: true);
            SetStartupSift();
            UpdateDataSet();
        }

        private void bCustomShowData_Click(object sender, RoutedEventArgs e)
        {
        }

        private void lbShifts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectShift(lbShifts.SelectedIndex);
        }
    }    
}
