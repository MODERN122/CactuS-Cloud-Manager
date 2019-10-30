using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Cloud_Manager.Managers;

namespace Cloud_Manager
{
    /// <summary>
    /// Interaction logic for AddCloudWindow.xaml
    /// </summary>
    public partial class AddCloudWindow : Window
    {
        private List<string> _clouds;
        private List<CloudInfo> _cloudsInfo;
        public delegate void AddCloud(string name, CloudManagerType type);

        private AddCloud _addCloudMethod;

        public AddCloudWindow(AddCloud addCloudMethod, List<CloudInfo> clouds)
        {
            InitializeComponent();

            _addCloudMethod = addCloudMethod;
            _cloudsInfo = clouds;

            _clouds = new List<string>();

            _clouds.Add("Dropbox");
            _clouds.Add("Google Drive");
            _clouds.Sort();

            ComboBoxClouds.ItemsSource = _clouds;
            ComboBoxClouds.SelectedIndex = 0;
        }

        private void AddCloudWindow_OnClosing(object sender, CancelEventArgs e)
        {
            MainWindow.WindowObject.IsEnabled = true;
        }

        private void AddCloud_OnClick(object sender, RoutedEventArgs e)
        {
            if (TbCloudName.Text == "")
            {
                MessageBox.Show("Поле названия облака пустое.", "Ошибка");
                return;
            }
            foreach (var item in _cloudsInfo)
            {
                if (TbCloudName.Text == item.Name)
                {
                    MessageBox.Show("Такое название облака уже используется.", "Ошибка");
                    return;
                }
            }


            switch (ComboBoxClouds.SelectedItem)
            {
                case "Dropbox":
                    {
                        _addCloudMethod(TbCloudName.Text, CloudManagerType.Dropbox);
                        break;
                    }

                case "Google Drive":
                    {
                        _addCloudMethod(TbCloudName.Text, CloudManagerType.GoogleDrive);
                        break;
                    }

                default:
                    {
                        throw new NotImplementedException();
                    }
            }
            MainWindow.WindowObject.Refresh();
            Close();
        }
    }
}
