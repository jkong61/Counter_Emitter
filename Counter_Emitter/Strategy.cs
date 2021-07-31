using Counter_Emitter.Model;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Counter_Emitter
{
    abstract class Strategy
    {
        public CancellationToken cts;
        public string Url { get; set; }
        public string FilePath { get; set; }
        public IList<IRecord> Records { get; set; }

        protected Strategy(string lUrl, string lFilePath, CancellationToken lToken)
        {
            Url = lUrl;
            FilePath = lFilePath;
            cts = lToken;
            Records = new List<IRecord>();
        }

        public abstract Task Execute();
        public virtual void Clear()
        {
            Records.Clear();
        }
    }
}