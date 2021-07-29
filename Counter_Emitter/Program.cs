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
using System.Linq;
using System.Text.Json;

namespace Counter_Emitter
{
    class Program
    {
        //private static readonly DateTime todayDate = new DateTime(2019, 8, 31);
        private static readonly DateTime todayDate = DateTime.Today;
        private static readonly Dictionary<DictionaryKey, string> settingsConfig = new Dictionary<DictionaryKey, string>();

        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly ManualResetEvent workToDo = new ManualResetEvent(false);

        private static List<string> fileToWatch = new List<string> { "PLOGIN.DBF", "USER.DBF" };
        private static FileSystemWatcher watcher;
        private static DictionaryKey? settingKey;

        async static Task Main(string[] args)
        {
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
                    Filter = "*.DBF"
                };

                watcher.Changed += OnChanged;

                watcher.EnableRaisingEvents = true;
                Console.WriteLine("Listening to file change.. Press CTRL + C to exit..");

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

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // get the file's extension
            string fileName = e.Name;

            // filter file types
            if (fileToWatch.Contains(fileName))
            {
                switch (fileName)
                {
                    case "PLOGIN.DBF":
                        settingKey = DictionaryKey.PLOGIN;
                        break;
                    case "USER.DBF":
                        settingKey = DictionaryKey.USER;
                        break;
                    default:
                        settingKey = null;
                        break;
                }
                Console.WriteLine($"A change has been made to a watched file type. {fileName}");
                workToDo.Set();
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
            string fileRoute;
            string url = settingsConfig[DictionaryKey.APIURL];
            IList<IRecord> records;

            try
            {
                switch (settingKey)
                {
                    case DictionaryKey.PLOGIN:
                        url = $"{url}/loginsession";
                        fileRoute = settingsConfig[DictionaryKey.PLOGIN];
                        records = await GetRecordsFromDbfAsync<LoginRecord>(fileRoute, cts);
                        records = records.Where(record => record.DATE == todayDate).ToList();
                        break;
                    case DictionaryKey.USER:
                        url = $"{url}/counteruser";
                        fileRoute = settingsConfig[DictionaryKey.USER];
                        records = await GetRecordsFromDbfAsync<UserRecord>(fileRoute, cts);
                        break;
                    default:
                        settingKey = null;
                        // Means that it is not supported
                        return;
                }

                if (records.Count > 0)
                {

                    var jsonOptions = new JsonSerializerOptions
                    {
                        Converters = { new IRecordConverter() },
                        IncludeFields = true
                    };

                    Dictionary<string, string> jsonObj = new Dictionary<string, string>
                    {
                        { "data", JsonSerializer.Serialize(records, jsonOptions) }
                    };

                    //var serializedJsonObj = JsonSerializer.SerializeToUtf8Bytes(jsonObj);
                    var stringJsonObj = JsonSerializer.Serialize(jsonObj);


                    using (var content = new StringContent(stringJsonObj, Encoding.UTF8, "application/json"))
                    {
                        // Send the datajsonObj to api
                        await SendPostRequestAsync(url, content, cts);
                    }
                    // Reset the setting key
                    settingKey = null;
                }
                else
                {
                    Console.WriteLine("No records for today.");
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }

            records.Clear();
        }

        private static void InitializeConfiguration()
        {
            const string PLOGIN = "PLOGINPATH";
            const string APIURL = "APIURL";
            const string DBASEDIR = "DBASEDIR";
            const string USER = "USER";

            Console.WriteLine("Initializing configs..");
            settingsConfig.Add(DictionaryKey.PLOGIN, ConfigurationManager.AppSettings.Get(PLOGIN) ?? throw new KeyNotFoundException($"Key {PLOGIN} not found"));
            settingsConfig.Add(DictionaryKey.APIURL, ConfigurationManager.AppSettings.Get(APIURL) ?? throw new KeyNotFoundException($"Key {APIURL} not found"));
            settingsConfig.Add(DictionaryKey.DBASEDIR, ConfigurationManager.AppSettings.Get(DBASEDIR) ?? throw new KeyNotFoundException($"Key {DBASEDIR} not found"));
            settingsConfig.Add(DictionaryKey.USER, ConfigurationManager.AppSettings.Get(USER) ?? throw new KeyNotFoundException($"Key {USER} not found"));
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
        private async static Task<IList<IRecord>> GetRecordsFromDbfAsync<T>(string fileRoute, CancellationToken cts) where T : IRecord, new()
        {
            Console.WriteLine($"Reading DBF from ..{fileRoute}");
            var records = new List<IRecord>();
            try
            {
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
                            records.Add(TRecord);
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
                throw new FileNotFoundException($"{fileRoute} not found, ensure DBF location properly configured.");
            }
            return records;
        }

        private async static Task SendPostRequestAsync(string urlParam, HttpContent content, CancellationToken cts)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Uri url = new UriBuilder(urlParam).Uri;
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
    }

    enum DictionaryKey
    {
        PLOGIN,
        USER,
        APIURL,
        DBASEDIR
    }
}

