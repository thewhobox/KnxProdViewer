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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Kaenx.DataContext.Catalog;
using KnxProdViewer.Models;
using Microsoft.EntityFrameworkCore;

namespace KnxProdViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<DeviceModel> Devices {get;set;} = new ObservableCollection<DeviceModel>();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void DoOpenImport(object sender, RoutedEventArgs e)
        {
            ImportDialog diag = new ImportDialog();
            diag.ShowDialog();
        }
        
        private void DoRefresh(object sender, RoutedEventArgs e)
        {
            Devices.Clear();
            using(CatalogContext context = new CatalogContext())
            {
                context.Database.Migrate();
                foreach(DeviceViewModel model in context.Devices)
                {
                    DeviceModel dmod = new DeviceModel();
                    dmod.Device = model;
                    dmod.Applications = context.Applications.Where(a => a.HardwareId == model.HardwareId).ToList();
                    Devices.Add(dmod);
                }
            }
        }
        
        private void DoDelete(object sender, RoutedEventArgs e)
        {
            //ApplicationViewModel model = App
            using(CatalogContext context = new CatalogContext())
            {
                //AppList.ItemsSource = context.Devices.ToList();
            }
        }
        
        private void DoLoad(object sender, RoutedEventArgs e)
        {
            if(VersionList.SelectedItem == null)
            {
                MessageBox.Show("Wählen Sie zuerst eine Applikation aus");
                return;
            }

            ParameterView view = new ParameterView();
            parameterView.Content = view;
            view.Load(VersionList.SelectedItem as ApplicationViewModel);
        }
    }
}
