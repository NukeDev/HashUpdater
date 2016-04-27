using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace HashUpdater
{
    public class Updater
    {
        /// <summary>
        /// Config class, must compile it!
        /// </summary>
        
        public class Config
        {
            /// <summary>
            /// Path Directory
            /// </summary>
            public static string Path { get; set; }
            /// <summary>
            /// File with hashes, save location .json
            /// </summary>
            public static string Md5File { get; set; }
            /// <summary>
            /// Bool var, create or not hashes file
            /// </summary>
            public static bool CreateMd5File { get; set; }
            /// <summary>
            /// http:// - https:// - web link for json file with latest hashes
            /// </summary>
            public static Uri Md5Uri { get; set; }
            /// <summary>
            /// http:// - https:// - web link for latest files
            /// </summary>
            public static Uri WebFolder { get; set; }
        }

        protected string GetMd5HashFromFile(string fileName)
        {

            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
           
        } 

        protected string[] FilesPath(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }        
        } 

        protected Dictionary<string, string> Hashes = new Dictionary<string, string>();
        protected Dictionary<string, string> OnlineHashesList = new Dictionary<string, string>();
        protected List<string> FilesOutOfDate = new List<string>();
        protected string OnlineHashesJson = string.Empty;

        /// <summary>
        /// Create Recursive file hashes of a directory and save them into a json file or a local variable
        /// </summary>

        public string CreateHashes() 
        {
            if (Config.Path.ToString() != string.Empty)
            {    
                try
                {
                    var files = FilesPath(Config.Path.ToString());
                    foreach (var file in files)
                    {
                        var hash = GetMd5HashFromFile(file);
                        Hashes.Add(file.Replace(Config.Path + "\\", ""), hash);
                    }
                    var json = JsonConvert.SerializeObject(Hashes);
                    if (Config.CreateMd5File != true) return json;
                    if(Config.Md5File == string.Empty)
                        MessageBox.Show("Can't write md5 to json file! Please set MD5File var in the Updater.Config", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        File.WriteAllText(Config.Md5File.ToString(), json);

                    return json;
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            else
            {
                MessageBox.Show("Config.Path var not Set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }          
        }

        /// <summary>
        /// Get latest hashes from webserver to json var
        /// </summary>
        
        public string OnlineHashes()
        {
            if (Config.Md5Uri.ToString() != string.Empty)
            {
                try
                {
                    WebClient wb = new WebClient();
                    OnlineHashesJson = wb.DownloadString(Config.Md5Uri);
                    OnlineHashesList = JsonConvert.DeserializeObject<Dictionary<string, string>>(OnlineHashesJson);
                    return OnlineHashesJson;
                }   

                catch (WebException ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
            }
            else
            {
                MessageBox.Show("Config.Md5Uri var not Set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Analyze file differences - Download/Update/Delete Files
        /// </summary>
        
        public async void Analyze()
        {
            foreach (var key in OnlineHashesList.Keys)
            {
                if (Hashes.ContainsKey(key))
                {
                    if (Hashes[key] != OnlineHashesList[key])
                    {
                        FilesOutOfDate.Add(key);
                    }
                }
                else
                {
                    FilesOutOfDate.Add(key);
                }
            }

            try
            {
                var totFiles = FilesOutOfDate.Count;
                var downloaded = 0;
                foreach (var file in FilesOutOfDate)
                {
                    var path = Config.Path + "\\" + file;
                    var dir = path;
                    var index = dir.LastIndexOf("\\", StringComparison.Ordinal);
                    if (index > 0)
                        dir = dir.Substring(0, index); 
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    Uri ur = new Uri(Config.WebFolder + "/" + file);
                    downloaded++;
                    await DownloadAsync(ur, path, downloaded + "/" + totFiles);
                    
                }
                Thread.Sleep(50);

                var totalFiles = FilesPath(Config.Path.ToString());
                var _totalFiles = totalFiles.Select(file => file.Replace(Config.Path + "\\", "")).ToList();
                var onlineFiles = OnlineHashesList.Keys.ToList();
                var filesToDelete = _totalFiles.Where(file => !onlineFiles.Contains(file)).ToList();

                foreach (var file in filesToDelete)
                {
                    File.Delete(Config.Path + "\\" + file);
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(FilesOutOfDate.Count == 0 && filesToDelete.Count == 0 ? "No update needed!" : "Update Completed!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        /// <summary>
        /// Download file async
        /// </summary>

        protected async Task DownloadAsync(Uri url, string path, string filesN)
        {
            try
            {
                WebClient wb = new WebClient();
                wb.DownloadProgressChanged += (s, e) =>
                {
                    Console.Write("\r{0}{1}{2}%", "Downloading... ### ", filesN + " ", e.ProgressPercentage);
                };
                wb.DownloadFileCompleted += (s, e) =>
                {
                    //Console.WriteLine("File: " + path + " Download Completed.");
                };
                await wb.DownloadFileTaskAsync(url, path);
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

    }
}
