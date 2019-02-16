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

using Microsoft.Win32;

using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;

namespace Cloud_Manager
{
    class GoogleDriveManager : CloudDrive
    {
        static string[] Scopes = { DriveService.Scope.Drive };

        private ObservableCollection<Google.Apis.Drive.v3.Data.File> folderItems;
        public DriveService service;

        public readonly ICollection<Google.Apis.Drive.v3.Data.File> selectedItems = new Collection<Google.Apis.Drive.v3.Data.File>();
        public readonly ICollection<Google.Apis.Drive.v3.Data.File> cutItems = new Collection<Google.Apis.Drive.v3.Data.File>();

        public GoogleDriveManager()
        {
            UserCredential credential = GetCredentials();

            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = MainWindow.windowName,
            });
        }

        private static UserCredential GetCredentials()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

                credPath = System.IO.Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            return credential;
        }

        public ObservableCollection<Google.Apis.Drive.v3.Data.File> FolderItems
        {
            get { return this.folderItems; }
            set
            {
                this.folderItems = value;
            }
        }

        public override void InitFolder(string path, string fileParent = "")
        {
            MainWindow.mainWindow.selectedItems.Clear();
            selectedItems.Clear();
            if (path == "/Google Drive/Trash")
            {
                InitTrash();
            }
            else
            {
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
                MainWindow.mainWindow.previousPath = MainWindow.mainWindow.CurrentPath;
                MainWindow.mainWindow.CurrentPath = path;
                FilesResource.ListRequest listRequest = service.Files.List();
                listRequest.PageSize = 1000;
                listRequest.Fields = "nextPageToken, files(id, name, fileExtension, size, modifiedByMeTime, parents)";
                listRequest.PageToken = null;
                listRequest.Q = "parents in '" + fileParent + "' and trashed=false";
                var request = listRequest.Execute();
                var files = request.Files;

                if (files != null && files.Count > 0)
                {
                    FolderItems = new ObservableCollection<Google.Apis.Drive.v3.Data.File>(files);
                    if (MainWindow.mainWindow.CurrentPath == "/Google Drive")
                    {
                        FolderItems.Add(new Google.Apis.Drive.v3.Data.File());
                        FolderItems[folderItems.Count - 1].Name = "Trash";
                    }
                }
                else
                {
                    FolderItems = new ObservableCollection<Google.Apis.Drive.v3.Data.File>();
                }
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
            }

        }

        public override void InitTrash()
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Q = "trashed=true";
            listRequest.PageSize = 1000;
            listRequest.Fields = "nextPageToken, files(id, name, fileExtension, size, modifiedByMeTime)";

            var request = listRequest.Execute();
            var files = request.Files;

            if (files != null && files.Count > 0)
            {
                FolderItems = new ObservableCollection<Google.Apis.Drive.v3.Data.File>(files);
            }
            else
            {
                var file = new Google.Apis.Drive.v3.Data.File();
                FolderItems = new ObservableCollection<Google.Apis.Drive.v3.Data.File>();
                FolderItems.Add(file);
            }
            MainWindow.mainWindow.previousPath = MainWindow.mainWindow.CurrentPath;
            MainWindow.mainWindow.CurrentPath = "/Google Drive/Trash";
            MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
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
                var request = service.Files.Get(id);
                var stream = new MemoryStream();

                request.MediaDownloader.ProgressChanged +=
                    (IDownloadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Visible);
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    using (FileStream file = new FileStream(downloadFileName, FileMode.Create, FileAccess.Write))
                                    {
                                        stream.WriteTo(file);
                                    }
                                    MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    MessageBox.Show("Ошибка при загрузке файла", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    break;
                                }
                        }
                    };
                request.DownloadAsync(stream);
            }
            else
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
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
                var file = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = shortFileName,
                    Parents = folderItems[0].Parents,

                };

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(openFileDialog.FileName,
                        FileMode.Open))
                {
                    request = service.Files.Create(
                        file, stream, mimeType);
                    request.Fields = "id";
                    request.Upload();
                }

            }
            else
            {
                MainWindow.mainWindow.ChangeVisibilityOfProgressBar(Visibility.Collapsed);
            }
        }

        public override void CutFiles()
        {
            foreach (var selectedItem in selectedItems)
            {
                cutItems.Add(selectedItem);
                MainWindow.mainWindow.cutItems.Add(selectedItem);
            }
        }

        public override void PasteFiles()
        {
            FilesResource.UpdateRequest updateRequest;
            if (cutItems.Count > 0)
            {
                foreach (var cutItem in cutItems)
                {
                    updateRequest = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), cutItem.Id);
                    updateRequest.RemoveParents = cutItem.Parents[0];
                    updateRequest.AddParents = FolderItems[0].Parents[0];
                    updateRequest.Execute();
                }
                cutItems.Clear();
                MainWindow.mainWindow.cutItems.Clear();
            }
        }

        public override void CreateFolder(string name)
        {
            string parent = MainWindow.mainWindow.FolderItems[0].Parents[0];
            var fileMetaData = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name,
                Parents = new List<string>() { parent },
                MimeType = "application/vnd.google-apps.folder",
            };
            service.Files.Create(fileMetaData).Execute();
        }

        public override void RemoveFile()
        {
            foreach (var item in selectedItems)
            {
                service.Files.Delete(item.Id).Execute();
            }
        }

        public override void TrashFile()
        {
            foreach (var item in selectedItems)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File();
                file.Trashed = true;
                service.Files.Update(file, item.Id).Execute();
            }
        }

        public override void UnTrashFile()
        {
            foreach (var item in selectedItems)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File();
                file.Trashed = false;
                service.Files.Update(file, item.Id).Execute();
            }
        }

        public override void ClearTrash()
        {
            service.Files.EmptyTrash().Execute();
        }

        public override void RenameFile()
        {
            Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File();
            file.Name = MainWindow.mainWindow.txtRenamedFile.Text;
            service.Files.Update(file, selectedItems.First<Google.Apis.Drive.v3.Data.File>().Id).Execute();
        }
    }
}
