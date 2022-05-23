using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Kaenx.DataContext.Catalog;

namespace KnxProdViewer.Models
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public class DeviceModel
    {
        public DeviceViewModel Device {get;set;}
        public List<ApplicationViewModel> Applications {get;set;}
    }
}
