using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            public static string MD5File { get; set; }
            /// <summary>
            /// Bool var, create or not hashes file
            /// </summary>
            public static bool CreateMD5File { get; set; }
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
           
        } //Create MD5Hash for file.

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
           
        } //Get all files list of dir.

        protected Dictionary<string, string> Hashes = new Dictionary<string, string>(); // DIR/FILE, MD5

        /// <summary>
        /// Create Recursive file hashes of a directory and save them into a json file or a local variable
        /// </summary>

        public string CreateHashes() //Create Hashes to Json
        {
            if (Config.Path.ToString() != string.Empty)
            {
                
                try
                {
                    var files = FilesPath(Config.Path.ToString());
                    foreach (var file in files)
                    {
                        var hash = GetMd5HashFromFile(file);
                        Hashes.Add(file, hash);
                    }

                    var json = JsonConvert.SerializeObject(Hashes);
                    if (Config.CreateMD5File == true)
                        if(Config.MD5File == string.Empty)
                            MessageBox.Show("Can't write md5 to json file! Please set MD5File var in the Updater.Config", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        else
                            File.WriteAllText(Config.MD5File.ToString(), json);

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
                MessageBox.Show("Path var not Set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            
        }
    }
}
