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
            Url = $"{lUrl}/loginsession";
            FilePath = lFilePath;
            cts = token;
        }

        async public override Task Execute()
        {
            DateTime todayDate = DateTime.Today;
            var records = await GetRecordsFromDbfAsync<LoginRecord>(FilePath, cts);
            Records = records.Where(record => record.DATE == todayDate).ToList();
        }
    }
}
