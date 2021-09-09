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
using System.Net;
using System.Text.Json;
using System.Security.Cryptography;

namespace Counter_Emitter
{
    class Program
    {
        //private static readonly DateTime todayDate = new DateTime(2019, 8, 31);
        private static readonly Dictionary<DictionaryKey, string> SettingsConfig = new Dictionary<DictionaryKey, string>();

        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static readonly ManualResetEvent WorkToDo = new ManualResetEvent(false);

        private static List<string> FileToWatch;
        private static FileSystemWatcher _watcherUsers;
        private static FileSystemWatcher _watcherLogin;
        private static FileSystemWatcher _watcherDailyCounter;

        private static Strategy _strategy;

        static async Task Main(string[] args)
        {
            InitializeConfiguration();
            Console.CancelKeyPress += (s, evnt) =>
            {
                Console.WriteLine("Canceling...");
                Cts.Cancel();
                WorkToDo.Set();
                evnt.Cancel = true;
            };

            try
            {
                _watcherUsers = new FileSystemWatcher(SettingsConfig[DictionaryKey.Dbasedir])
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.DBF"
                };
                _watcherDailyCounter = new FileSystemWatcher(SettingsConfig[DictionaryKey.Logindir])
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.pos"
                };
                _watcherLogin = new FileSystemWatcher(SettingsConfig[DictionaryKey.Logindir])
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = "*.DBF"
                };

                _watcherUsers.Changed += OnChanged;
                _watcherDailyCounter.Changed += OnChanged;
                _watcherLogin.Changed += OnChanged;

                _watcherUsers.EnableRaisingEvents = true;
                _watcherDailyCounter.EnableRaisingEvents = true;
                _watcherLogin.EnableRaisingEvents = true;
                Console.WriteLine("Listening to file change.. Press CTRL + C to exit..");

                while (!Cts.IsCancellationRequested)
                {
                    if (WorkToDo.WaitOne())
                    {
                        WorkToDo.Reset();
                        Console.WriteLine("File Change Detected.");
                    }
                    else
                    {
                        Console.WriteLine("Timed-out, check if there is any file changed anyway, in case we missed a signal");
                    }

                    try
                    {
                        if (!Cts.IsCancellationRequested) await MainAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine($"Message : {ex.Message}");
                        LogWriterCounter.LogWrite($"{ex.Message} : {ex.StackTrace}");
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
                _watcherUsers?.Dispose();
                _watcherDailyCounter?.Dispose();
                _watcherLogin?.Dispose();
                WorkToDo.Close();
                Cts.Dispose();
            }
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // get the file's extension
            string fileName = e.Name;
            string baseUrl = SettingsConfig[DictionaryKey.Apiurl];

            // filter file types
            if (FileToWatch.Contains(fileName))
            {
                if (fileName == FileToWatch[0])
                {
                    _strategy = new LoginStrategy(baseUrl, SettingsConfig[DictionaryKey.Plogin], Cts.Token);
                }
                else if (fileName == FileToWatch[1])
                {
                    _strategy = new UserStrategy(baseUrl, SettingsConfig[DictionaryKey.User], Cts.Token);
                }
                else if (fileName == FileToWatch[2])
                {
                    _strategy = new SaleStrategy(baseUrl, $"{SettingsConfig[DictionaryKey.Logindir]}/{FileToWatch[2]}", Cts.Token);
                }
                else
                {
                    _strategy = null;
                    return;
                }
                Console.WriteLine($"A change has been made to a watched file type. {fileName}");
                WorkToDo.Set();
            }
        }

