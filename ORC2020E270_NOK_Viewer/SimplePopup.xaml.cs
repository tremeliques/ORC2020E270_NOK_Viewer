using System.Windows.Controls;

namespace ORC2020E270_NOK_Viewer
{
    /// <summary>
    /// Interaction logic for SimplePopup.xaml
    /// </summary>
    public partial class SimplePopup : UserControl
    {
        public SimplePopup(string msg)
        {
            InitializeComponent();
            tMessage.Text = msg;
        }
    }
}
