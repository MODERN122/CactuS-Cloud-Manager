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
        private readonly List<CloudInfo> _cloudsList;
        public delegate void AddCloud(string name, CloudManagerType type);

        private readonly AddCloud _addCloudMethod;

        public AddCloudWindow(AddCloud addCloudMethod, List<CloudInfo> clouds)
        {
            InitializeComponent();

            _addCloudMethod = addCloudMethod;
             _cloudsList = clouds;

             List<string> cloudTypeList = new List<string>();

             cloudTypeList.Add("Dropbox");
             cloudTypeList.Add("Google Drive");
             cloudTypeList.Sort();

            ComboBoxClouds.ItemsSource = cloudTypeList;
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
            foreach (var item in _cloudsList)
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
