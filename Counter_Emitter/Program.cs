using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using NDbfReader;
using Counter_Emitter.Model;

namespace Counter_Emitter
{
    class Program
    {
        //private static readonly DateTime todayDate = new DateTime(2019, 8, 31);
        private static readonly DateTime todayDate = DateTime.Today;
        private static readonly Dictionary<DictionaryKey, string> settingsConfig = new Dictionary<DictionaryKey, string>();

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly ManualResetEvent workToDo = new ManualResetEvent(false);

        private static FileSystemWatcher watcher;

        async static Task Main(string[] args) {
            InitializeConfiguration();
            Console.CancelKeyPress += (s, evnt) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                workToDo.Set();
                evnt.Cancel = true;
            };

            try
            {
                watcher = new FileSystemWatcher(settingsConfig[DictionaryKey.DBASEDIR])
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size,
                    Filter = "PLOGIN.DBF"
                };

                //watcher.Changed += OnChanged;
                watcher.Changed += (source, e) => { workToDo.Set(); };

                watcher.EnableRaisingEvents = true;
                Console.WriteLine("Listening to file change.. Press CTRL + C to exit..");

                //Console.WriteLine("Listening to file change.. Press any key to exit..");
                //Console.ReadLine();

                while (!cts.IsCancellationRequested)
                {
                    if (workToDo.WaitOne())
                    {
                        workToDo.Reset();
                        Console.WriteLine("File Change Detected.");
                    }
                    else
                    {
                        Console.WriteLine("Timed-out, check if there is any file changed anyway, in case we missed a signal");
                    }

                    try
                    {
                        if (!cts.IsCancellationRequested) await MainAsync(cts.Token);
                        //if (!cts.IsCancellationRequested) await NewImplementation<LoginRecord>("./hello",cts.Token);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine($"Message : {ex.Message}");
                        //Console.WriteLine("Press any key to continue...");
                    }
                    finally
                    {
                        Console.WriteLine("Listening to file change.. Press CTRL + C to exit..");
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine($"Message : {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadLine();
            }
            finally
            {
                watcher?.Dispose();
                workToDo.Close();
                cts.Dispose();
            }
        }

        //No longer used
        //private static async void OnChanged(object sender, FileSystemEventArgs fileEvent)
        //{
        //    if (fileEvent.ChangeType != WatcherChangeTypes.Changed)
        //    {
        //        return;
        //    }
        //    Console.WriteLine($"Changed: {fileEvent.FullPath}");

        //    try
        //    {
        //        await MainAsync(cts.Token);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("\nException Caught!");
        //        Console.WriteLine($"Message : {ex.Message}");
        //        //Console.WriteLine("Press any key to continue...");
        //    }
        //    finally
        //    {
        //        Console.WriteLine("Listening to file change.. Press any key to exit.");
        //    }
        //}

        private static async Task MainAsync(CancellationToken cts)
        {

            // Get the path of PLOGIN dBaseFile
            string fileRoute = settingsConfig[DictionaryKey.PLOGIN];
            IList<LoginRecord> loginRecords;
            try
            {
                Console.WriteLine($"Reading DBF from ..{fileRoute}");
                loginRecords = await GetRecordsFromDbfAsync<LoginRecord>(fileRoute, cts);

                //if (!await IsThereRecordChange(dbf))
                //{
                //    Console.WriteLine("No changes to Database. Ending early..");
                //    Thread.Sleep(1000);
                //    return;
                //}

            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception) 
            {
                throw new FileNotFoundException($"{fileRoute} not found, ensure DBF location properly configured.");
            }

            if(loginRecords.Count > 0)
            {
                Dictionary<string, object> jsonObj = new Dictionary<string, object>
                    {
                        { "data", JsonConvert.SerializeObject(loginRecords) }
                    };

                var serializedJsonObj = JsonConvert.SerializeObject(jsonObj);

                var content = new StringContent(serializedJsonObj, Encoding.UTF8, "application/json");

                //Send the datajsonObj to api
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        Uri url = new UriBuilder(settingsConfig[DictionaryKey.APIURL]).Uri;
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        Console.WriteLine($"Sending Request to ..{url}..");
                        HttpResponseMessage response = await client.PostAsync(url, content, cts);

                        var httpStatus = response.StatusCode;

                        response.EnsureSuccessStatusCode();
                        Console.WriteLine($"Response was {httpStatus}");
                        //Console.WriteLine("Request Complete. Exiting..!");
                        Console.WriteLine("Request Complete.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine($"Message : {ex.Message}");
                        //Console.WriteLine("Press any key to continue...");
                    }
                }
            } 
            else
            {
                Console.WriteLine("No records for today.");
            }

        }
        private static void InitializeConfiguration()
        {
            const string PLOGIN = "PLOGINPATH";
            const string APIURL = "APIURL";
            const string DBASEDIR = "DBASEDIR";

            Console.WriteLine("Initializing configs..");
            settingsConfig.Add(DictionaryKey.PLOGIN, ConfigurationManager.AppSettings.Get(PLOGIN) ?? throw new KeyNotFoundException($"Key {PLOGIN} not found"));
            settingsConfig.Add(DictionaryKey.APIURL, ConfigurationManager.AppSettings.Get(APIURL) ?? throw new KeyNotFoundException($"Key {APIURL} not found"));
            settingsConfig.Add(DictionaryKey.DBASEDIR, ConfigurationManager.AppSettings.Get(DBASEDIR) ?? throw new KeyNotFoundException($"Key {DBASEDIR} not found"));
        }

