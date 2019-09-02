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
        #region Properties

        /// <summary>
        /// Gets or sets of the cloud.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets the cloud type.
        /// </summary>
        public CloudDrive Cloud { get; }
        /// <summary>
        /// Gets or sets a list of the cloud files.
        /// </summary>
        public ObservableCollection<FileStructure> Files { get; set; }
        /// <summary>
        /// Gets or sets the current directory.
        /// </summary>
        public FileStructure CurrentDir { get; set; }


        #endregion

        #region Constructor
        public CloudInfo(string name, CloudDrive cloud)
        {
            Name = name;
            Cloud = cloud;
            Files = Cloud.GetFiles();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Gets files that are in the current directory.
        /// </summary>
        /// <returns>A list of files that are in the current directory.</returns>
        public ObservableCollection<FileStructure> GetFilesInCurrentDir()
        {
            List<FileStructure> files = new List<FileStructure>();
            if (CurrentDir.Name == "Trash")
            {
                foreach (var item in Files)
                {
                    if (item.IsTrashed == true)
                    {
                        files.Add(item);
                    }
                }
            }
            else if (CurrentDir.Name == "Root")
            {
                foreach (var item in Files)
                {
                    if (item.IsInRoot && item.IsTrashed == false)
                    {
                        files.Add(item);
                    }
                }
            }
            else
            {
                foreach (var item in Files)
                {
                    if (item.Parents.Count > 0 && item.Parents[0] == CurrentDir.Id)
                    {
                        files.Add(item);
                    }
                }
            }

            files.Sort();
            return new ObservableCollection<FileStructure>(files);
        }

        #endregion

    }
}
