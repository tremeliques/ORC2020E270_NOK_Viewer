using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORC2020E270_NOK_Viewer.Dlls
{
    using System.ComponentModel;
    public class EntryRow
    {
        [Category("Data")]
        [ReadOnly(true)]
        [DisplayName("PSN")]
        public long PSN { get; set; }

        [ReadOnly(true)]
        [DisplayName("Date and Time")]
        public DateTime Event_DateTime { get; set; }

        [ReadOnly(true)]
        [DisplayName("Huf Number")]
        public string Huf_Part_Number { get; set; }

        [ReadOnly(true)]
        [DisplayName("BMW Number")]
        public string BMW_part_number_finish_good { get; set; }

        [ReadOnly(true)]
        [DisplayName("Part name")]
        public string Description { get; set; }

        [ReadOnly(true)]
        [DisplayName("Serial number")]
        public string Part_serial_number { get; set; }

        [Category("Error")]
        [ReadOnly(true)]
        [DisplayName("Error code")]
        public Int16 Error_Code { get; set; }

        [ReadOnly(true)]
        [DisplayName("Error Description")]
        public string Error_Description { get; set; }

        [ReadOnly(true)]
        [DisplayName("Test result")]
        public string Test_Result { get; set; }

        [ReadOnly(true)]
        [DisplayName("Fail limit")]
        public string Test_Limit { get; set; }
    }
}
