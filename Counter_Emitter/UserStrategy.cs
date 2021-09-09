using Counter_Emitter.Model;
using System;
using static Counter_Emitter.Program;
using System.Threading;
using System.Threading.Tasks;

namespace Counter_Emitter
{
    class UserStrategy : Strategy
    {
        public UserStrategy(string lUrl, string lFilePath, CancellationToken token) : base (lUrl, lFilePath, token)
        {
            URL = $"{lUrl}/counteruser";
            FILE_PATH = lFilePath;
            Cts = token;
        }

        public override async Task Execute()
        {
            RECORDS = await GetRecordsFromDbfAsync<UserRecord>(FILE_PATH, Cts, null);
        }
    }
}
