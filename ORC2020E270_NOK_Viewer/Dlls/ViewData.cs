using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORC2020E270_NOK_Viewer.Dlls
{
    using System.Windows.Media;
    public class ViewData
    {
        public EntryRow SelectedRow { get; set; }

        public ViewData()
        {
            this.SelectedRow = new EntryRow
            {
                PSN = 12345,
                Event_DateTime = DateTime.Now,
                Huf_Part_Number = "123",
                BMW_part_number_finish_good = "456",
                Description = "Right part",
                Part_serial_number = "KJNVTBH",
                Error_Code = 1003,
                Error_Description = "PLC fail",
                Test_Result = "34",
                Test_Limit = "35"
            };
        }
    }
}
