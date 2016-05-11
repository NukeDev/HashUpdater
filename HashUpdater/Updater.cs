using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            /// <summary>
            /// Path of lzma.exe compressor
            /// </summary>
            public static string LzmaCompressor { get; set; }
            /// <summary>
            /// Path compressed files
            /// </summary>
            public static string CompressedDir { get; set; }
            /// <summary>
            /// Bool, compress or not files
            /// </summary>
            public static bool Compress { get; set; }
        }

        protected string GetMd5HashFromFile(string fileName)
        {

            try
            {
                using (FileStream stream = File.OpenRead(fileName))
                {
                    SHA256Managed sha = new SHA256Managed();
                    byte[] checksum = sha.ComputeHash(stream);
                    return BitConverter.ToString(checksum).Replace("-", String.Empty);
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

        public async Task<string> CreateHashes() 
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
                    if (Config.CreateMd5File == true)
                    {
                        if (Config.Md5File == string.Empty)
                            MessageBox.Show(
                                "Can't write md5 to json file! Please set MD5File var in the Updater.Config", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else
                            using (StreamWriter sw = new StreamWriter(Config.Md5File))
                                sw.Write(json);
                        if (Config.Compress != true) return json;
                        if (!Directory.Exists(Config.CompressedDir)) return json;
                        if (!File.Exists(Config.LzmaCompressor)) return json;
                        foreach (var file in files)
                        {
                            await CompressFile(file);
                        }
                        return json;
                    }
                    if (Config.Compress != true) return json;
                    if (!Directory.Exists(Config.CompressedDir)) return json;
                    if (!File.Exists(Config.LzmaCompressor)) return json;
                    foreach (var file in files)
                    {
                        await CompressFile(file);
                    }
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
                    Uri ur = new Uri(Config.WebFolder + "/" + file + ".lzma");
                    downloaded++;
                    await DownloadAsync(ur, path, downloaded + "/" + totFiles);
                    
                }
                Thread.Sleep(50);

                var totalFiles = FilesPath(Config.Path.ToString());
                var _totalFiles = totalFiles.Select(file => file.Replace(Config.Path + "\\", "")).ToList();
                var onlineFiles = OnlineHashesList.Keys.ToList();
                var filesToDelete = _totalFiles.Where(file => !onlineFiles.Contains(file)).ToList();
                var totaFiles = 0;
                foreach (var file in filesToDelete)
                {
                    totaFiles++;
                    Console.WriteLine("Deleting: " + file + " -- " + totaFiles + "/" + filesToDelete.Count);
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
                    Console.WriteLine("{0}{1}{2}%", "Downloading: " + filesN + " -- ", (e.BytesReceived / 1024d / 1024d).ToString("0.00") +"Mb/"+ (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00") + "Mb -- ", e.ProgressPercentage );
                };
                wb.DownloadFileCompleted += (s, e) =>
                {
                    DecompressFile(path + ".lzma");
                };
                await wb.DownloadFileTaskAsync(url, path + ".lzma");
            }
            catch (WebException ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Compress Files Async
        /// </summary>

        protected async Task CompressFile(string file)
        {
            if (file.Contains(".lzma")) return;
            Console.WriteLine("Compressing: " + file.Replace(Config.Path, ""));
            var path = Config.CompressedDir + "\\" + file.Replace(Config.Path, "");
            var dir = path;
            var index = dir.LastIndexOf("\\", StringComparison.Ordinal);
            if (index > 0)
                dir = dir.Substring(0, index);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await Compression(file);
        }
        
        /// <summary>
        /// Decompress Files Async
        /// </summary>
        
        protected async void DecompressFile(string file)
        {
            Console.WriteLine("Extracting: " + file.Replace(Config.Path, ""));
            await Decompression(file);
        }

        #pragma warning disable CS1998
        /// <summary>               
        /// Compression Framework                
        /// </summary>

        protected virtual async Task Compression(string file)
        {
            using (Process process = Process.Start(new ProcessStartInfo()
            {
                FileName = Config.LzmaCompressor,
                Arguments = "e " + AddCommasIfRequired(file) + " " + AddCommasIfRequired(Config.CompressedDir + "\\" + file.Replace(Config.Path, "")) + ".lzma -d21",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }))

            {
                while (process != null && !process.HasExited)
                    Thread.Sleep(300);
            }
        }

        /// <summary>               
        /// Decompression Framework                
        /// </summary>

        protected virtual async Task Decompression(string file)
        {
            using (Process process = Process.Start(new ProcessStartInfo()
            {
                FileName = Config.LzmaCompressor,
                Arguments = "d " + AddCommasIfRequired(file) + " " + AddCommasIfRequired(file.Replace(".lzma", "")) + " -d21",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }))

            {
                while (process != null && !process.HasExited)
                    Thread.Sleep(300);
            }
        }
        
        /// <summary>               
        /// Comma Path Checking              
        /// </summary>
 
        public string AddCommasIfRequired(string path)
        {
            return (path.Contains(" ")) ? "\"" + path + "\"" : path;
        }

    }

}


