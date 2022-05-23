using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Manager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace KnxProdViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ImportDialog : Window
    {
        public IManager Manager { get; set; } 
        public ObservableCollection<ImportDevice> Devices { get; set; } = new ObservableCollection<ImportDevice>();

        public ImportDialog()
        {
            InitializeComponent();
            this.DataContext = this;
            

            this.TaskbarItemInfo = new TaskbarItemInfo();
            this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            this.TaskbarItemInfo.ProgressValue = 0.3;
        }

        private void DoSelectFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
			if(diag.ShowDialog() != true) return;

            Manager = ImportManager.GetImportManager(diag.FileName);
            Manager.DeviceChanged += (a) => Debug.WriteLine($"Device: {a}");
            Manager.StateChanged += (a) => Debug.WriteLine($"State: {a}");
            Devices.Clear();

            
            CatalogContext context = new CatalogContext();
            context.Database.Migrate();
            foreach(ImportDevice d in Manager.GetDeviceList(context))
                Devices.Add(d);

            if(Devices.Any(d => d.ExistsInDatabase))
                MessageBox.Show("Einige der Applikationen existieren bereits in der Datenbank");
        }

        private void DoImport(object sender, RoutedEventArgs e)
        {
            CatalogContext context = new CatalogContext();
            context.Database.Migrate();
            List<ImportDevice> toImport = new List<ImportDevice>();
            foreach(object x in DeviceList.SelectedItems)
                toImport.Add(x as ImportDevice);
            try{
                Manager.StartImport(toImport, context);
            } catch(Exception ex)
            {

            }
        }
    }
}