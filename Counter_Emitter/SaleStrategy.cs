using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Counter_Emitter.Model;
using NDbfReader;

namespace Counter_Emitter
{
    class SaleStrategy : Strategy
    {
        public SaleStrategy(string lUrl, string lFilePath, CancellationToken token) : base(lUrl, lFilePath, token)
        {
            URL = $"{lUrl}/salesreceipt";
            FILE_PATH = lFilePath;
            Cts = token;
        }
        public override async Task Execute()
        {
            Console.WriteLine($"Reading DBF from ..{FILE_PATH}");
            var records = new List<IRecord>();
            try
            {
                using (Table table = await Table.OpenAsync(FILE_PATH, Cts))
                {
                    try
                    {
                        var reader = table.OpenReader(Encoding.UTF8);
                        while (await reader.ReadAsync(Cts))
                        {
                            SaleRecord record = new SaleRecord();
                            records.Add(record);
                        }
                    }
                    catch (Exception)
                    {
                        throw new InvalidDataException("DBF Table is likely in the wrong format.");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException($"{FILE_PATH} not found, ensure DBF location properly configured.");
            }
            LogWriterCounter.LogWrite("Able to read POS file.");
            RECORDS.Clear();
        }
    }
}