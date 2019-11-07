using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.Globalization;
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

        public DriveService Service;
        private UserCredential _credential;
        private readonly string _pathName;

        public string Root = "";

        public ObservableCollection<Google.Apis.Drive.v3.Data.File> FolderItems { get; set; }

        public GoogleDriveManager(string name)
        {
            _pathName = "profile\\" + name + ".token_response";
            _credential = GetCredentials();

            Service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = MainWindow.WindowName,
            });

            SetRoot();
        }


        private UserCredential GetCredentials()
        {
            using (var stream = new FileStream("client_secret_google.json", FileMode.Open, FileAccess.Read))
            {
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None, 
                    new FileDataStore(_pathName, true)).Result;
            }
            

            return _credential;
        }

        private void SetRoot()
        {
            FilesResource.ListRequest listRequest = Service.Files.List();
            listRequest.PageSize = 1;
            listRequest.Fields = "nextPageToken, files(id, parents)";
            listRequest.PageToken = null;
            listRequest.Q = "parents in 'root' and trashed=false";
            var request = listRequest.Execute();
            var files = request.Files;

            if (files != null && files.Count > 0)
            {
                Root = files[0].Parents[0];
            }
        }

        /// <summary>
        /// Downloads a file.
        /// </summary>
        /// <param name="name">The name of the file that file have on the computer</param>
        /// <param name="id">ID of the file</param>
        public override void DownloadFile(string name, string id)
        {
            var saveDialog = new SaveFileDialog
            {
                FileName = name,
                Filter = "All files (*.*)|*.*"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string downloadFileName = saveDialog.FileName;
                var request = Service.Files.Get(id);
                var stream = new MemoryStream();

                request.MediaDownloader.ProgressChanged +=
                    progress =>
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
                            default:
                                MessageBox.Show("Unexpected program behaviour");
                                break;
                        }
                    };
                request.DownloadAsync(stream);
            }
        }

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="curDir">The current cloud directory where the file will be uploaded</param>
        public override void UploadFile(FileStructure curDir)
        {
            var openFileDialog = new OpenFileDialog {Filter = "All files (*.*)|*.*", FileName = ""};
            if (openFileDialog.ShowDialog() == true)
            {
                string mimeType = "application/unknown";
                string ext = Path.GetExtension(openFileDialog.FileName).ToLower(CultureInfo.InvariantCulture);
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                {
                    mimeType = regKey.GetValue("Content Type").ToString();
                }
                string shortFileName = openFileDialog.FileName;
                shortFileName = shortFileName.Substring(shortFileName.LastIndexOf('\\', shortFileName.Length - 2) + 1);
                var file = new Google.Apis.Drive.v3.Data.File
                {
                    Name = shortFileName,
                    Parents = FolderItems[0].Parents,

                };

                FilesResource.CreateMediaUpload request;
                using (var stream = new FileStream(openFileDialog.FileName,
                        FileMode.Open))
                {
                    request = Service.Files.Create(
                        file, stream, mimeType);
                    request.Fields = "id";
                    request.Upload();
                }

            }
        }

        /// <summary>
        /// Pastes the cutted files.
        /// </summary>
        /// <param name="cutFiles">Collection of cutted files</param>
        /// <param name="curDir">The current cloud directory where the cutted files will be pasted</param>
        public override void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir)
        {
            FilesResource.UpdateRequest updateRequest;
            if (cutFiles.Count > 0)
            {
                foreach (var cutItem in cutFiles)
                {
                    updateRequest = Service.Files.Update(new Google.Apis.Drive.v3.Data.File(), cutItem.Id);
                    updateRequest.RemoveParents = cutItem.Parents[0];
                    updateRequest.AddParents = FolderItems[0].Parents[0];
                    updateRequest.Execute();
                }
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="name">The name of a new folder</param>
        /// <param name="parentDir">Cloud parent directory</param>
        public override void CreateFolder(string name, FileStructure parentDir)
        {
            string parent = MainWindow.WindowObject.FolderItems[0].Parents[0];
            var fileMetaData = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                Parents = new List<string> { parent },
                MimeType = "application/vnd.google-apps.folder",
            };
            Service.Files.Create(fileMetaData).Execute();
        }

        /// <summary>
        /// Deletes files.
        /// </summary>
        /// <param name="selectedFiles">Collection of selected files</param>
        public override void RemoveFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                Service.Files.Delete(item.Id).Execute();
            }
        }

        /// <summary>
        /// Moves files into trash directory.
        /// </summary>
        /// <param name="selectedFiles">Collection of selected files</param>
        public override void TrashFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File {Trashed = true};
                Service.Files.Update(file, item.Id).Execute();
            }
        }

        /// <summary>
        /// Moves the files from trash directory into the previous directory.
        /// </summary>
        /// <param name="selectedFiles">Collection of selected files</param>
        public override void UnTrashFile(ICollection<FileStructure> selectedFiles)
        {
            foreach (var item in selectedFiles)
            {
                Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File {Trashed = false};
                Service.Files.Update(file, item.Id).Execute();
            }
        }

        /// <summary>
        /// Clears the trash.
        /// </summary>
        public override void ClearTrash()
        {
            Service.Files.EmptyTrash().Execute();
        }

        public override void RenameFile(ICollection<FileStructure> selectedFiles, string newName)
        {
            Google.Apis.Drive.v3.Data.File file = new Google.Apis.Drive.v3.Data.File
            {
                Name = newName
            };
            Service.Files.Update(file, selectedFiles.First().Id).Execute();
        }

        /// <summary>
        /// Gets a collection of the files.
        /// </summary>
        /// <returns></returns>
        public override ObservableCollection<FileStructure> GetFiles()
        {
            FilesResource.ListRequest listRequest = Service.Files.List();
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
                    {
                        fileList.Add(item);
                    }
                }
                fileList.Add(new Google.Apis.Drive.v3.Data.File { Name = "Trash", Parents = new List<string> { Root }, Trashed = false });
            }
            else
            {
                fileList = new ObservableCollection<Google.Apis.Drive.v3.Data.File>();
            }
            return FileStructure.Convert(fileList, Root);
        }
    }
}
