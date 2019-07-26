using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public static string windowName = "Cloud Manager";

        public static MainWindow mainWindow;

        private readonly CloudManagerLogic _cloudManagerLogic;

        // Buttons availability
        public bool IsDriveOpened
        {
            get { return _cloudManagerLogic.CurrentPath != "/"; }
        }

        public bool IsExistItems
        {
            get
            {
                return _cloudManagerLogic.CutItems.Any() && _cloudManagerLogic.CurrentPath != "/";
            }
        }

        public bool IsSingleSelected
        {
            get { return _cloudManagerLogic.SelectedItems.Count() == 1 && _cloudManagerLogic.CurrentPath != "/"; }
        }

        public bool IsSelected
        {
            get { return _cloudManagerLogic.SelectedItems.Count > 0 && _cloudManagerLogic.CurrentPath != "/"; }
        }

        public bool IsSelectedInTrash
        {
            get { return _cloudManagerLogic.SelectedItems.Count > 0 &&  _cloudManagerLogic.CurrentPath.Substring(_cloudManagerLogic.CurrentPath.LastIndexOf('/')) == "/Trash"; }
        }

        public bool IsDownloadAvailable
        {
            get { return _cloudManagerLogic.SelectedItems.Count(item => item.FileExtension != null) == 1 && _cloudManagerLogic.SelectedItems.Count == 1 && _cloudManagerLogic.CurrentPath != "/"; }
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
                OnPropertyChanged("FolderItems");
            }
        }

        public string WindowTitle
        {
            get { return string.Format("{0} - {1}", windowName, _cloudManagerLogic.CurrentPath); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            _cloudManagerLogic = new CloudManagerLogic();
            InitializeComponent();
            DataContext = this;
            OnPropertyChanged("WindowTitle");
            mainWindow = this;

            _cloudManagerLogic.AddCloud("GoogleDrive", CloudManagerType.GoogleDrive);
            _cloudManagerLogic.AddCloud("Dropbox", CloudManagerType.Dropbox);

            FolderItems = _cloudManagerLogic.InitStartFolder();
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.RefreshInfo();
        }

        private void goUp_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetParentDirectory();
            NotifyMenuItems();
        }

        private void home_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetHomeDirectory();
            NotifyMenuItems();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            FolderItems = _cloudManagerLogic.GetPreviousDirectory();
            NotifyMenuItems();
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.DownloadFile();
            NotifyMenuItems();
        }

        private void upload_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.UploadFile();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void cut_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.CutFiles();
            NotifyMenuItems();
        }

        private void paste_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.PasteFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void makeDir_Click(object sender, RoutedEventArgs e)
        {
            this.popupNewFolder.IsOpen = true;
        }

        private void createFolder_Click(object sender, RoutedEventArgs e)
        {
            popupNewFolder.IsOpen = false;
            _cloudManagerLogic.CreateFolder(txtNewFolderName.Text);
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void remove_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.RemoveFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void trash_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.TrashFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void untrash_Click(object sender, RoutedEventArgs e)
        {
            _cloudManagerLogic.UnTrashFiles();
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void clearTrash_Click(object sender, RoutedEventArgs e)
        {
           _cloudManagerLogic.ClearTrash();
           FolderItems = _cloudManagerLogic.RefreshInfo();
           NotifyMenuItems();
        }

        private void rename_Click(object sender, RoutedEventArgs e)
        {
            this.popupRenameFile.IsOpen = true;
        }

        private void renameFile_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.popupNewFolder.IsOpen = false;
            _cloudManagerLogic.RenameFile(txtRenamedFile.Text);
            FolderItems = _cloudManagerLogic.RefreshInfo();
            NotifyMenuItems();
        }

        private void NotifyMenuItems()
        {
            OnPropertyChanged("IsDriveOpened");
            OnPropertyChanged("IsExistItems");
            OnPropertyChanged("IsSingleSelected");
            OnPropertyChanged("IsSelected");
            OnPropertyChanged("IsDownloadAvailable");
            OnPropertyChanged("WindowTitle");
            OnPropertyChanged("CurrentPath");
        }

        private void gridItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var items = e.AddedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    _cloudManagerLogic.SelectedItems.Add(item);
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                var items = e.RemovedItems.Cast<FileStructure>();
                foreach (var item in items)
                {
                    _cloudManagerLogic.SelectedItems.Remove(item);
                }
            }

            NotifyMenuItems();
        }

        private void gridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileStructure item = this.gridItems.SelectedItem as FileStructure;

            if (item == null)
                return;

            FolderItems = _cloudManagerLogic.EnterFile(item);

            NotifyMenuItems();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            switch (menuItem.Header.ToString())
            {
                case "English":
                    _cloudManagerLogic.ChangeLanguage("en-US");
                    MessageBox.Show("Program will run in English after restart.");
                    break;

                case "Русский":
                    _cloudManagerLogic.ChangeLanguage("ru-RU");
                    MessageBox.Show("Программа сменит язык после рестарта.");
                    break;
            }
        }

    }
}


