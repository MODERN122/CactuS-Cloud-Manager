using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    public class FileStructure : IComparable
    {
        #region Properties

        /// <summary>
        /// Sets or gets Id of the file.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Sets or gets Name of the file.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Sets or gets Size of the file
        /// </summary>
        public long? Size { get; set; }
        /// <summary>
        /// Sets or gets File extension.
        /// </summary>
        public string FileExtension { get; set; }
        /// <summary>
        /// Gets or sets time of file's last modification.
        /// </summary>
        public DateTime? ModifiedByMeTime { get; set; }
        /// <summary>
        /// Gets or sets a list of file's parents.
        /// </summary>
        public List<string> Parents { get; set; }
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string Path { get; set; }

        public bool IsFile { get; set; }
        public bool? IsTrashed { get; set; }
        public bool IsInRoot { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of FileStructure class.
        /// </summary>
        public FileStructure() { }

        /// <summary>
        /// Initializes a new instance of FileStructure class.
        /// </summary>
        /// <param name="parents">A list of file's parents.</param>
        public FileStructure(string id, string name, long size, string fileExtension, DateTime modifiedByMeTime, List<string> parents, string path, bool isFile, bool isTrashed, bool isInRoot)
        {
            Id = id;
            Name = name;
            Size = size;
            FileExtension = fileExtension;
            ModifiedByMeTime = modifiedByMeTime;
            Parents = parents;
            Path = path;
            IsFile = isFile;
            IsTrashed = isTrashed;
            IsInRoot = isInRoot;
        }

        /// <summary>
        /// Initializes a new instance of FileStructure class. Converts from GoogleDriveFile into FileStructure. 
        /// </summary>
        /// <param name="file">A Google Drive API's type of file.</param>
        public FileStructure(Google.Apis.Drive.v3.Data.File file)
        {
            Id = file.Id;
            Name = file.Name;
            Size = file.Size;
            FileExtension = file.FileExtension;
            ModifiedByMeTime = file.ModifiedByMeTime;
            Parents = (List<string>)file.Parents;
            IsFile = file.FileExtension != null ? true : false;
            IsTrashed = file.Trashed;
            IsInRoot = false;
        }

        /// <summary>
        /// Initializes a new instance of FileStructure class. Converts from DropboxFile into FileStructure.
        /// </summary>
        /// <param name="file">A Dropbox API's type of file.</param>
        public FileStructure(Dropbox.Api.Files.Metadata file)
        {
            IsFile = file.IsFile;
            if (IsFile)
            {
                Dropbox.Api.Files.FileMetadata fileMetadata = file.AsFile;
                Id = fileMetadata.Id;
                Name = fileMetadata.Name;
                Size = (long)fileMetadata.Size;
                FileExtension = Name.Substring(Name.LastIndexOf('.') + 1);
                ModifiedByMeTime = fileMetadata.ClientModified;
                Path = fileMetadata.PathDisplay;

            }
            else
            {
                Dropbox.Api.Files.FolderMetadata folderMetadata = file.AsFolder;
                Id = folderMetadata.Id;
                Name = folderMetadata.Name;
                Path = folderMetadata.PathDisplay;
            }
            Parents = new List<string>();
            IsInRoot = false;
            IsTrashed = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts a list of Google Drive API's files into a list of FileStructure files.
        /// </summary>
        /// <param name="folderItems">A list of GoogleDrive API's files.</param>
        /// <returns>A list of FileStructure files</returns>
        internal static ObservableCollection<FileStructure> Convert(ObservableCollection<Google.Apis.Drive.v3.Data.File> folderItems)
        {
            ObservableCollection<FileStructure> files = new ObservableCollection<FileStructure>();
            foreach (var item in folderItems)
            {
                files.Add(new FileStructure(item));
            }
            files = SetPaths(files);
            return files;
        }

        /// <summary>
        /// Sets paths for the files using their list of parents.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static ObservableCollection<FileStructure> SetPaths(ObservableCollection<FileStructure> files)
        {
            foreach (var item in files)
            {
                if (item.Parents[0] == GoogleDriveManager.root)
                {
                    item.IsInRoot = true;
                    item.Path = "/" + item.Name;
                }
                if (item.IsTrashed == true)
                    item.Path = "/Trash/" + item.Name;
            }

            bool flag;

            do
            {
                flag = false;
                foreach (var item in files)
                {
                    if (item.Path == null)
                    {
                        flag = true;
                        foreach (var temp in files)
                        {

                            if (temp.Path != null && temp.Id == item.Parents[0])
                            {
                                item.Path = temp.Path + "/" + item.Name;
                                break;
                            }
                        }
                    }
                }
            }
            while (flag);
            return files;
        }

        /// <summary>
        /// Converts a list of Dropbox API's files into a list of FileStructure files.
        /// </summary>
        /// <param name="folderItems">A list of Dropbox API's files.</param>
        /// <returns>A list of FileStructure files.</returns>
        internal static ObservableCollection<FileStructure> Convert(List<Dropbox.Api.Files.Metadata> folderItems)
        {
            ObservableCollection<FileStructure> files = new ObservableCollection<FileStructure>();
            foreach (var item in folderItems)
            {
                files.Add(new FileStructure(item));
            }
            files = SetParents(files);
            return files;
        }

        /// <summary>
        /// Sets a list of parents for the files using their paths.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private static ObservableCollection<FileStructure> SetParents(ObservableCollection<FileStructure> files)
        {
            bool flag;
            do
            {
                flag = false;
                foreach (var item in files)
                {
                    if (item.IsInRoot == true || item.Parents.Count > 0)
                        continue;
                    if (item.Path == '/' + item.Name)
                    {
                        item.IsInRoot = true;
                        continue;
                    }
                    flag = true;
                    string prevDirPath = item.Path.Substring(0, item.Path.LastIndexOf('/'));
                    foreach (var tmp in files)
                    {
                        if (tmp.Path == prevDirPath)
                        {
                            item.Parents.Add(tmp.Id);
                            break;
                        }
                    }
                }
            } while (flag);

            return files;
        }

        #endregion

        public int CompareTo(object obj)
        {
            FileStructure file = obj as FileStructure;
            if (file != null)
                return this.Name.CompareTo(file.Name);
            throw new Exception("Not possible to compare these objects");
        }
    }
}
