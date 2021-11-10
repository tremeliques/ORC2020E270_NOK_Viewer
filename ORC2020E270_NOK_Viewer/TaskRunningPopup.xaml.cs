using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Data.SqlClient;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for TaskRunningPopup.xaml
    /// </summary>
    public partial class TaskRunningPopup : UserControl
    {
        private Action toExec;

        public bool Result { private set; get; }
        public String ErrorMsg { private set; get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="taskName">Message to be displayed</param>
        /// <param name="icon">Icon popup</param>
        public TaskRunningPopup(Action toExec, string taskName, PackIconKind icon)
        {
            this.toExec = toExec;
            InitializeComponent();
            tMessage.Text = taskName;
            iIcon.Kind = icon;

            Task.Run(new Action(AsyncTaskRunner))
                .ConfigureAwait(true)
                .GetAwaiter()
                .OnCompleted(() =>
                {
                    DialogHost.CloseDialogCommand.Execute(null, this);
                });
        }

        private void ProgressBar_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// Function to call and run async function
        /// </summary>
        private void AsyncTaskRunner()
        {
            //System.Threading.Thread.Sleep(1000);
            try
            {
                //Global.dbCon.Open();

                this.toExec();

                Result = true;
                ErrorMsg = "";
            }
            catch (Exception ex)
            {
                Result = false;
                ErrorMsg = ex.Message;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void popup_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }


}
