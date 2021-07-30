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
            Url = $"{lUrl}/counteruser";
            FilePath = lFilePath;
            cts = token;
        }

        async public override Task Execute()
        {
            Records = await GetRecordsFromDbfAsync<UserRecord>(FilePath, cts);
        }
    }
}
