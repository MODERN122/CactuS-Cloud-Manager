using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Cloud_Manager.Managers;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using Cloud_Manager.Data;
using Dropbox.Api.Files;

namespace Cloud_Manager
{
    class CloudManagerLogic
    {
        #region Variables

        private ApplicationContext _db;

        private readonly List<CloudInfo> _cloudList;
        private CloudInfo _currentCloudInfo;

        private string _currentPath;
        #endregion

        #region Properties
        public readonly ICollection<FileStructure> SelectedItems = new Collection<FileStructure>();
        public readonly ICollection<FileStructure> CutItems = new Collection<FileStructure>();

        public string CurrentPath
        {
            get => _currentPath ?? "/";
            set
            {
                if (value != null)
                {
                    _currentPath = value;
                }
            }
        }

        public string PreviousPath { get; set; }

        public List<CloudInfo> CloudList
        {
            get => _cloudList;

        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <c>CloudManagerLogic</c> class
        /// </summary>
        public CloudManagerLogic()
        {
            _db = new ApplicationContext();
            _cloudList = new List<CloudInfo>();
            GetInfo();
            CurrentPath = "/";
            PreviousPath = "/";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes a new instance of specified <paramref name="type"/> and adds it into the list. 
        /// </summary>
        /// <param name="name">Displayed name of the cloud.</param>
        /// <param name="type">The cloud instance of what <paramref name="type"/> need to be created.</param>
        public void AddCloud(string name, CloudManagerType type)
        {
            switch (type)
            {
                case CloudManagerType.GoogleDrive:
                    _cloudList.Add(new CloudInfo(name, new GoogleDriveManager(name)));
                    break;

                case CloudManagerType.Dropbox:
                    _cloudList.Add(new CloudInfo(name, new DropboxManager(name)));
                    break;

                default:
                    throw new NotImplementedException();
            }

        }

        /// <summary>
        /// Creates a new cloud with specified name and type and adds it into CloudList
        /// </summary>
        /// <param name="name">A name of cloud</param>
        /// <param name="fullType">A full type of cloud which include namespaces</param>
        private void AddCloud(string name, string fullType)
        {
            switch (fullType)
            {
                case "Cloud_Manager.Managers.GoogleDriveManager":
                    _cloudList.Add(new CloudInfo(name, new GoogleDriveManager(name)));
                    break;

                case "Cloud_Manager.Managers.DropboxManager":
                    _cloudList.Add(new CloudInfo(name, new DropboxManager(name)));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Initializes an instance of the cloud via string <paramref name="nameAndType"/>.
        /// </summary>
        /// <param name="nameAndType">Contains name and type of the cloud</param>
        private void AddCloud(string nameAndType)
        {
            string[] splited = nameAndType.Split(':');
            switch (splited[1])
            {
                case "Cloud_Manager.Managers.GoogleDriveManager":
                    _cloudList.Add(new CloudInfo(splited[0], new GoogleDriveManager(splited[0])));
                    break;

                case "Cloud_Manager.Managers.DropboxManager":
                    _cloudList.Add(new CloudInfo(splited[0], new DropboxManager(splited[0])));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Renames cloud.
        /// </summary>
        /// <param name="name">New name of cloud.</param>
        /// <param name="cloud">An object which name should be renamed.</param>
        public void RenameCloud(string name, CloudInfo cloud)
        {
            cloud.Name = name;
        }

        /// <summary>
        /// Removes cloud.
        /// </summary>
        /// <param name="name">The name of the removable cloud.</param>
        public void RemoveCloud(string name)
        {
            foreach (var cloud in _cloudList)
            {
                if (cloud.Name == name)
                {
                    _cloudList.Remove(cloud);
                }
            }
        }

        /// <summary>
        /// Initializes and return a start folder that contains cloud names that has saved.
        /// </summary>
        /// <returns>A list that contains cloud names.</returns>
        public ObservableCollection<FileStructure> InitStartFolder()
        {
            var files = new ObservableCollection<FileStructure>();
            foreach (var cloud in _cloudList)
            {
                files.Add(new FileStructure { Name = cloud.Name });
            }
            SelectedItems.Clear();
            CutItems.Clear();

            return files;
        }

        /// <summary>
        /// Refreshes and return files info of the current cloud.
        /// </summary>
        /// <returns>A list of refreshed files info.</returns>
        public ObservableCollection<FileStructure> RefreshInfo()
        {
            if (CurrentPath == "/")
            {
                return InitStartFolder();
            }
            else
            {
                _currentCloudInfo.Files = _currentCloudInfo.Cloud.GetFiles();
                return _currentCloudInfo.GetFilesInCurrentDir();
            }

        }

        /// <summary>
        /// Returns a list of the files that are siblings of the parent directory.
        /// </summary>
        /// <returns>A list of sibling of the parent directory.</returns>
        public ObservableCollection<FileStructure> GetParentDirectory()
        {
            // The parent directory of the root is root
            if (CurrentPath == "/")
            {
                return InitStartFolder();
            }
            // The parent directory of the cloud root is program root
            else if (CurrentPath.IndexOf('/') == CurrentPath.LastIndexOf('/'))
            {

                foreach (var item in _cloudList)
                {
                    if (CurrentPath != '/' + item.Name)
                    {
                        continue;
                    }

                    PreviousPath = CurrentPath;
                    CurrentPath = "/";

                    return InitStartFolder();
                }
            }
            else
            {
                PreviousPath = CurrentPath;
                CurrentPath = CurrentPath.Substring(0,
                    CurrentPath.Length - _currentCloudInfo.CurrentDir.Name.Length - 1);
                // If parent directory is cloud root
                if (CurrentPath == '/' + _currentCloudInfo.Name)
                {
                    _currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
                    return _currentCloudInfo.GetFilesInCurrentDir();
                }
                else
                {
                    foreach (var item in _currentCloudInfo.Files)
                    {
                        if (item.Id != _currentCloudInfo.CurrentDir.Parents[0]) { continue; }

                        _currentCloudInfo.CurrentDir = item;
                        return _currentCloudInfo.GetFilesInCurrentDir();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of the clouds.
        /// </summary>
        /// <returns>A list of the clouds</returns>
        public ObservableCollection<FileStructure> GetHomeDirectory()
        {
            PreviousPath = CurrentPath;
            CurrentPath = "/";

            return InitStartFolder();
        }

        /// <summary>
        /// Returns a list of files in the previous folder.
        /// </summary>
        /// <returns>A list of files in the previous folder</returns>
        public ObservableCollection<FileStructure> GetPreviousDirectory()
        {
            // If previous path is the selection between clouds
            if (PreviousPath == "/")
            {
                string tmp = PreviousPath;
                PreviousPath = CurrentPath;
                CurrentPath = tmp;
                return InitStartFolder();
            }
            // If previous path is the any cloud's root
            else if (PreviousPath == '/' + _currentCloudInfo.Name)
            {
                _currentCloudInfo.CurrentDir = new FileStructure { Name = "Root" };
                string tmp = PreviousPath;
                PreviousPath = CurrentPath;
                CurrentPath = tmp;
                return _currentCloudInfo.GetFilesInCurrentDir();
            }
            // If previous path is the any cloud's trashed files
            else if (PreviousPath == '/' + _currentCloudInfo.Name + "/Trash")
            {
                _currentCloudInfo.CurrentDir = new FileStructure { Name = "Trash" };
                string tmp = PreviousPath;
                PreviousPath = CurrentPath;
                CurrentPath = tmp;
                return _currentCloudInfo.GetFilesInCurrentDir();
            }
            else
            {
                string path = PreviousPath;
                path = path.Substring(1); // delete first slash in the path
                path = path.Substring(path.IndexOf('/')); // delete the name of the current cloud
                foreach (var item in _currentCloudInfo.Files)
                {
                    if (item.Path == path)
                    {
                        _currentCloudInfo.CurrentDir = item;
                        string tmp = PreviousPath;
                        PreviousPath = CurrentPath;
                        CurrentPath = tmp;
                        return _currentCloudInfo.GetFilesInCurrentDir();
                    }
                }
            }


            return null;
        }

        /// <summary>
        /// Downloads the first file of list that contains selected files.
        /// </summary>
        public void DownloadFile()
        {
            var selectedItem = SelectedItems.First();
            if (selectedItem != null)
            {
                _currentCloudInfo.Cloud.DownloadFile(selectedItem.Name, selectedItem.Id);
            }
        }

        /// <summary>
        /// Uploads a file into the current directory.
        /// </summary>
        public void UploadFile()
        {
            _currentCloudInfo.Cloud.UploadFile(_currentCloudInfo.CurrentDir);
        }

        /// <summary>
        /// Adds selected files into the list of cuted files.
        /// </summary>
        public void CutFiles()
        {
            CutItems.Clear();
            foreach (var item in SelectedItems)
            {
                CutItems.Add(item);
            }
            SelectedItems.Clear();
        }

        /// <summary>
        /// Pastes files from the list of cuted files into the current directory.
        /// </summary>
        public void PasteFiles()
        {
            _currentCloudInfo.Cloud.PasteFiles(CutItems, _currentCloudInfo.CurrentDir);
            CutItems.Clear();
        }

        /// <summary>
        /// Creates a new folder with selected <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of a new folder</param>
        public void CreateFolder(string name)
        {
            if (name != "")
            {
                _currentCloudInfo.Cloud.CreateFolder(name, _currentCloudInfo.CurrentDir);
            }
        }

        /// <summary>
        /// Deletes selected files (files that are in the selected files array).
        /// </summary>
        public void RemoveFiles()
        {
            _currentCloudInfo.Cloud.RemoveFile(SelectedItems);
            SelectedItems.Clear();
        }

        /// <summary>
        /// Moves selected files into trash folder (files that are in the selected files array).
        /// </summary>
        public void TrashFiles()
        {
            _currentCloudInfo.Cloud.TrashFile(SelectedItems);
            SelectedItems.Clear();
        }

        /// <summary>
        /// Moves selected files from trash folder into root (files that are in the selected files array). 
        /// </summary>
        public void UnTrashFiles()
        {
            _currentCloudInfo.Cloud.UnTrashFile(SelectedItems);
            SelectedItems.Clear();
        }

        /// <summary>
        /// Moves all files from the trash folder into root folder.
        /// </summary>
        public void ClearTrash()
        {
            _currentCloudInfo.Cloud.ClearTrash();
        }

        /// <summary>
        /// Renames selected file.
        /// </summary>
        /// <param name="name">The new name of file</param>
        public void RenameFile(string name)
        {
            if (name != "")
            {
                _currentCloudInfo.Cloud.RenameFile(SelectedItems, name);
            }

            SelectedItems.Clear();
        }

        /// <summary>
        /// Opens file. If file is a folder then returns a list of inner files.
        /// </summary>
        /// <param name="item">The file that was interacted.</param>
        /// <returns> A list of inner files.</returns>
        public ObservableCollection<FileStructure> EnterFile(FileStructure item)
        {
            if (CurrentPath == "/")
            {
                foreach (var cloudItem in _cloudList)
                {
                    if (cloudItem.Name == item.Name)
                    {
                        _currentCloudInfo = cloudItem;
                        _currentCloudInfo.CurrentDir = new FileStructure { Name = "Root" };
                        PreviousPath = CurrentPath;
                        CurrentPath = '/' + _currentCloudInfo.Name + _currentCloudInfo.CurrentDir.Path;
                        return _currentCloudInfo.GetFilesInCurrentDir();
                    }
                }
            }
            else if (item.FileExtension == null)
            {
                _currentCloudInfo.CurrentDir = item;
                PreviousPath = CurrentPath;
                CurrentPath = '/' + _currentCloudInfo.Name + _currentCloudInfo.CurrentDir.Path;
                return _currentCloudInfo.GetFilesInCurrentDir();
            }

            return null;
        }

        /// <summary>
        /// Saves information about clouds into a file.
        /// </summary>
        public void SaveInfo()
        {
            var clouds = _db.Clouds.Local;
            foreach (var item in _cloudList)
            {
                var currentCloud = new Cloud {CloudName = item.Name, CloudType = item.Cloud.ToString(), UserName = "User1"};
                bool exists = false;
                foreach (var cloud in clouds)
                {
                    if (cloud.CloudName == currentCloud.CloudName && cloud.CloudType == currentCloud.CloudType)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _db.Clouds.Add(currentCloud);
                }
                
            }

            _db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets information about clouds from a file
        /// </summary>
        public void GetInfo()
        {
            _db.Clouds.Load();
            try
            {
                foreach (var item in _db.Clouds)
                {
                    AddCloud(item.CloudName, item.CloudType);
                }
            }
            catch (Exception)
            {
                // TODO: Add log messages

            }
        }

        #endregion

    }
}
