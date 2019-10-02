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


        public CloudInfo()
        {

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

        /// <summary>
        /// Searches files which name contains <paramref name="fileName"/>,
        /// size is above than <paramref name="minSize"/> and less than <paramref name="maxSize"/>,
        /// and have lastModifiedDate less/above than <paramref name="date"/>.
        /// </summary>
        /// <param name="fileName">A part of the file's name.</param>
        /// <param name="minSize">Minimum size of the file.</param>
        /// <param name="maxSize">Maximum size of the file. If it equals 0, than there is no maximum size.</param>
        /// <param name="date">The date of the last file's modification.</param>
        /// <param name="isBeforeDate">True, if the file's last modification date is earlier than <paramref name="date"/></param>
        /// <returns>A list of files that have been searched.</returns>
        public List<FileStructure> SearchFiles(string fileName, int minSize, int maxSize, DateTime? date, bool isBeforeDate)
        {
            var files = new List<FileStructure>();

            foreach (var item in this.Files)
            {
                if (item.IsFile == true && item.Name.Contains(fileName) && item.Size > minSize)
                {
                    if (maxSize != 0 && item.Size < maxSize)
                    {
                        if (date != null && item.ModifiedByMeTime != null)
                        {
                            if ((isBeforeDate && item.ModifiedByMeTime < date) || (!isBeforeDate && item.ModifiedByMeTime > date))
                                files.Add(item);
                        }
                        else if (date == null)
                        {
                            files.Add(item);
                        }
                    }
                    else if (maxSize == 0)
                    {
                        if (date != null && item.ModifiedByMeTime != null)
                        {
                            if ((isBeforeDate && item.ModifiedByMeTime < date) || (!isBeforeDate && item.ModifiedByMeTime > date))
                                files.Add(item);
                        }
                        else if (date == null)
                        {
                            files.Add(item);
                        }
                    }

                }
            }

            return files;
        }

        #endregion

    }
}
