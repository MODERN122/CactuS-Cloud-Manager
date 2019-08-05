using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    class CloudManagerLogic
    { 
        public readonly ICollection<FileStructure> SelectedItems = new Collection<FileStructure>();
        public readonly ICollection<FileStructure> CutItems = new Collection<FileStructure>();

        private readonly List<CloudInfo> _cloudList;
        private CloudInfo _currentCloudInfo;

        private string _currentPath;
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

        public CloudManagerLogic()
        {
            _cloudList = new List<CloudInfo>();

            CurrentPath = "/";
            PreviousPath = "/";
        }

        public void AddCloud(string name, CloudManagerType type)
        {
            switch (type)
            {
                case CloudManagerType.GoogleDrive:
                    _cloudList.Add(new CloudInfo(name, new GoogleDriveManager()));
                    break;

                case CloudManagerType.Dropbox:
                    _cloudList.Add(new CloudInfo(name, new DropboxManager()));
                    break;
            }
            
        }

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

        public ObservableCollection<FileStructure> InitStartFolder()
        {
            var files = new ObservableCollection<FileStructure>();
            foreach (var cloud in _cloudList)
            {
                files.Add(new FileStructure(){Name = cloud.Name});
            }
            SelectedItems.Clear();
            CutItems.Clear();

            return files;
        }

        public ObservableCollection<FileStructure> RefreshInfo()
        {
            if (CurrentPath == "/")
                return InitStartFolder();
            else
            {
                _currentCloudInfo.Files = _currentCloudInfo.Cloud.GetFiles();
                return _currentCloudInfo.GetFilesInCurrentDir();
            }
                
        }

        public ObservableCollection<FileStructure> GetParentDirectory()
        {
            if (CurrentPath == "/")
                return InitStartFolder();
            else if (CurrentPath.IndexOf('/') == CurrentPath.LastIndexOf('/'))
            {

                foreach (var item in _cloudList)
                {
                    if (CurrentPath != '/' + item.Name) continue;

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
                if (CurrentPath == '/' + _currentCloudInfo.Name)
                {
                    _currentCloudInfo.CurrentDir = new FileStructure() {Name = "Root"};
                    return _currentCloudInfo.GetFilesInCurrentDir();
                }
                else
                {
                    foreach (var item in _currentCloudInfo.Files)
                    {
                        if (item.Id != _currentCloudInfo.CurrentDir.Parents[0]) continue;

                        _currentCloudInfo.CurrentDir = item;
                        return _currentCloudInfo.GetFilesInCurrentDir();
                    }
                }
            }

            return null;
        }

        public ObservableCollection<FileStructure> GetHomeDirectory()
        {
            PreviousPath = CurrentPath;
            CurrentPath = "/";

            return InitStartFolder();
        }

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
                _currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
                string tmp = PreviousPath;
                PreviousPath = CurrentPath;
                CurrentPath = tmp;
                return _currentCloudInfo.GetFilesInCurrentDir();
            }
            // If previous path is the any cloud's trashed files
            else if (PreviousPath == '/' + _currentCloudInfo.Name + "/Trash")
            {
                _currentCloudInfo.CurrentDir = new FileStructure() { Name = "Trash" };
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

        public void DownloadFile()
        {
            var selectedItem = SelectedItems.First();
            if (selectedItem != null)
                _currentCloudInfo.Cloud.DownloadFile(selectedItem.Name, selectedItem.Id);
        }

        public void UploadFile()
        {
            _currentCloudInfo.Cloud.UploadFile(_currentCloudInfo.CurrentDir);
        }

        public void CutFiles()
        {
            CutItems.Clear();
            foreach (var item in SelectedItems)
            {
                CutItems.Add(item);
            }
            SelectedItems.Clear();
        }

        public void PasteFiles()
        {
            _currentCloudInfo.Cloud.PasteFiles(CutItems, _currentCloudInfo.CurrentDir);
            CutItems.Clear();
        }

        public void CreateFolder(string name)
        {
            if (name != "")
                _currentCloudInfo.Cloud.CreateFolder(name, _currentCloudInfo.CurrentDir);
        }

        public void RemoveFiles()
        {
            _currentCloudInfo.Cloud.RemoveFile(SelectedItems);
            SelectedItems.Clear();
        }

        public void TrashFiles()
        {
            _currentCloudInfo.Cloud.TrashFile(SelectedItems);
            SelectedItems.Clear();
        }

        public void UnTrashFiles()
        {
            _currentCloudInfo.Cloud.UnTrashFile(SelectedItems);
            SelectedItems.Clear();
        }

        public void ClearTrash()
        {
            _currentCloudInfo.Cloud.ClearTrash();
        }

        public void RenameFile(string name)
        {
            if (name != "")
                _currentCloudInfo.Cloud.RenameFile(SelectedItems, name);

            SelectedItems.Clear();
        }

        public ObservableCollection<FileStructure> EnterFile(FileStructure item)
        {
            if (CurrentPath == "/")
            {
                foreach (var cloudItem in _cloudList)
                {
                    if (cloudItem.Name == item.Name)
                    {
                        _currentCloudInfo = cloudItem;
                        _currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
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
    }
}
