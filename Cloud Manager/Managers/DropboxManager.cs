using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using Dropbox.Api;
using Dropbox.Api.Files;

namespace Cloud_Manager
{
    class DropboxManager : CloudDrive
    {
        static DropboxClient dbx;

        static Dropbox.Api.Files.ListFolderResult folderItems;
                
        public DropboxManager()
        {
            using (var stream = new FileStream("dropbox_secret.txt", FileMode.Open, FileAccess.Read))
            {
                byte[] key = new byte[stream.Length];
                stream.Read(key, 0, key.Length);
                dbx = new DropboxClient(Encoding.Default.GetString(key));
            }
        }

        public Dropbox.Api.Files.ListFolderResult FolderItems
        {
            get { return folderItems; }
            set
            {
                folderItems = value;
            }
        }

        async Task ListRootFolder()
        {
            FolderItems = await dbx.Files.ListFolderAsync(string.Empty);
            if (FolderItems.Entries.Count == 0)
            {
                FolderItems.Entries.Add(new Dropbox.Api.Files.Metadata { });
            }

        }

        async Task ListFolder(string path)
        {
            FolderItems = await dbx.Files.ListFolderAsync(path);
        }

        async Task ListTrashFolder()
        {
            //var items = await dbx.Files.ListFolderAsync(string.Empty, true, true, true);
            //FolderItems = new Dropbox.Api.Files.ListFolderResult();
            //foreach(var item in items.Entries)
            //{
            //    if(item.IsDeleted==true)
            //    {
            //        FolderItems.Entries.Add(item);
            //    }
            //}


            // It shows deleted files, not files in trash
            // There is no access to trash from .NET API
        }

        public override void InitFolder(string path, string parent = "")
        {
            MainWindow.mainWindow.previousPath = MainWindow.mainWindow.CurrentPath;
            MainWindow.mainWindow.CurrentPath = path;
            if (parent == "root" || path == "/Dropbox")
            {
                var task = Task.Run(this.ListRootFolder);
                task.Wait();
            }
            else if (path == "/Dropbox/Trash")
            {
                InitTrash();
            }
            else
            {
                path = path.Substring(path.IndexOf("/") + 1);
                path = path.Substring(path.IndexOf("/"));
                var task = Task.Run(() => this.ListFolder(path));
                task.Wait();
            }
        }

        public override void InitTrash()
        {
            var task = Task.Run(this.ListTrashFolder);
            task.Wait();
        }

        static async Task Download(string name, string id)
        {
            var items = await dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var item in items.Entries)
            {
                if (item.IsFile && item.AsFile.Id == id)
                {
                    using (var response = await dbx.Files.DownloadAsync(item.PathDisplay))
                    {
                        var s = response.GetContentAsByteArrayAsync();
                        s.Wait();
                        var d = s.Result;
                        File.WriteAllBytes(downloadFileName, d);
                    }

                }
            }
        }

        public override void DownloadFile(string name, string id)
        {
            var saveDialog = new SaveFileDialog()
            {
                FileName = name,
                Filter = "All files (*.*)|*.*"
            };
            if (saveDialog.ShowDialog() == true)
            {
                downloadFileName = saveDialog.FileName;
                var task = Task.Run(() => Download(name, id));
                task.Wait();
            }
            else
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
        }

        static async Task Upload(string file, string content)
        {
            using (var mem = new MemoryStream(File.ReadAllBytes(content)))
            {
                string path = MainWindow.mainWindow.CurrentPath;
                if (path != "/Dropbox")
                {
                    path = path.Substring(path.IndexOf("/") + 1);
                    path = path.Substring(path.IndexOf("/") + 1);
                }
                else
                {
                    path = "";
                }

                var updated = await dbx.Files.UploadAsync(
                    path + "/" + file,
                    WriteMode.Overwrite.Instance,
                    body: mem);
            }
        }

        public override void UploadFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.FileName = "";
            if (openFileDialog.ShowDialog() == true)
            {
                string mimeType = "application/unknown";
                string ext = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                    mimeType = regKey.GetValue("Content Type").ToString();
                string shortFileName = openFileDialog.FileName;
                shortFileName = shortFileName.Substring(shortFileName.LastIndexOf('\\', shortFileName.Length - 2) + 1);
                var task = Task.Run(() => Upload(shortFileName, openFileDialog.FileName));
                task.Wait();
            }
            else
            {
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
            }
        }

        public override void PasteFiles(ICollection<FileStructure> cutFiles)
        {
            string path = MainWindow.mainWindow.CurrentPath;
            if (path == "/Dropbox")
                path = "";
            else
            {
                path = path.Substring(path.IndexOf("/") + 1);
                path = path.Substring(path.IndexOf("/"));
            }
            var list = dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var item in cutFiles)
            {
                foreach (var listItem in list.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == item.Id) || (listItem.IsFolder && listItem.AsFolder.Id == item.Id))
                        dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + listItem.Name);
                }
            }
        }

        public override void CreateFolder(string name)
        {
            string path = MainWindow.mainWindow.CurrentPath;
            if (path == "/Dropbox")
            {
                dbx.Files.CreateFolderV2Async("/" + name);
            }
            else
            {
                path = path.Substring(path.IndexOf("/") + 1);
                path = path.Substring(path.IndexOf("/"));
                dbx.Files.CreateFolderV2Async(path + "/" + name);
            }
        }

        public override void RemoveFile(ICollection<FileStructure> selectedFiles)
        {
            var list = dbx.Files.ListFolderAsync(string.Empty, true);
            foreach (var selectedItem in selectedFiles)
            {
                foreach (var listItem in list.Result.Entries)
                {
                    if ((listItem.IsFile && listItem.AsFile.Id == selectedItem.Id) || (listItem.IsFolder && listItem.AsFolder.Id == selectedItem.Id))
                        dbx.Files.DeleteV2Async(listItem.PathDisplay);
                }
            }

        }

        public override void TrashFile(ICollection<FileStructure> selectedFiles)
        {
            RemoveFile(selectedFiles);
        }

        public override void UnTrashFile(ICollection<FileStructure> selectedFiles)
        {
            // There is no access to trash from .NET API
        }

        public override void ClearTrash()
        {
            // There is no access to trash from .NET API
        }

        public override void RenameFile(ICollection<FileStructure> selectedFiles, string newName)
        {
            var list = dbx.Files.ListFolderAsync(string.Empty, true).Result;
            foreach (var listItem in list.Entries)
            {
                if ((listItem.IsFile && listItem.AsFile.Id == selectedFiles.First<FileStructure>().Id)
                    || (listItem.IsFolder && listItem.AsFolder.Id == selectedFiles.First<FileStructure>().Id))
                {
                    var path = listItem.PathDisplay;
                    path = path.Substring(0, path.LastIndexOf("/"));
                    if(listItem.Name.IndexOf('.')>=0)
                        newName += listItem.Name.Substring(listItem.Name.LastIndexOf('.'));
                    dbx.Files.MoveV2Async(listItem.PathDisplay, path + "/" + newName);
                }
            }
            MainWindow.mainWindow.cutItems.Clear();
        }

    }
}