        //No longer used
        //private async static Task<bool> IsThereRecordChange(Dbf dbfInstance)
        //{
        //    string path = "./reference.txt";
        //    int recordCount = dbfInstance.Records.Count;

        //    // Only create text file if it doesn't exist
        //    if (!File.Exists(path))
        //    {
        //        // Create a file to write to.
        //        using (StreamWriter sw = File.CreateText(path))
        //        {
        //            await sw.WriteAsync(recordCount.ToString());
        //        }
        //    }

        //    // Open the file to read from.
        //    using (StreamReader sr = File.OpenText(path))
        //    {
        //        string count = await sr.ReadLineAsync();
        //        int numRecords = Convert.ToInt32(count);
        //        if (recordCount == 0 || numRecords == recordCount)
        //        {
        //            // Don't need to do anything here,and return early
        //            return false;
        //        }
        //    }

        //    // Continue to overwrite the files if the record counts are not the
        //    using (StreamWriter sw = File.CreateText(path))
        //    {
        //        await sw.WriteAsync(recordCount.ToString());
        //    }
        //    return true;
        //}

        /// <summary>
        /// Transforms a DBaseFile into provided type <typeparamref name="T"/>.<br/>
        /// In order for this to work accurately, the provided type property field arrangements must match the column arrangements from the DBaseFile.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileRoute">Path to The DBF file</param>
        /// <param name="cts"></param>
        /// <returns>An IList of Type <typeparamref name="T"/></returns>
        private async static Task<IList<T>> GetRecordsFromDbfAsync<T>(string fileRoute , CancellationToken cts) where T : IRecord, new()
        {
            var records = new List<T>();
            using (Table table = await Table.OpenAsync(fileRoute, cts))
            {
                try
                {
                    var reader = table.OpenReader(Encoding.UTF8);
                    while (await reader.ReadAsync(cts))
                    {
                        T TRecord = new T();
                        PropertyInfo[] properties = typeof(T).GetProperties();
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            properties[i].SetValue(TRecord, reader.GetValue(table.Columns[i]));
                        }

                        if (TRecord.DATE == todayDate)
                        {
                            records.Add(TRecord);
                        }
                    }
                }
                catch (Exception)
                {
                    throw new InvalidDataException("DBF Table is likely in the wrong format.");
                }
            }
            return records;
        }
    }

    enum DictionaryKey
    {
        PLOGIN,
        APIURL,
        DBASEDIR
    }
}
