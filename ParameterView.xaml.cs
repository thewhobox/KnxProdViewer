using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Microsoft.EntityFrameworkCore;

using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Dynamic;
using KnxProdViewer.Models;

namespace KnxProdViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ParameterView : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ApplicationViewModel _app;
        private Dictionary<int, string> values = new Dictionary<int, string>();
        private List<IDynParameter> Parameters = new List<IDynParameter>();
        private List<ParamBinding> Bindings = new List<ParamBinding>();
        private List<ComBinding> _comBindings;
        private List<AppComObject> _comObjects;

        public ObservableCollection<DeviceComObject> ComObjects { get; set; } = new ObservableCollection<DeviceComObject>();
        public ObservableCollection<IDynChannel> Channels { get; set; }

        private ParameterBlock _selectedBlock;
        public ParameterBlock SelectedBlock
        {
            get { return _selectedBlock; }
            set
            {
                if(value == null) return;
                _selectedBlock = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedBlock"));
            }
        }

        private int _selectedBlockId;
        public int SelectedBlockId
        {
            get { return _selectedBlockId; }
            set
            {
                _selectedBlockId = value;
                Debug.WriteLine("Id1:" + value.ToString());
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedBlockId"));
            }
        }

        public ParameterView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public async void Load(ApplicationViewModel app)
        {
            _app = app;

            //TODO load changes
            AppAdditional adds;
            string comsDefault;
            using(CatalogContext _context = new CatalogContext())
            {
                adds = _context.AppAdditionals.Single(a => a.ApplicationId == app.Id);
                _comBindings = FunctionHelper.ByteArrayToObject<List<ComBinding>>(adds.ComsAll, true);
                Channels = FunctionHelper.ByteArrayToObject<ObservableCollection<IDynChannel>>(adds.ParamsHelper, true, "Kaenx.DataContext.Import.Dynamic");
                comsDefault = System.Text.Encoding.UTF8.GetString(adds.ComsDefault);
                _comObjects = _context.AppComObjects.Where(c => c.ApplicationId == app.Id).ToList();
            }

            if(!string.IsNullOrEmpty(comsDefault))
            {
                foreach(string comIdStr in comsDefault.Split(','))
                {
                    int comId = int.Parse(comIdStr);
                    AppComObject comObj = _comObjects.Single(c => c.UId == comId);
                    ComObjects.Add(new DeviceComObject(comObj));
                }
                comsDefault = null;
            }
            
            Bindings = FunctionHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings, true);
            //Assignments = FunctionHelper.ByteArrayToObject<List<AssignParameter>>(adds.Assignments, true);



            foreach (IDynChannel ch in Channels)
            {
                if (!ch.HasAccess)
                {
                    ch.IsVisible = false;
                    continue;
                }

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if (!block.HasAccess)
                    {
                        block.IsVisible = false;
                        continue;
                    }

                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (!para.HasAccess)
                        {
                            para.IsVisible = false;
                            continue;
                        }
                    }
                }
            }

            using(CatalogContext co = new CatalogContext())
            {
                foreach (AppParameter para in co.AppParameters.Where(p => p.ApplicationId == adds.ApplicationId))
                {
                    values.Add(para.ParameterId, para.Value);
                }
            }

            foreach(IDynChannel ch in Channels)
            {
                foreach(ParameterBlock block in ch.Blocks)
                {
                    foreach(IDynParameter para in block.Parameters)
                    {
                        para.PropertyChanged += Para_PropertyChanged;
                        Parameters.Add(para);
                    }
                }
            }

            Changed("ComObjects");
            Changed("Channels");
        }

        private async void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (e != null && e.PropertyName != "Value") return;

            IDynParameter para = sender as IDynParameter;

            string oldValue = values[para.Id];
            if(para.Value == oldValue)
            {
                System.Diagnostics.Debug.WriteLine("Wert unverändert! " + para.Id + " -> " + para.Value);
                return;
            }
            System.Diagnostics.Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);

            IEnumerable<ParamBinding> list = Bindings.Where(b => b.SourceId == para.Id);
            foreach(ParamBinding bind in list)
            {
                switch(bind.Type)
                {
                    case BindingTypes.ComObject:
                        DeviceComObject com = ComObjects.SingleOrDefault(c => c.Id == bind.TargetId);
                        if(com != null)
                        {
                            string val = string.IsNullOrEmpty(para.Value) ? bind.DefaultText : para.Value;
                            com.Name = bind.FullText.Replace("{d}", val);
                        }
                        break;

                    case BindingTypes.ParameterBlock:
                        foreach (IDynChannel ch2 in Channels)
                        {
                            if (ch2.Blocks.Any(b => b.Id == bind.TargetId))
                            {
                                ParameterBlock bl = ch2.Blocks.Single(b => b.Id == bind.TargetId);
                                if (string.IsNullOrEmpty(para.Value) || string.IsNullOrWhiteSpace(para.Value))
                                    bl.Text = bind.FullText.Replace("{d}", bind.DefaultText);
                                else
                                    bl.Text = bind.FullText.Replace("{d}", para.Value);
                            }
                        }
                        break;

                    default:
                        throw new NotImplementedException(bind.Type.ToString());
                }
            }

            CalculateVisibilityParas(para);
            CalculateVisibilityComs(para);
        }

        private void CalculateVisibilityParas(IDynParameter para)
        {
            List<ChannelBlock> list = new List<ChannelBlock>();
            List<ParameterBlock> list2 = new List<ParameterBlock>();
            List<int> list5 = new List<int>();

            values[para.Id] = para.Value;

            foreach (IDynChannel ch in Channels)
            {
                if(ch.HasAccess) ch.IsVisible = FunctionHelper.CheckConditions(ch.Conditions, values);
                else ch.IsVisible = false;
                if(!ch.IsVisible) continue; 

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if (block.HasAccess)
                        block.IsVisible = FunctionHelper.CheckConditions(block.Conditions, values);
                    else
                        block.IsVisible = false;
                }
            }

            IEnumerable<IDynParameter> list3 = Parameters.Where(p => p.Conditions.Any(c => c.SourceId == para.Id)); // || list5.Contains(c.SourceId)));
            
            var x = list3.Where(p => p.Id == 5501);
            foreach (IDynParameter par in list3)
                if(par.HasAccess)
                    par.IsVisible = FunctionHelper.CheckConditions(par.Conditions, values);

            /*IEnumerable<ParamBinding> list4 = Bindings.Where(b => b.SourceId == para.Id);
            foreach (ParamBinding bind in list4)
            {
                switch (bind.Type)
                {
                    case BindingTypes.Channel:
                        IDynChannel ch = Channels.Single(c => c.Id == bind.TargetId);
                        if (ch is ChannelBlock)
                        {
                            ChannelBlock chb = ch as ChannelBlock;
                            if (string.IsNullOrEmpty(para.Value))
                                chb.Text = bind.FullText.Replace("{{dyn}}", bind.DefaultText);
                            else
                                chb.Text = bind.FullText.Replace("{{dyn}}", para.Value);
                        }
                        break;

                    case BindingTypes.ParameterBlock:
                        foreach (IDynChannel ch2 in Channels)
                        {
                            if (ch2.Blocks.Any(b => b.Id == bind.TargetId))
                            {
                                ParameterBlock bl = ch2.Blocks.Single(b => b.Id == bind.TargetId);
                                if (string.IsNullOrEmpty(para.Value) || string.IsNullOrWhiteSpace(para.Value))
                                    bl.Text = bind.FullText.Replace("{{dyn}}", bind.DefaultText);
                                else
                                    bl.Text = bind.FullText.Replace("{{dyn}}", para.Value);
                            }
                        }
                        break;

                    case BindingTypes.ComObject:
                        try
                        {
                            //TODO check what to do
                            //DeviceComObject com = Device.ComObjects.Single(c => c.Id == bind.TargetId);
                            //if (string.IsNullOrEmpty(para.Value))
                            //    com.DisplayName = com.Name.Replace("{{dyn}}", bind.DefaultText);
                            //else
                            //    com.DisplayName = com.Name.Replace("{{dyn}}", para.Value);
                        }
                        catch
                        {
                        }
                        break;
                }
            }*/


            foreach (IDynChannel ch in Channels)
            {
                if (ch.IsVisible)
                {
                    ch.IsVisible = ch.Blocks.Any(b => b.IsVisible);
                }
            }
        }

        private void CalculateVisibilityComs(IDynParameter para)
        {
            IEnumerable<ComBinding> list = _comBindings.Where(co => co.Conditions.Any(c => c.SourceId == para.Id));

            foreach(IGrouping<int, ComBinding> bindings in list.GroupBy(cb => cb.ComId))
            {
                if(bindings.Any(cond => FunctionHelper.CheckConditions(cond.Conditions, values)))
                {
                    if (!ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        AppComObject acom = _comObjects.Single(a => a.ApplicationId == _app.Id && a.Id == bindings.Key);
                        DeviceComObject dcom = new DeviceComObject(acom);
                        ParamBinding bind = Bindings.SingleOrDefault(b => b.TargetId == dcom.Id && b.Type == BindingTypes.ComObject);
                        if(bind != null)
                        {
                            string source = values[bind.SourceId];
                            string val = string.IsNullOrEmpty(source) ? bind.DefaultText : source;
                            dcom.Name = bind.FullText.Replace("{d}", val);
                        }
                        ComObjects.Add(dcom);
                    }
                } else
                {
                    if (ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        DeviceComObject dcom = ComObjects.Single(co => co.Id == bindings.Key);
                        ComObjects.Remove(dcom);
                    }
                }
            }

            //TODO allow to sort for name, function, etc
            //ComObjects.Sort(c => c.Number);
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
