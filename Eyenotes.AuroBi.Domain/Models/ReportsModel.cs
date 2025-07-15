using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Models
{
    public class ReportsModel
    {
        public int id { get; set; }
        public string ReportName { get; set; }
        public string ReportQuery { get; set; }
    }
}
