using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Counter_Emitter.Model
{
    class UserRecord : IRecord
    {
        [JsonInclude]
        public string USERID { get; set; }
        public string PASSWORD { get; set; }
        public string MENU { get; set; }
        [JsonInclude]
        public string NAME { get; set; }
        [JsonInclude]
        public string SNAME { get; set; }
        [JsonInclude]
        public string POSITION { get; set; }
        [JsonInclude]
        public DateTime DATE { get; set; }
        public string RIGHT { get; set; }
        public string RIGHT1 { get; set; }
        [JsonInclude]
        public DateTime LASTUPDATE { get; set; }
        public string LANGUAGE { get; set; }
        public decimal POLIMIT { get; set; }
        public string CARDNO { get; set; }
        [JsonInclude]
        public bool ACTIVE {  get; set; }
        public string T1 { get; set; }

        public UserRecord() {}
    }
}
