using Counter_Emitter.Model;
using System;
using System.Linq;
using static Counter_Emitter.Program;
using System.Threading;
using System.Threading.Tasks;

namespace Counter_Emitter
{
    class LoginStrategy : Strategy
    {
        public LoginStrategy(string lUrl, string lFilePath, CancellationToken token) : base (lUrl, lFilePath, token)
{
            URL = $"{lUrl}/loginsession";
            FILE_PATH = lFilePath;
            Cts = token;
        }

        public override async Task Execute()
        {
            DateTime todayDate = DateTime.Today;
            RECORDS = await GetRecordsFromDbfAsync<LoginRecord>(FILE_PATH, Cts, todayDate);
        }
    }
}
