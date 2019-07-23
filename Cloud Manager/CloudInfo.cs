using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    public class CloudInfo
    {
        public string Name { get; set; }
        public CloudDrive Cloud { get; set; }
        public ObservableCollection<FileStructure> Files {get;set; }
        public FileStructure CurrentDir { get; set; }

        public CloudInfo(string name, CloudDrive cloud)
        {
            Name = name;
            Cloud = cloud;
            Files = Cloud.GetFiles();
        }

        public ObservableCollection<FileStructure> GetFilesInCurrentDir()
        {
            ObservableCollection<FileStructure> currentDirFiles = new ObservableCollection<FileStructure>();
            if(CurrentDir.Name == "Trash")
            {
                foreach (var item in Files)
                {
                    if(item.IsTrashed == true)
                    {
                        currentDirFiles.Add(item);
                    }
                }
            }
            else if(CurrentDir.Name == "Root")
            {
                foreach(var item in Files)
                {
                    if (item.IsInRoot && item.IsTrashed==false)
                        currentDirFiles.Add(item);
                }
            }
            else
            {
                foreach (var item in Files)
                {
                    if (item.Parents.Count > 0 && item.Parents[0] == CurrentDir.Id)
                        currentDirFiles.Add(item);
                }
            }
            return currentDirFiles;
        }
    }
}
