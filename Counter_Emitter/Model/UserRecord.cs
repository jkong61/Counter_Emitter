using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counter_Emitter.Model
{
    class UserRecord : IRecord
    {
        public string USERID { get; set; }
        public string PASSWORD { get; set; }
        public string MENU { get; set; }
        public string NAME { get; set; }
        public string SNAME { get; set; }
        public string POSITION { get; set; }
        public DateTime DATE { get; set; }
        public string RIGHT { get; set; }
        public string RIGHT1 { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string LANGUAGE { get; set; }
        public Decimal POLIMIT { get; set; }
        public string CARDNO { get; set; }
        public bool ACTIVE {  get; set; }
        public string T1 { get; set; }

        public UserRecord() {}

        public bool ShouldSerializePASSWORD()
        {
            return false;
        }
        public bool ShouldSerializeMENU()
        {
            return false;
        }
        public bool ShouldSerializeRIGHT()
        {
            return false;
        }
        public bool ShouldSerializeRIGHT1()
        {
            return false;
        }
        public bool ShouldSerializeLANGUAGE()
        {
            return false;
        }
        public bool ShouldSerializePOLIMIT()
        {
            return false;
        }
        public bool ShouldSerializeCARDNO()
        {
            return false;
        }
        public bool ShouldSerializeT1()
        {
            return false;
        }
    }
}
