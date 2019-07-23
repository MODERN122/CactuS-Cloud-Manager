using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    enum Drives
    {
        GoogleDrive,
        Dropbox
    }
    public partial class MainWindow : INotifyPropertyChanged
    {
        public static string windowName = "Cloud Manager";

        public readonly ICollection<FileStructure> selectedItems = new Collection<FileStructure>();
        public readonly ICollection<FileStructure> cutItems = new Collection<FileStructure>();
        
        public string downloadFileName;

        public static MainWindow mainWindow;

        private List<CloudInfo> cloudsList;
        private CloudInfo currentCloudInfo;

        private string currentPath;
        public string CurrentPath
        {
            get
            {
                return currentPath != null ? currentPath : "/";
            }
            set
            {
                if (currentPath != value)
                {
                    currentPath = value;
                    OnPropertyChanged("CurrentPath");
                    OnPropertyChanged("WindowTitle");
                }
            }
        }

        public string PreviousPath { get; set; }

        // Buttons availability
        public bool IsDriveOpened
        {
            get { return CurrentPath != "/"; }
        }

        public bool IsExistItems
        {
            get
            {
                return this.cutItems.Any() && CurrentPath != "/";
            }
        }

        public bool IsSingleSelected
        {
            get { return this.selectedItems.Count() == 1 && CurrentPath != "/"; }
        }

        public bool IsSelected
        {
            get { return this.selectedItems.Count > 0 && CurrentPath != "/"; }
        }

        public bool IsSelectedInTrash
        {
            get { return this.selectedItems.Count > 0 && CurrentPath == '/' + currentCloudInfo.Name + "/Trash"; }
        }

        public bool IsDownloadAvailable
        {
            get { return this.selectedItems.Count(item => item.FileExtension != null) == 1 && this.selectedItems.Count == 1 && CurrentPath != "/"; }
        }


        private ObservableCollection<FileStructure> folderItems;
        public ObservableCollection<FileStructure> FolderItems
        {
            get
            {
                return this.folderItems;
            }
            set
            {
                this.folderItems = value;
                this.OnPropertyChanged("FolderItems");
                this.OnPropertyChanged("WindowTitle");
            }
        }

        public string WindowTitle
        {
            get { return string.Format("{0} - {1}", windowName, this.CurrentPath); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            CurrentPath = "/";
            PreviousPath = "/";
            this.OnPropertyChanged("WindowTitle");
            mainWindow = this;
            cloudsList = new List<CloudInfo>();
            cloudsList.Add(new CloudInfo("Google Drive", new GoogleDriveManager()));
            cloudsList.Add(new CloudInfo("Dropbox", new DropboxManager()));

            InitStartFolder();
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitStartFolder()
        {
            FolderItems = new ObservableCollection<FileStructure>();
            FolderItems.Add(new FileStructure());
            FolderItems.Add(new FileStructure());

            FolderItems[0].Name = "Google Drive";
            FolderItems[1].Name = "Dropbox";

            selectedItems.Clear();
            cutItems.Clear();

        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentPath == "/")
            {
                InitStartFolder();
            }
            else
            {
                foreach(var item in cloudsList)
                {
                    currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
                    FolderItems = currentCloudInfo.GetFilesInCurrentDir();
                    break;
                }
            }            
        }

        private void goUp_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath == "/")
                return;
            else if (CurrentPath.IndexOf('/') == CurrentPath.LastIndexOf('/'))
            {

                foreach (var item in cloudsList)
                {
                    if (CurrentPath == '/' + item.Name)
                    {
                        PreviousPath = CurrentPath;
                        CurrentPath = "/";

                        InitStartFolder();
                    }
                }
            }
            else
            {
                PreviousPath = CurrentPath;
                CurrentPath = CurrentPath.Substring(0, CurrentPath.Length - currentCloudInfo.CurrentDir.Name.Length - 1);
                if(CurrentPath == '/' + currentCloudInfo.Name)
                {
                    currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
                    FolderItems = currentCloudInfo.GetFilesInCurrentDir();
                }
                else
                {
                    foreach (var item in currentCloudInfo.Files)
                    {

                        if (item.Id == currentCloudInfo.CurrentDir.Parents[0])
                        {
                            currentCloudInfo.CurrentDir = item;
                            FolderItems = currentCloudInfo.GetFilesInCurrentDir();
                            break;
                        }
                    }
                }
            }
            NotifyMenuItems();
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            PreviousPath = CurrentPath;
            CurrentPath = "/";

            InitStartFolder();

            NotifyMenuItems();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            // If previous path is the selection between clouds
            if (PreviousPath == "/")
            {
                InitStartFolder();
            }
            // If previous path is the any cloud's root
            else if (PreviousPath == '/' + currentCloudInfo.Name)
            {
                currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
                FolderItems = currentCloudInfo.GetFilesInCurrentDir();
            }
            // If previous path is the any cloud's trashed files
            else if (PreviousPath == '/' + currentCloudInfo.Name + "/Trash")
            {
                currentCloudInfo.CurrentDir = new FileStructure() { Name = "Trash" };
                FolderItems = currentCloudInfo.GetFilesInCurrentDir();
            }
            else
            {
                string path = PreviousPath;
                path = path.Substring(1); // delete first slash in the path
                path = path.Substring(path.IndexOf('/')); // delete the name of the current cloud
                foreach(var item in currentCloudInfo.Files)
                {
                    if(item.Path == path)
                    {
                        currentCloudInfo.CurrentDir = item;
                        FolderItems = currentCloudInfo.GetFilesInCurrentDir();
                        break;
                    }
                }
            }
            string tmp = PreviousPath;
            PreviousPath = CurrentPath;
            CurrentPath = tmp;
            NotifyMenuItems();
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.selectedItems.First();
            currentCloudInfo.Cloud.DownloadFile(selectedItem.Name, selectedItem.Id);
        }

        private void upload_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.UploadFile(currentCloudInfo.CurrentDir);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
        }

        private void cut_Click(object sender, RoutedEventArgs e)
        {
            cutItems.Clear();
            foreach (var item in selectedItems)
            {
                cutItems.Add(item);
            }
            selectedItems.Clear();

            NotifyMenuItems();
        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.PasteFiles(cutItems, currentCloudInfo.CurrentDir);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
            cutItems.Clear();
            NotifyMenuItems();
            OnPropertyChanged("FolderItems");
        }

        private void makeDir_Click(object sender, RoutedEventArgs e)
        {
            this.popupNewFolder.IsOpen = true;
        }

        private void createFolder_Click(object sender, RoutedEventArgs e)
        {
            popupNewFolder.IsOpen = false;
            currentCloudInfo.Cloud.CreateFolder(txtNewFolderName.Text);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.RemoveFile(selectedItems);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
        }

        private void trash_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.TrashFile(selectedItems);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
            OnPropertyChanged("FolderItems");
        }

        private void untrash_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.UnTrashFile(selectedItems);
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
            OnPropertyChanged("FolderItems");
        }

        private void clearTrash_Click(object sender, RoutedEventArgs e)
        {
            currentCloudInfo.Cloud.ClearTrash();
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
            OnPropertyChanged("FolderItems");
        }

        private void rename_Click(object sender, RoutedEventArgs e)
        {
            this.popupRenameFile.IsOpen = true;
        }

        private void renameFile_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.popupNewFolder.IsOpen = false;
            currentCloudInfo.Cloud.RenameFile(selectedItems, txtRenamedFile.Text);
            selectedItems.Clear();
            currentCloudInfo.Files = currentCloudInfo.Cloud.GetFiles();
            OnPropertyChanged("FolderItems");
        }
        

        private void NotifyMenuItems()
        {
            OnPropertyChanged("IsDriveOpened");
            OnPropertyChanged("IsExistItems");
            OnPropertyChanged("IsSingleSelected");
            OnPropertyChanged("IsSelected");
            OnPropertyChanged("IsDownloadAvailable");
        }

        private void gridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var items = e.AddedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    selectedItems.Add(item);
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                var items = e.RemovedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    selectedItems.Remove(item);
                }
            }

            NotifyMenuItems();
        }

        private void gridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileStructure item = this.gridItems.SelectedItem as FileStructure;
            if (item == null)
                return;
            if (CurrentPath == "/")
            {
                foreach (var cloudItem in cloudsList)
                {
                    if (cloudItem.Name == item.Name)
                    {
                        currentCloudInfo = cloudItem;
                        currentCloudInfo.CurrentDir = new FileStructure() { Name = "Root" };
                        FolderItems = currentCloudInfo.GetFilesInCurrentDir();
                        break;
                    }
                }
            }
            else if(item.FileExtension == null)
            {
                currentCloudInfo.CurrentDir = item;
                FolderItems = currentCloudInfo.GetFilesInCurrentDir();
            }
            PreviousPath = CurrentPath;
            CurrentPath = '/' + currentCloudInfo.Name + currentCloudInfo.CurrentDir.Path;
            NotifyMenuItems();
        }

    }
}


