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

using Cloud_Manager.Converters;
using Cloud_Manager.Factories;

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

        private static Drives currentDrive;

        private ObservableCollection<FileStructure> folderItems;

        public readonly ICollection<FileStructure> selectedItems = new Collection<FileStructure>();
        public readonly ICollection<FileStructure> cutItems = new Collection<FileStructure>();

        private string currentPath;
        public string previousPath, downloadFileName;

        public static MainWindow mainWindow;

        private CloudDrive[] cloudDrives;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.currentPath = "/";
            this.previousPath = "/";
            this.progressBar.Visibility = Visibility.Collapsed;
            this.OnPropertyChanged("WindowTitle");
            mainWindow = this;

            InitStartFolder();
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

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

            GoogleDriveFactory gdFactory = new GoogleDriveFactory();
            DropboxFactory dropboxFactory = new DropboxFactory();

            cloudDrives = new CloudDrive[2];

            cloudDrives[0] = gdFactory.Create();
            cloudDrives[1] = dropboxFactory.Create();
        }

        public void ChangeVisibilityOfProgressBar(Visibility visibility, bool isIndeterminate = true)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.progressBar.Value = 0;
                this.progressBar.Visibility = visibility;
                this.progressBar.IsIndeterminate = isIndeterminate;
            }));
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentPath == "/")
            {
                FolderItems = new ObservableCollection<FileStructure>();
                FolderItems.Add(new FileStructure());
                FolderItems.Add(new FileStructure());

                FolderItems[0].Name = "Google Drive";
                FolderItems[1].Name = "Dropbox";
            }
            else
            {
                var previous = previousPath;
                switch (currentDrive)
                {
                    case Drives.GoogleDrive:
                        if (CurrentPath == "/Google Drive/Trash")
                        {
                            cloudDrives[0].InitTrash();
                            FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        }
                        else
                        {
                            cloudDrives[0].InitFolder(CurrentPath, FolderItems[0].Parents[0]);
                            FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        }
                        break;

                    case Drives.Dropbox:
                        if (CurrentPath == "/Dropbox/Trash")
                        {
                            cloudDrives[1].InitTrash();
                            FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        }
                        else
                        {
                            cloudDrives[1].InitFolder(CurrentPath);
                            FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        }
                        break;
                }

                previousPath = previous;
            }            
        }

        private void goUp_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath == "/")
                return;
            else if(CurrentPath == "/Google Drive" || CurrentPath == "/Dropbox")
            {
                previousPath = CurrentPath;
                CurrentPath = "/";

                FolderItems = new ObservableCollection<FileStructure>();
                FolderItems.Add(new FileStructure());
                FolderItems.Add(new FileStructure());
                FolderItems[0].Name = "Google Drive";
                FolderItems[1].Name = "Dropbox";
            }
            else
            {
                switch(currentDrive)
                {
                    case Drives.GoogleDrive:
                        if (CurrentPath == "/Google Drive/Trash")
                        {
                            cloudDrives[0].InitFolder("/Google Drive", "root");
                            FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        }
                        else
                        {
                            var tmpstr = CurrentPath.Substring(CurrentPath.LastIndexOf("/") + 1);
                            FilesResource.ListRequest listRequest = (cloudDrives[0] as GoogleDriveManager).service.Files.List();
                            listRequest.PageSize = 1;
                            listRequest.Fields = "nextPageToken, files(id, name, parents)";
                            listRequest.Q = "name = '" + tmpstr + "'";
                            var files = listRequest.Execute().Files;
                            cloudDrives[0].InitFolder(CurrentPath.Substring(0, CurrentPath.LastIndexOf("/")), files[0].Parents[0]);
                            FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        }
                        break;

                    case Drives.Dropbox:
                        if(CurrentPath == "/Dropbox/Trash")
                        {
                            cloudDrives[1].InitFolder(CurrentPath, "root");
                            FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        }
                        else
                        {
                            cloudDrives[1].InitFolder(CurrentPath.Substring(0, CurrentPath.LastIndexOf("/")));
                            FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        }

                        break;
                }
            }
            NotifyMenuItems();
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            previousPath = CurrentPath;
            CurrentPath = "/";

            FolderItems = new ObservableCollection<FileStructure>();
            FolderItems.Add(new FileStructure());
            FolderItems.Add(new FileStructure());
            FolderItems[0].Name = "Google Drive";
            FolderItems[1].Name = "Dropbox";

            NotifyMenuItems();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            if (previousPath == "/")
            {
                previousPath = CurrentPath;
                CurrentPath = "/";

                FolderItems = new ObservableCollection<FileStructure>();
                FolderItems.Add(new FileStructure());
                FolderItems.Add(new FileStructure());
                FolderItems[0].Name = "Google Drive";
                FolderItems[1].Name = "Dropbox";
            }
            else if (previousPath == "/Google Drive")
            {
                cloudDrives[0].InitFolder("/Google Drive", "root");
                FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);

            }
            else if (previousPath == "/Dropbox")
            {
                cloudDrives[1].InitFolder("/Dropbox", "root");
                FolderItems = FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
            }
            else
            {
                switch(currentDrive)
                {
                    case Drives.GoogleDrive:
                        FilesResource.ListRequest listRequest = (cloudDrives[0] as GoogleDriveManager).service.Files.List();
                        string tmpPath = previousPath;
                        tmpPath = tmpPath.Substring(tmpPath.LastIndexOf("/") + 1);
                        listRequest.Q = "name = '" + tmpPath + "'";
                        listRequest.PageSize = 1;
                        listRequest.Fields = "nextPageToken, files(id, name, parents)";
                        var files = listRequest.Execute().Files;
                        if (files != null && files.Count > 0)
                        {
                            cloudDrives[0].InitFolder(previousPath, files[0].Id);
                            FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        }
                        break;

                    case Drives.Dropbox:
                        cloudDrives[1].InitFolder(previousPath);
                        FolderItems = FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        break;
                }
            }
            NotifyMenuItems();
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.selectedItems.First();
            cloudDrives[(int)currentDrive].DownloadFile(selectedItem.Name, selectedItem.Id);
        }

        private void upload_Click(object sender, RoutedEventArgs e)
        {
            cloudDrives[(int)currentDrive].UploadFile();
            
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
            cloudDrives[(int)currentDrive].PasteFiles(cutItems);
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
            cloudDrives[(int)currentDrive].CreateFolder(txtNewFolderName.Text);
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            cloudDrives[(int)currentDrive].RemoveFile(selectedItems);
            
        }

        private void trash_Click(object sender, RoutedEventArgs e)
        {
            cloudDrives[(int)currentDrive].TrashFile(selectedItems);
            OnPropertyChanged("FolderItems");
        }

        private void untrash_Click(object sender, RoutedEventArgs e)
        {
            cloudDrives[(int)currentDrive].UnTrashFile(selectedItems);
            OnPropertyChanged("FolderItems");
        }

        private void clearTrash_Click(object sender, RoutedEventArgs e)
        {
            cloudDrives[(int)currentDrive].ClearTrash();
            OnPropertyChanged("FolderItems");
        }

        private void rename_Click(object sender, RoutedEventArgs e)
        {
            this.popupRenameFile.IsOpen = true;
        }

        private void renameFile_Click(object sender, RoutedEventArgs e)
        {

            MainWindow.mainWindow.popupNewFolder.IsOpen = false;
            cloudDrives[(int)currentDrive].RenameFile(selectedItems, txtRenamedFile.Text);
            selectedItems.Clear();
            OnPropertyChanged("FolderItems");
        }
        
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
            get { return this.selectedItems.Count > 0 && CurrentPath == "/Google Drive/Trash"; }
        }

        public bool IsDownloadAvailable
        {
            get { return this.selectedItems.Count(item => item.FileExtension != null) == 1 && this.selectedItems.Count == 1 && CurrentPath != "/"; }
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
            if (CurrentPath == "/")
            {
                switch(item.Name)
                {
                    case "Google Drive":
                        currentDrive = Drives.GoogleDrive;
                        cloudDrives[0].InitFolder(CurrentPath+"Google Drive", "root");
                        FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        break;

                    case "Dropbox":
                        currentDrive = Drives.Dropbox;
                        cloudDrives[1].InitFolder(CurrentPath + "Dropbox", "root");
                        FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        break;
                }
            }
            else if(item.FileExtension == null)
            {
                switch(currentDrive)
                {
                    case Drives.GoogleDrive:
                        cloudDrives[0].InitFolder(CurrentPath + "/" + item.Name, item.Id);
                        FolderItems = FileStructure.Convert((cloudDrives[0] as GoogleDriveManager).FolderItems, currentPath);
                        break;
                    case Drives.Dropbox:
                        cloudDrives[1].InitFolder(CurrentPath + "/" + item.Name, item.Id);
                        FolderItems = FileStructure.Convert((cloudDrives[1] as DropboxManager).FolderItems, currentPath);
                        break;
                }
            }
        }

        public string CurrentPath
        {
            get
            {
                return this.currentPath != null ? this.currentPath : "/";
            }
            set
            {
                if (this.currentPath != value)
                {
                    this.currentPath = value;
                    this.OnPropertyChanged("CurrentPath");
                    this.OnPropertyChanged("WindowTitle");
                }
            }
        }

        public string WindowTitle
        {
            get { return string.Format("{0} - {1}", windowName, this.CurrentPath); }
        }
    }
}


