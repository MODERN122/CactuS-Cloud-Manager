using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

using Microsoft.Win32;

using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;

namespace Cloud_Manager.Managers
{
    class GoogleDriveManager : CloudDrive
    {
        static readonly string[] Scopes = { DriveService.Scope.Drive };

        public DriveService service;
        private UserCredential credential;
        private readonly string _pathName;

        public static string root = "";

        public ObservableCollection<Google.Apis.Drive.v3.Data.File> FolderItems { get; set; }

        public GoogleDriveManager(string name)
        {
            _pathName = "profile\\" + name + ".token_response";
            credential = GetCredentials();

            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = MainWindow.WindowName,
            });

            SetRoot();
        }


        private UserCredential GetCredentials()
        {
            using (var stream = new FileStream("client_secret_google.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None, 
                    new FileDataStore(_pathName, true)).Result;
            }
            

            return credential;
        }

        private void SetRoot()
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1;
            listRequest.Fields = "nextPageToken, files(id, parents)";
            listRequest.PageToken = null;
            listRequest.Q = "parents in 'root' and trashed=false";
            var request = listRequest.Execute();
            var files = request.Files;

            if (files != null && files.Count > 0)
            {
                root = files[0].Parents[0];
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
                string downloadFileName = saveDialog.FileName;
                var request = service.Files.Get(id);
                var stream = new MemoryStream();

                request.MediaDownloader.ProgressChanged +=
                    (IDownloadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    using (FileStream file = new FileStream(downloadFileName, FileMode.Create, FileAccess.Write))
                                    {
                                        stream.WriteTo(file);
                                    }
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
        }

        public override void UploadFile(FileStructure curDir)
        {
            var openFileDialog = new OpenFileDialog {Filter = "All files (*.*)|*.*", FileName = ""};
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
                    Parents = FolderItems[0].Parents,

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
        }

        public override void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir)
        {
            FilesResource.UpdateRequest updateRequest;
            if (cutFiles.Count > 0)
            {
                foreach (var cutItem in cutFiles)
                {
                    updateRequest = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), cutItem.Id);
                    updateRequest.RemoveParents = cutItem.Parents[0];
                    updateRequest.AddParents = FolderItems[0].Parents[0];
                    updateRequest.Execute();
                }
            }
        }

        public override void CreateFolder(string name, FileStructure parentDir)
        {
            string parent = MainWindow.WindowObject.FolderItems[0].Parents[0];
            var fileMetaData = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name,
                Parents = new List<string>() { parent },
                MimeType = "application/vnd.google-apps.folder",
            };
            service.Files.Create(fileMetaData).Execute();
        }

        public override void RemoveFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                service.Files.Delete(item.Id).Execute();
            }
        }

        public override void TrashFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File {Trashed = true};
                service.Files.Update(file, item.Id).Execute();
            }
        }

        public override void UnTrashFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File {Trashed = false};
                service.Files.Update(file, item.Id).Execute();
            }
        }

        public override void ClearTrash()
        {
            service.Files.EmptyTrash().Execute();
        }

        public override void RenameFile(ICollection<FileStructure> selectedFiles, string newName)
        {
            Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File
            {
                Name = newName
            };
            service.Files.Update(file, selectedFiles.First<FileStructure>().Id).Execute();
        }

        public override ObservableCollection<FileStructure> GetFiles()
        {
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 1000;
            listRequest.Fields = "nextPageToken, files(id, name, fileExtension, size, modifiedByMeTime, parents, trashed, ownedByMe, shared)";
            listRequest.PageToken = null;
            
            
            var request = listRequest.Execute();
            var files = request.Files;

            ObservableCollection<Google.Apis.Drive.v3.Data.File> fileList;
            if (files != null && files.Count > 0)
            {
                fileList = new ObservableCollection<Google.Apis.Drive.v3.Data.File>();
                foreach (var item in files)
                {
                    if (item.Parents != null && item.OwnedByMe == true && item.Shared == false) 
                        fileList.Add(item);
                }
                fileList.Add(new Google.Apis.Drive.v3.Data.File() { Name = "Trash", Parents = new List<string>() { root }, Trashed = false });
            }
            else
            {
                fileList = new ObservableCollection<Google.Apis.Drive.v3.Data.File>();
            }
            return FileStructure.Convert(fileList);
        }
    }
}
