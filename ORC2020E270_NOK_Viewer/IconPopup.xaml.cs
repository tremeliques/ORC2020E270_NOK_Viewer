using System.Windows.Controls;
using System.ComponentModel;

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
        public IconPopup(string msg, IconPopupType type)
        {
            InitializeComponent();
            tMessage.Text = msg;
            iIcon.Kind = (MaterialDesignThemes.Wpf.PackIconKind)((int)type);
        }
    }
}
