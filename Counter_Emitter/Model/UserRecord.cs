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
        public string USERID { get; set; }
        [JsonIgnore]
        public string PASSWORD { get; set; }
        [JsonIgnore]
        public string MENU { get; set; }
        public string NAME { get; set; }
        public string SNAME { get; set; }
        public string POSITION { get; set; }
        public DateTime DATE { get; set; }
        [JsonIgnore]
        public string RIGHT { get; set; }
        [JsonIgnore]
        public string RIGHT1 { get; set; }
        public DateTime LASTUPDATE { get; set; }
        [JsonIgnore]
        public string LANGUAGE { get; set; }
        [JsonIgnore]
        public decimal POLIMIT { get; set; }
        [JsonIgnore]
        public string CARDNO { get; set; }
        public bool ACTIVE {  get; set; }
        [JsonIgnore]
        public string T1 { get; set; }

        public UserRecord() {}
    }
}
