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
using System.Globalization;
using System.Threading;
using System.Windows.Markup;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;

using ClosedXML.Excel;

using uBix.Utilities;


namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const String customQueryName = "Custom";
        private const String csvSign = ",";

        private SqlConnection dbCon = new SqlConnection();
        private bool isShiftSelected = false;
        private Shift selectedShift = null;

        #region BindingElements

        public MenuSettings menuSettings { get; private set; } = new MenuSettings();
        
        public List<String> shiftList { get; set; }

        public DataSet queryDataSet = new DataSet();

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

            SetCulture();

            InitializeComponent();
        }
        
        /// <summary>
        /// Set the Specific Culture for app
        /// </summary>
        /// <param name="culture">Culture string, example: "en-US" <see cref="CultureInfo"/></param>
        private void SetCulture(string culture = null)
        {
            /*
             * Starting with the .NET Framework 4.5, you can set the culture and UI culture of all threads in an application domain
             * more directly by assigning a CultureInfo object that represents that culture to the DefaultThreadCurrentCulture and
             * DefaultThreadCurrentUICulture properties. The following example uses these properties to ensure that all threads in
             * the default application domain share the same culture.
             */
            if (string.IsNullOrEmpty(culture)) culture = Application.Current.FindResource("strCulture").ToString();

            try
            {
                var cultureInfo = CultureInfo.CreateSpecificCulture(culture);
                cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                cultureInfo.DateTimeFormat.LongDatePattern = "dd/MM/yyyy";

                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                // Note: if the xml language is set the DatePicker will lose the date and time format 
                //this.Language = XmlLanguage.GetLanguage(cultureInfo.Name);
            }
            // If an exception occurs, we'll just fall back to the system default.
            catch (CultureNotFoundException)
            {
                return;
            }
            catch (ArgumentException)
            {
                return;
            }
        }

        /// <summary>
        /// Load menu settings
        /// </summary>
        /// <param name="searchEnable">Enable or disable search text box</param>
        private void LoadMenuSettings(bool searchEnable = true)
        {
            menuSettings.UISearchElement.IsEnabled = searchEnable;
            menuSettings.UISearchElement.Visibility = menuSettings.UISearchElement.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

            if (searchEnable)
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lbShifts.ItemsSource);
                view.Filter = MenuFilter;
            }
        }

        /// <summary>
        /// Delegate menu filter for list box
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns a boolean value that indicates whether or not the given item should be visible on the list</returns>
        private bool MenuFilter(object obj)
        {
            if (string.IsNullOrEmpty(txBoxItemsSearchBox.Text))
            {
                return true;
            }
            else
            {
                return (obj.ToString().IndexOf(txBoxItemsSearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
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

            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Loading shift list");

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
                    log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, String.Format("Add shift {0}", shift.Name));
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
                shiftObjList[i].UpdateDateAndTime();
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
                    return;
                }
            }

            // there is no running shift to load
            // set custom view
            SelectShift(-1);
        }

        /// <summary>
        /// Select shift based on selected index
        /// </summary>
        /// <param name="index">Selected index</param>
        private void SelectShift(int index)
        {
            isShiftSelected = false;

            // check if selected index is outside of the shiftObjList count
            if ((index < 0) || (index > shiftObjList.Count))
            {
                //IconPopup.ShowDialog("Selected shift index is outside of the range", IconPopupType.Error);
                tShiftName.Text = "Please select manually the time interval";
                stCusomToolBar.IsEnabled = true;
                isShiftSelected = false;

                dpStartDate.SelectedDate = DateTime.Now;
                dpEndDate.SelectedDate = DateTime.Now;

                log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Shift index is outside of the range. Enable custom filter");
                return;
            }

            // check if was selected custom filter
            if (index == shiftObjList.Count)
            {
                tShiftName.Text = customQueryName;
                stCusomToolBar.IsEnabled = true;
                isShiftSelected = false;

                dpStartDate.SelectedDate = DateTime.Now;
                dpEndDate.SelectedDate = DateTime.Now;

                log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Enable custom filter");
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

            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, String.Format("Was selected {0}", selectedShift.Name));
        }

        /// <summary>
        /// Export current dataset to Excel file
        /// </summary>
        /// <param name="excelFilePath">Output Excel file path</param>
        private void ExportDataSetToExcel(String excelFilePath)
        {
            if (!queryDataSet.HasErrors)
            {
                log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "There is no data to export to Excel file");
                IconPopup.ShowDialog("There is no data to export to Excel file", IconPopupType.Error);
                return;
            }

            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, String.Format("Export data to Excel file: {0}", excelFilePath));
            try
            {
                using(XLWorkbook workbook = new XLWorkbook())
                {
                    workbook.Worksheets.Add(queryDataSet);
                    //workbook.Worksheet(0).Name = String.Format("NOK view - {0}", selectedShift.Name);
                    workbook.SaveAs(excelFilePath);
                }

                IconPopup.ShowDialog(String.Format("Data was exported to {0} file", excelFilePath), PackIconKind.MicrosoftExcel);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
                IconPopup.ShowDialog(ex.Message, IconPopupType.Error);
            }
        }

        /// <summary>
        /// Export current dataset to Excel file
        /// </summary>
        private void ExportDataSetToExcel()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            if (isShiftSelected) saveDialog.FileName = String.Format("NOK list - {0}", selectedShift.Name);
            else saveDialog.FileName = "NOK list";

            saveDialog.DefaultExt = ".xlsx";
            saveDialog.Filter = "Excel 2007 (.xlsx)|*.xlsx";

            Nullable<bool> result = saveDialog.ShowDialog();
            if (result == true)
            {
                ExportDataSetToExcel(saveDialog.FileName);
            }
        }

        /// <summary>
        /// Export current dataset to Excel file
        /// </summary>
        /// <param name="csvFilePath">Output CSV file path</param>
        private void ExportDataSetToCSV(String csvFilePath)
        {
            String csvData = "";
            log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "There is no data to export to CSV file");
            try
            {
                DataTable dt = queryDataSet.Tables[0];

                if (dt.Columns.Count == 0)
                {
                    log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "There is no Columns in data table to export to CSV file");
                    IconPopup.ShowDialog("There is no Columns in data table to export to CSV file", IconPopupType.Error);
                    return;
                }

                if (dt.Rows.Count == 0)
                {
                    log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "There is no Rows in data table to export to CSV file");
                    IconPopup.ShowDialog("There is no Rows in data table to export to CSV file", IconPopupType.Error);
                    return;
                }

                using (var textWriter = File.CreateText(csvFilePath))
                {
                    //create csv header
                    csvData = dt.Columns[0].ColumnName;
                    for (int i = 1; i < dt.Columns.Count; i++)
                    {
                        csvData += csvSign + dt.Columns[i].ColumnName;
                    }

                    // write csv header
                    textWriter.WriteLine(csvData);

                    // fill csv file with table content
                    foreach(DataRow row in dt.Rows)
                    {
                        csvData = row[0].ToString();
                        for (int i = 1; i < dt.Columns.Count; i++)
                        {
                            csvData += csvSign + row[i].ToString();
                        }
                        textWriter.WriteLine(csvData);
                    }
                    textWriter.Close();
                }
                IconPopup.ShowDialog(String.Format("Data was exported to {0} file", csvFilePath), PackIconKind.FileDelimitedOutline);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
                IconPopup.ShowDialog(ex.Message, IconPopupType.Error);
            }
            
        }

        /// <summary>
        /// Export current dataset to Excel file
        /// </summary>
        private void ExportDataSetToCSV()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();

            if (isShiftSelected) saveDialog.FileName = String.Format("NOK list - {0}", selectedShift.Name);
            else saveDialog.FileName = "NOK list";

            saveDialog.DefaultExt = ".xlsx";
            saveDialog.Filter = "CSV file(.csv)|*.csv";

            Nullable<bool> result = saveDialog.ShowDialog();
            if (result == true)
            {
                ExportDataSetToCSV(saveDialog.FileName);
            }
        }

        ///// <summary>
        ///// Check SQL database connections.
        ///// If connection is closed try open it
        ///// </summary>
        ///// <returns></returns>
        //private async void CheckDataBaseConnection()
        //{
        //    // check if database connection is open
        //    if (Global.dbCon.State == ConnectionState.Open) return;


        //    try
        //    {
        //        // database connection is closed or broken
        //        // try close it first
        //        log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Database connection is closed or broken");
        //        dbCon.Close();
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
        //    }

        //    try
        //    {
        //        // open connection string
        //        log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, "Try to open database connection");
        //        log.DebugFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, 
        //            String.Format("Connection string: {0}", dbConnectionString));

        //        Global.dbCon.ConnectionString = dbConnectionString;

        //        //dbCon.Open();

        //        TaskRunningPopup tOpenDb = new TaskRunningPopup(null, "Open Database", PackIconKind.Database);
        //        await DialogHost.Show(tOpenDb);

        //        if (!tOpenDb.Result) throw new Exception(tOpenDb.ErrorMsg);
        //    }
        //    catch(Exception ex)
        //    {
        //        log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
        //        IconPopup.ShowDialog("Open Database: " + ex.Message, IconPopupType.Critical);
        //        return;
        //    }
        //}

        #region UIEvents

        /// <summary>
        /// Update query data set
        /// </summary>
        private async void UpdateDataSet(DateTime startDate, DateTime endDate)
        {
            // check if database connection is open
            if (dbCon.State != ConnectionState.Open)
            {
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

                    //dbCon.Open();

                    TaskRunningPopup tOpenDb = new TaskRunningPopup(dbCon.Open, "Open Database", PackIconKind.Database);
                    await DialogHost.Show(tOpenDb);

                    if (!tOpenDb.Result) throw new Exception(tOpenDb.ErrorMsg);
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("[{0}]\t{1}", MethodBase.GetCurrentMethod().Name, ex.Message);
                    IconPopup.ShowDialog("Open Database: " + ex.Message, IconPopupType.Critical);
                    return;
                }
            }

            if (dbCon.State != ConnectionState.Open) return;

            // build SQL query
            String sqlQuery = "SELECT Product_Error_Events.PSN AS [PSN], " +
                                     "Product_Error_Events.Event_DateTime AS [Date and Time], " +
                                     "Product_Quality.Huf_Part_Number AS [Huf Number], " +
                                     "Product_Quality.BMW_part_number_finish_good AS [BMW Number], " +
                                     "Product_Quality.Description AS [Part name], " +
                                     "Product_Quality.Part_serial_number AS [Serial number], " +
                                     "Product_Error_Events.Error_Code AS [Error code], " +
                                     "Product_Error_Events.Error_Description AS [Error Description], " +
                                     "Product_Error_Events.Test_Result AS [Test result], " +
                                     "Product_Error_Events.Test_Limit AS [Fail limit] " +
                                "FROM Product_Error_Events INNER JOIN Product_Quality ON Product_Error_Events.PSN = Product_Quality.PSN " +
                                "WHERE (Product_Error_Events.Event_DateTime >= @startDateTime) AND (Product_Error_Events.Event_DateTime < @endDateTime) " +
                                "ORDER BY Product_Error_Events.Event_DateTime;";



            bCustomShowData.IsEnabled = false;
            try
            {
                queryDataSet.Clear();
                using (SqlCommand sqlCmd = new SqlCommand(sqlQuery, dbCon))
                {
                    sqlCmd.Parameters.Add("startDateTime", SqlDbType.DateTime).Value = startDate;
                    sqlCmd.Parameters.Add("endDateTime", SqlDbType.DateTime).Value = endDate;

                    SqlDataAdapter sqlData = new SqlDataAdapter(sqlCmd);
                    sqlData.Fill(queryDataSet);

                    //Dispatcher.BeginInvoke((Action)(() => dgNokListView.ItemsSource = queryDataSet.CreateDataReader()));
                    dgNokListView.ItemsSource = queryDataSet.CreateDataReader();
                }
                bCustomShowData.IsEnabled = true;
            }
            catch (Exception ex)
            {
                IconPopup.ShowDialog("Update dataset: " + ex.Message, IconPopupType.Error);
                bCustomShowData.IsEnabled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void bCustomShowData_Click(object sender, RoutedEventArgs e)
        {
            DateTime startDT = dpStartDate.SelectedDate.Value.Date;
            startDT = startDT.AddHours(tpStartTime.SelectedTime.Value.Hour);
            startDT = startDT.AddMinutes(tpStartTime.SelectedTime.Value.Minute);
            startDT = startDT.AddSeconds(tpStartTime.SelectedTime.Value.Second);

            DateTime endDT = dpEndDate.SelectedDate.Value.Date;
            endDT = endDT.AddHours(tpEndTime.SelectedTime.Value.Hour);
            endDT = endDT.AddMinutes(tpEndTime.SelectedTime.Value.Minute);
            endDT = endDT.AddSeconds(tpEndTime.SelectedTime.Value.Second);

            UpdateDataSet(startDT, endDT);
            isShiftSelected = false;
        }

        private void lbShifts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectShift(lbShifts.SelectedIndex);
            
            if (isShiftSelected) UpdateDataSet(selectedShift.StartDateTime, selectedShift.EndDateTime);
            else
            {
                dgNokListView.ItemsSource = null;
            }
        }

        private void DialogHost_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void dgNokListView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadShiftList();
            LoadMenuSettings(searchEnable: true);
            SetStartupSift();
            if (isShiftSelected) UpdateDataSet(selectedShift.StartDateTime, selectedShift.EndDateTime);
            else
            {
                dgNokListView.ItemsSource = null;
            }
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
                (e.Column as System.Windows.Controls.DataGridTextColumn).Binding.StringFormat = "dd/MM/yyyy HH:mm:ss";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dbCon.State == ConnectionState.Open) dbCon.Close();
            dbCon.Dispose();

            queryDataSet.Dispose();

            shiftList.Clear();
        }

        /// <summary>
        /// Text changed event for search box in the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txBoxItemsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lbShifts.ItemsSource).Refresh();
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

        private void lbOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // export to excel
            switch(lbOptions.SelectedIndex)
            {
                case 0:
                    ExportDataSetToExcel();
                    break;

                case 1:
                    ExportDataSetToCSV();
                    break;
            }



            lbOptions.UnselectAll();
        }

        #endregion UIEvents


    }
}
