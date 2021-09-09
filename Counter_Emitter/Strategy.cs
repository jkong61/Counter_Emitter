using Counter_Emitter.Model;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Counter_Emitter
{
    abstract class Strategy
    {
        public CancellationToken Cts;
        public string URL { get; set; }
        public string FILE_PATH { get; set; }
        public IList<IRecord> RECORDS { get; set; }

        protected Strategy(string lUrl, string lFilePath, CancellationToken lToken)
        {
            URL = lUrl;
            FILE_PATH = lFilePath;
            Cts = lToken;
            RECORDS = new List<IRecord>();
        }

        public abstract Task Execute();
        public virtual void Clear()
        {
            RECORDS.Clear();
        }
    }
}