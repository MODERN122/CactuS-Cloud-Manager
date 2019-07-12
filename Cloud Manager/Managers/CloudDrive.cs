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

namespace Cloud_Manager
{
    abstract class CloudDrive
    {
        protected static string downloadFileName;

        protected ObservableCollection<object> folderItems;
        public abstract void InitFolder(string path, string parent="");
        public abstract void InitTrash();
        public abstract void DownloadFile(string name, string id);
        public abstract void UploadFile();
        public abstract void PasteFiles(ICollection<FileStructure> cutFiles);
        public abstract void CreateFolder(string name);
        public abstract void RemoveFile(ICollection<FileStructure> selectedFiles);
        public abstract void TrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void UnTrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void ClearTrash();
        public abstract void RenameFile(ICollection<FileStructure> selectedFiles, string newName);


        public virtual ObservableCollection<Object> FolderItems { get; set; }
    }
}
