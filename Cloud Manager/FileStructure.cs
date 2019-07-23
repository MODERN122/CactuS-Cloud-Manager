using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    public class FileStructure
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long? Size { get; set; }
        public string FileExtension { get; set; }
        public DateTime? ModifiedByMeTime { get; set; }
        public List<string> Parents { get; set; }
        public string Path { get; set; }
        public bool IsFile { get; set; }
        public bool? IsTrashed { get; set; }
        public bool IsInRoot { get; set; }

        public FileStructure() { }
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

        public FileStructure(Dropbox.Api.Files.Metadata file, string path)
        {
            IsFile = file.IsFile;
            if(IsFile)
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
                    if(item.Path == null)
                    {
                        flag = true;
                        foreach (var temp in files)
                        {

                            if (temp.Path!=null && temp.Id == item.Parents[0])
                            {
                                item.Path = temp.Path + "/" + item.Name;
                                break;
                            }
                        }
                    }
                }
            }
            while (flag) ;
            return files;
        }



        internal static ObservableCollection<FileStructure> Convert(List<Dropbox.Api.Files.Metadata> folderItems)
        {
            ObservableCollection<FileStructure> files = new ObservableCollection<FileStructure>();
            foreach (var item in folderItems)
            {
                files.Add(new FileStructure(item, item.PathDisplay));
            }
            files = SetParents(files);
            return files;
        }

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
    }
}
