using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

using System;
using System.Reflection;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for InfoPopup.xaml
    /// </summary>
    public partial class InfoPopup : UserControl
    {
        private Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;

        public String AppVersion
        {
            get
            {
#if DEBUG
                return String.Format("Debug version {0}.{1}.{2}.{3}", appVersion.Major, appVersion.Minor, appVersion.Build, appVersion.Revision);
#else
                return String.Format("Release version {0}.{1}.{2}.{3}", appVersion.Major, appVersion.Minor, appVersion.Build, appVersion.Revision);
#endif
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public InfoPopup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Display Simple Popup message
        /// </summary>
        public async static void ShowDialog()
        {
            InfoPopup obj = new InfoPopup();
            await DialogHost.Show(obj, "RootDialog");
        }
    }
}
