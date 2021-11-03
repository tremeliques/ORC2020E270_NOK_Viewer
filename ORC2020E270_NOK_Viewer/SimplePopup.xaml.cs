using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for SimplePopup.xaml
    /// </summary>
    public partial class SimplePopup : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        public SimplePopup(string msg)
        {
            InitializeComponent();
            tMessage.Text = msg;
        }

        /// <summary>
        /// Display Simple Popup message
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        public async static void ShowDialog(string msg)
        {
            SimplePopup obj = new SimplePopup(msg);
            await DialogHost.Show(obj, "RootDialog");
        }
    }
}
