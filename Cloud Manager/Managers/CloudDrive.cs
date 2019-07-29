using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Cloud_Manager.Managers
{
    enum CloudManagerType
    {
        GoogleDrive,
        Dropbox
    }

    public abstract class CloudDrive
    {
        public abstract void DownloadFile(string name, string id);
        public abstract void UploadFile(FileStructure curDir);
        public abstract void PasteFiles(ICollection<FileStructure> cutFiles, FileStructure curDir);
        public abstract void CreateFolder(string name, FileStructure parentDir);
        public abstract void RemoveFile(ICollection<FileStructure> selectedFiles);
        public abstract void TrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void UnTrashFile(ICollection<FileStructure> selectedFiles);
        public abstract void ClearTrash();
        public abstract void RenameFile(ICollection<FileStructure> selectedFiles, string newName);
        public abstract ObservableCollection<FileStructure> GetFiles();


    }
}
