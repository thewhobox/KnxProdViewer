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
            if(DeviceList.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie zuerst ein Gerät aus");
                return;
            }
            //ApplicationViewModel model = App
            using(CatalogContext _context = new CatalogContext())
            {
                DeviceModel device = DeviceList.SelectedItem as DeviceModel;

                foreach(ApplicationViewModel app in device.Applications)
                {
                    DeleteApplication(app, _context);
                }

                _context.SaveChanges();

                foreach(Hardware2AppModel hard in _context.Hardware2App.ToList())
                {
                    if(!_context.Applications.Any(a => a.HardwareId == hard.Id))
                    {
                        _context.Hardware2App.Remove(hard);
                        IEnumerable<object> tempList = _context.Devices.Where(d => d.HardwareId == hard.Id);
                        _context.RemoveRange(tempList);
                    }
                }

                _context.SaveChanges();
            }
        }

        private void DeleteApplication(ApplicationViewModel app, CatalogContext _context)
        {
            IEnumerable<object> tempList = _context.AppSegments.Where(a => a.ApplicationId == app.Id);
            _context.RemoveRange(tempList);

            tempList = _context.AppComObjects.Where(a => a.ApplicationId == app.Id);
            _context.RemoveRange(tempList);

            tempList = _context.AppAdditionals.Where(a => a.ApplicationId == app.Id);
            _context.RemoveRange(tempList);

            tempList = _context.AppParameters.Where(a => a.ApplicationId == app.Id);
            _context.RemoveRange(tempList);

            //Check database so everything deletes

            List<AppParameterTypeViewModel> toDelete = new List<AppParameterTypeViewModel>();
            tempList = _context.AppParameterTypes.Where(p => p.ApplicationId == app.Id);
            foreach (AppParameterTypeViewModel pType in tempList)
            {
                if (pType.Type == ParamTypes.Enum)
                {
                    IEnumerable<AppParameterTypeEnumViewModel> tempList2 = _context.AppParameterTypeEnums.Where(e => e.TypeId == pType.Id);
                    _context.AppParameterTypeEnums.RemoveRange(tempList2);
                }
                toDelete.Add(pType);
            }
            _context.AppParameterTypes.RemoveRange(toDelete);
            _context.Applications.Remove(app);
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
