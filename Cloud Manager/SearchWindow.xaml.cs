using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cloud_Manager
{
    /// <summary>
    /// Логика взаимодействия для SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private readonly List<CloudInfo> _clouds;

        public SearchWindow(List<CloudInfo> clouds)
        {
            this._clouds = clouds;
            InitializeComponent();
            clouds.Reverse();
            clouds.Add(new CloudInfo(){Name = "All clouds"});
            clouds.Reverse();

            this.ComboBoxDate.SelectedIndex = 0;
            this.ComboBoxClouds.ItemsSource = clouds;
            this.ComboBoxClouds.DisplayMemberPath = "Name";
            this.ComboBoxClouds.SelectedIndex = 0;
            this.ComboBoxLess.SelectedIndex = 0;
            this.ComboBoxGreater.SelectedIndex = 0;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string nameOfFile = this.TbFileName.Text;
            string cloudName = (this.ComboBoxClouds.SelectionBoxItem as CloudInfo).Name == "All clouds"
                ? "All clouds"
                : (this.ComboBoxClouds.SelectionBoxItem as CloudInfo).Name;

            int minSize = this.TbSizeAbove.Text == "" ? 0 : Int32.Parse(this.TbSizeAbove.Text);
            int maxSize = this.TbSizeLess.Text == "" ? 0 : Int32.Parse(this.TbSizeLess.Text);
            int cbIndex1 = this.ComboBoxGreater.SelectedIndex;
            int cbIndex2 = this.ComboBoxLess.SelectedIndex;

            DateTime? date = null;
            if (this.DatePickerModification.SelectedDate != null)
            {
                date = (DateTime) this.DatePickerModification.SelectedDate;
            }

            bool isBeforeDate = this.ComboBoxDate.SelectedIndex == 0 ? true : false;

            minSize = ConvertToBytes(minSize, cbIndex1);
            maxSize = ConvertToBytes(maxSize, cbIndex2);

            List<FileStructure> files = new List<FileStructure>();
            if (cloudName != "All clouds")
            {
                List<FileStructure> tmpFiles = (this.ComboBoxClouds.SelectionBoxItem as CloudInfo).SearchFiles(nameOfFile, minSize, maxSize, date,
                    isBeforeDate);
                foreach (var item in tmpFiles)
                {
                    files.Add(item);
                }
            }
            else
            {
                foreach (var item in _clouds)
                {
                    if (item.Name != "All clouds")
                    {
                        List<FileStructure> tmpFiles = item.SearchFiles(nameOfFile, minSize, maxSize, date, isBeforeDate);
                        foreach(var file in tmpFiles)
                            files.Add(file);
                    }
                }
            }
            files.Sort();

            ObservableCollection<FileStructure> obsColFiles = new ObservableCollection<FileStructure>(files);
            
            MainWindow.mainWindow.FolderItems = obsColFiles;
            this.Close();
            MainWindow.mainWindow.IsEnabled = true;
        }

        private int ConvertToBytes(int size, int cbIndex)
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
    }
}
