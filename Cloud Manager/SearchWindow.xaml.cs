using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Cloud_Manager
{
    /// <summary>
    /// Логика взаимодействия для SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow
    {
        private readonly List<CloudInfo> _clouds;

        /// <summary>
        /// Creating a search window and filling it with data
        /// </summary>
        /// <param name="clouds"></param>
        public SearchWindow(List<CloudInfo> clouds)
        {
            _clouds = new List<CloudInfo>(clouds);
            InitializeComponent();
            _clouds.Reverse();
            _clouds.Add(new CloudInfo { Name = Properties.Resources.SearchWindowAllClouds });
            _clouds.Reverse();

            ComboBoxDate.SelectedIndex = 0;
            ComboBoxClouds.ItemsSource = _clouds;
            ComboBoxClouds.DisplayMemberPath = "Name";
            ComboBoxClouds.SelectedIndex = 0;
            ComboBoxLess.SelectedIndex = 0;
            ComboBoxGreater.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles search button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string nameOfFile = TbFileName.Text;
            string cloudName = (ComboBoxClouds.SelectionBoxItem as CloudInfo)?.Name == Properties.Resources.SearchWindowAllClouds
                ? Properties.Resources.SearchWindowAllClouds
                : (ComboBoxClouds.SelectionBoxItem as CloudInfo)?.Name;

            int minSize = TbSizeAbove.Text == "" ? 0 : Int32.Parse(TbSizeAbove.Text);
            int maxSize = TbSizeLess.Text == "" ? 0 : Int32.Parse(TbSizeLess.Text);
            int cbIndex1 = ComboBoxGreater.SelectedIndex;
            int cbIndex2 = ComboBoxLess.SelectedIndex;

            DateTime? date = null;
            if (DatePickerModification.SelectedDate != null)
            {
                date = (DateTime)DatePickerModification.SelectedDate;
            }

            bool isBeforeDate = ComboBoxDate.SelectedIndex == 0;

            minSize = ConvertToBytes(minSize, cbIndex1);
            maxSize = ConvertToBytes(maxSize, cbIndex2);

            List<FileStructure> files = new List<FileStructure>();
            if (cloudName != Properties.Resources.SearchWindowAllClouds)
            {
                List<FileStructure> tmpFiles = (ComboBoxClouds.SelectionBoxItem 
                    as CloudInfo)?.SearchFiles(nameOfFile, minSize, maxSize, date,
                    isBeforeDate);
                if (tmpFiles != null)
                {
                    foreach (var item in tmpFiles)
                    {
                        files.Add(item);
                    }

                }
            }
            else
            {
                foreach (var item in _clouds)
                {
                    if (item.Name != Properties.Resources.SearchWindowAllClouds)
                    {
                        List<FileStructure> tmpFiles = item.SearchFiles(nameOfFile, minSize, maxSize, date, isBeforeDate);
                        foreach (var file in tmpFiles)
                        {
                            files.Add(file);
                        }
                    }
                }
            }
            files.Sort();

            ObservableCollection<FileStructure> obsColFiles = new ObservableCollection<FileStructure>(files);

            MainWindow.WindowObject.FolderItems = obsColFiles;
            Close();
            MainWindow.WindowObject.IsEnabled = true;
        }

        /// <summary>
        /// Converts file size into bytes
        /// </summary>
        /// <param name="size"></param>
        /// <param name="cbIndex">Type of size: 1 - KB, 2 - MB, 3 - GB</param>
        /// <returns></returns>
        private static int ConvertToBytes(int size, int cbIndex)
        {
            switch (cbIndex)
            {
                default:
                    return size;
                case 1:
                    return 1024 * size;
                case 2:
                    return 1024 * 1024 * size;
                case 3:
                    return 1024 * 1024 * 1024 * size;
            }
        }


        private void SearchWindow_OnClosing(object sender, CancelEventArgs e)
        {
            MainWindow.WindowObject.IsEnabled = true;
        }
    }

    
}
