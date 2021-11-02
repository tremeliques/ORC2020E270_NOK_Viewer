using System.Windows.Controls;
using System.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace ORC2020E270_NOK_Viewer
{
    public enum IconPopupType : int
    {
        [Description("Information")]
        Information = MaterialDesignThemes.Wpf.PackIconKind.Information,

        [Description("Alert")]
        Alert = MaterialDesignThemes.Wpf.PackIconKind.Alert,

        [Description("AlertCircle")]
        Error = MaterialDesignThemes.Wpf.PackIconKind.AlertCircle,

        [Description("MinusCircle")]
        Critical = MaterialDesignThemes.Wpf.PackIconKind.MinusCircle,

        [Description("CheckboxMarkedCircle")]
        Accept = MaterialDesignThemes.Wpf.PackIconKind.CheckboxMarkedCircle
    }

    /// <summary>
    /// Interaction logic for IconPopup.xaml
    /// </summary>
    public partial class IconPopup : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="type">Icon popup</param>
        public IconPopup(string msg, IconPopupType type)
        {
            InitializeComponent();
            tMessage.Text = msg;
            iIcon.Kind = (MaterialDesignThemes.Wpf.PackIconKind)((int)type);
        }

        /// <summary>
        /// Display Icon Popup message
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="type">Icon popup</param>
        public async static void ShowDialog(string msg, IconPopupType type)
        {
            IconPopup obj = new IconPopup(msg, type);
            await DialogHost.Show(obj, "RootDialog");
        }
    }


}
