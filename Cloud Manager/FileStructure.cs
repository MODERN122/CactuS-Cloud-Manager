using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public FileStructure() { }
        public FileStructure(string id, string name, long size, string fileExtension, DateTime modifiedByMeTime, List<string> parents, string path, bool isFile)
        {
            Id = id;
            Name = name;
            Size = size;
            FileExtension = fileExtension;
            ModifiedByMeTime = modifiedByMeTime;
            Parents = parents;
            Path = path;
            IsFile = isFile;
        }

        public FileStructure(Google.Apis.Drive.v3.Data.File file, string path)
        {
            Id = file.Id;
            Name = file.Name;
            Size = file.Size;
            FileExtension = file.FileExtension;
            ModifiedByMeTime = file.ModifiedByMeTime;
            Parents = (List<string>)file.Parents;
            IsFile = file.FileExtension != null ? true : false;
            Path = path;
        }

        public FileStructure(Dropbox.Api.Files.Metadata file, string path, List<string> parents)
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
                Parents = parents;
            }
            else
            {
                Dropbox.Api.Files.FolderMetadata folderMetadata = file.AsFolder;
                Id = folderMetadata.Id;
                Name = folderMetadata.Name;
                Path = folderMetadata.PathDisplay;
                Parents = parents;
            }
        }
        
        internal static ObservableCollection<FileStructure> Convert(ObservableCollection<Google.Apis.Drive.v3.Data.File> folderItems, string path)
        {
            ObservableCollection<FileStructure> files = new ObservableCollection<FileStructure>();
            foreach (var item in folderItems)
            {
                files.Add(new FileStructure(item, path));
            }
            return files;
        }

        internal static ObservableCollection<FileStructure> Convert(Dropbox.Api.Files.ListFolderResult folderItems, string path)
        {
            ObservableCollection<FileStructure> files = new ObservableCollection<FileStructure>();
            foreach (var item in folderItems.Entries)
            {
                files.Add(new FileStructure(item, path, null));
            }
            return files;
        }
    }
}
