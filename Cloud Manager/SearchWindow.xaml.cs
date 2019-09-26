using System;
using System.Collections.Generic;
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
        public SearchWindow(List<CloudInfo> clouds)
        {
            InitializeComponent();
            clouds.Reverse();
            clouds.Add(new CloudInfo(){Name = "All clouds"});
            clouds.Reverse();

            this.ComboBoxClouds.ItemsSource = clouds;
            this.ComboBoxClouds.DisplayMemberPath = "Name";
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
