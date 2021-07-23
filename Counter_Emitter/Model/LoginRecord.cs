using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counter_Emitter.Model
{
    public class LoginRecord : IRecord
    {
        public string USER { get; set; }
        public DateTime DATE { get; set; }
        public string TIMEIN { get; set; }
        public string TIMEOUT { get; set; }
        public string COUNTER { get; set; }
        public LoginRecord() { }
        public LoginRecord(string user, DateTime date, string timeIn, string timeOut, string counter)
        {
            USER = user;
            DATE = date;
            TIMEIN = timeIn;
            TIMEOUT = timeOut;
            COUNTER = counter;
        }
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4}", USER, DATE, TIMEIN, TIMEOUT, COUNTER);
        }
    }
}
