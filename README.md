# HashUpdater
HashUpdater is used to update all types of files using MD5Checksum and a WebServer.
First create the JSON file with the main hashes, than upload them to a webserver and file's hashes too.
Setup the Updater:
            
            var up = new Updater();
            Updater.Config.Path = @"D:\Dir\Filestoupdate\"; 
            Updater.Config.Md5File = @"D:\Dir\Filestoupdate\md5.json"; 
            Updater.Config.CreateMd5File = true; //First time create the JSON File
            Updater.Config.Md5Uri = new Uri("https://example.com/md5.json"); // URL Latest md5.json
            Updater.Config.WebFolder = new Uri("https://example.com/Filestoupdate/"); // dir latest files
			Updater.Config.LzmaCompressor = @"D:\Dir\lzma.exe";
            Updater.Config.CompressedDir = @"D:\Dir\Compressedfilestoupdate";
            Updater.Config.Compress = true;
            up.CreateHashes(); //Make local files checksum
            up.OnlineHashes(); //Download latest checksum
            up.Analyze(); //analyze checksums differences, Download/Update/delete files
            Console.Read();
            