        private static async Task MainAsync()
        {

            try
            {
                await _strategy.Execute();

                if (_strategy.RECORDS.Count > 0)
                {

                    var jsonOptions = new JsonSerializerOptions
                    {
                        Converters = { new RecordConverter() },
                        IncludeFields = true
                    };

                    Dictionary<string, string> jsonObj = new Dictionary<string, string>
                    {
                        { "data", JsonSerializer.Serialize(_strategy.RECORDS, jsonOptions) },
                        { "branch", SettingsConfig[DictionaryKey.Branchid] }
                    };

                    //var serializedJsonObj = JsonSerializer.SerializeToUtf8Bytes(jsonObj);
                    var stringJsonObj = JsonSerializer.Serialize(jsonObj);

                    using (var content = new StringContent(stringJsonObj, Encoding.UTF8, "application/json"))
                    {
                        // Send the datajsonObj to api
                        await SendPostRequestAsync(_strategy.URL, content, _strategy.Cts);
                    }
                    // Reset the setting key
                    _strategy.Clear();
                    _strategy = null;
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
        }

        private static void InitializeConfiguration()
        {
            const string plogin = "PLOGINPATH";
            const string apiurl = "APIURL";
            const string dbasedir = "DBASEDIR";
            const string user = "USER";
            const string branchid = "BRANCHID";
            const string logindir = "LOGINDIR";

            Console.WriteLine("Initializing configs..");
            SettingsConfig.Add(DictionaryKey.Plogin, ConfigurationManager.AppSettings.Get(plogin) ?? throw new KeyNotFoundException($"Key {plogin} not found"));
            SettingsConfig.Add(DictionaryKey.Apiurl, ConfigurationManager.AppSettings.Get(apiurl) ?? throw new KeyNotFoundException($"Key {apiurl} not found"));
            SettingsConfig.Add(DictionaryKey.Dbasedir, ConfigurationManager.AppSettings.Get(dbasedir) ?? throw new KeyNotFoundException($"Key {dbasedir} not found"));
            SettingsConfig.Add(DictionaryKey.Logindir, ConfigurationManager.AppSettings.Get(logindir) ?? throw new KeyNotFoundException($"Key {logindir} not found"));
            SettingsConfig.Add(DictionaryKey.User, ConfigurationManager.AppSettings.Get(user) ?? throw new KeyNotFoundException($"Key {user} not found"));
            SettingsConfig.Add(DictionaryKey.Branchid, ConfigurationManager.AppSettings.Get(branchid) ?? throw new KeyNotFoundException($"Key {branchid} not found"));

            FileToWatch = new List<string> {
                "PLOGIN.DBF",
                "USER.DBF",
                $"{SettingsConfig[DictionaryKey.Branchid]}{DateTime.Now:ddMMyy}.pos"
            };
        }

        /// <summary>
        /// Transforms a DBaseFile into provided type <typeparamref name="T"/>.<br/>
        /// In order for this to work accurately, the provided type property field arrangements must match the column arrangements from the DBaseFile.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileRoute">Path to The DBF file</param>
        /// <param name="cts"></param>
        /// <returns>An IList of Type <typeparamref name="T"/></returns>
        public static async Task<IList<IRecord>> GetRecordsFromDbfAsync<T>(string fileRoute, CancellationToken cts, DateTime? dateTimeFilter) where T : IRecord, new()
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
                            T record = new T();
                            PropertyInfo[] properties = typeof(T).GetProperties();
                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                properties[i].SetValue(record, reader.GetValue(table.Columns[i]));
                            }

                            if (dateTimeFilter is null)
                            {
                                records.Add(record);
                                continue;
                            }

                            if (record.DATE == dateTimeFilter)
                            {
                                records.Add(record);
                            }
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

        private static async Task SendPostRequestAsync(string urlParam, HttpContent content, CancellationToken cts)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    string apikey =
                        GenerateApiKey(
                            $"fungming-{SettingsConfig[DictionaryKey.Branchid].ToUpper()}-{DateTime.UtcNow:yyyyMMdd}");
                    Uri url = new UriBuilder(urlParam).Uri;
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (apikey != null)
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey);

                    Console.WriteLine($"Sending Request to ..{url}..");
                    HttpResponseMessage response = await client.PostAsync(url, content, cts);

                    var httpStatus = response.StatusCode;

                    response.EnsureSuccessStatusCode();
                    Console.WriteLine($"Response was {httpStatus}");
                    //Console.WriteLine("Request Complete. Exiting..!");
                    Console.WriteLine("Request Complete.");

                    LogWriterCounter.LogWrite($"Response to {url} was {httpStatus}");
                    response.Dispose();
                }
                catch
                {
                    throw new Exception($"Error occured when sending to {urlParam}");
                }
            }
        }

        private static string GenerateApiKey(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }

    enum DictionaryKey
    {
        Plogin,
        User,
        Apiurl,
        Logindir,
        Dbasedir,
        Branchid
    }
}

