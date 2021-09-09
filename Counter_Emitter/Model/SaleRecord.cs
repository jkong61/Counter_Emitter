using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Counter_Emitter.Model
{
    class SaleRecord : IRecord
    {
        public string RECEIPT { get; set; }
        [JsonIgnore]
        public decimal REC { get; set; }
        [JsonIgnore]
        public string PLU { get; set; }
        [JsonIgnore]
        public string DECS { get; set; }
        [JsonIgnore]
        public decimal PRICE { get; set; }
        [JsonIgnore]
        public decimal AMTDUE { get; set; }
        [JsonIgnore]
        public decimal QTY { get; set; }
        [JsonIgnore]
        public decimal MQTY { get; set; }
        [JsonIgnore]
        public string UOM { get; set; }
        [JsonIgnore]
        public decimal DISC { get; set; }
        [JsonIgnore]
        public string VIP { get; set; }
        [JsonIgnore]
        public string CCNO { get; set; }
        [JsonIgnore]
        public string CCTYPE { get; set; }
        public DateTime DATE { get; set; }
        [JsonIgnore]
        public string USER { get; set; }
        [JsonIgnore]
        public bool GST { get; set; }

    }
}
