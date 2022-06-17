using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace KnxProdViewer.Controls
{
    public sealed partial class NumberBox : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty ValueOkProperty = DependencyProperty.Register("ValueOk", typeof(int), typeof(NumberBox), new PropertyMetadata(0, new PropertyChangedCallback(TextProperty_PropertyChanged)));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumberBox), new PropertyMetadata(null));


        public delegate string PreviewChangedHandler(NumberBox sender, int Value);
        public event PreviewChangedHandler PreviewChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private static void TextProperty_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            obj.SetValue(ValueProperty, e.NewValue);
        }


        private string _errMessage;
        public string ErrMessage
        {
            get { return _errMessage; }
            set { _errMessage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ErrMessage")); }
        }

        public int Value { 
            get { return (int)GetValue(ValueProperty); }
            set
            {
                bool error = false;
                string handled = PreviewChanged?.Invoke(this, value);
                if (!string.IsNullOrEmpty(handled))
                {
                    error = true;
                    ErrMessage = handled;
                }

                BtnUp.IsEnabled = value < (int)GetValue(MaximumProperty);
                BtnDown.IsEnabled = value > (int)GetValue(MinimumProperty);

                if (value > (int)GetValue(MaximumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl größer als Maximum von " + (int)GetValue(MaximumProperty);
                }

                if (value < (int)GetValue(MinimumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl kleiner als Minimum  von " + (int)GetValue(MinimumProperty);
                }

                SetValue(ValueProperty, value);

                if (!error)
                {
                    SetValue(ValueOkProperty, value);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public int ValueOk
        {
            get { return (int)GetValue(ValueOkProperty); }
            set { SetValue(ValueProperty, value); SetValue(ValueOkProperty, value); }
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set
            {
                SetValue(MaximumProperty, value);

                int _val = (int)GetValue(ValueProperty);
                bool error = false;

                BtnUp.IsEnabled = _val < value;
                if (_val > value)
                    error = true;

                int min = (int)GetValue(MinimumProperty);
                BtnDown.IsEnabled = _val > min;
                if (_val < min)
                    error = true;

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set
            {
                SetValue(MinimumProperty, value);

                int _val = (int)GetValue(ValueProperty);
                bool error = false;

                int max = (int)GetValue(MaximumProperty);
                BtnUp.IsEnabled = _val < max;
                if (_val > max)
                    error = true;

                BtnDown.IsEnabled = _val > value;
                if (_val < value)
                    error = true;

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }
        
        private string Tooltip { get { return Minimum + " - " + Maximum; } }



        public NumberBox()
        {
            this.InitializeComponent();
            DataGrid.DataContext = this;
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.D0:
                case System.Windows.Input.Key.D1:
                case System.Windows.Input.Key.D2:
                case System.Windows.Input.Key.D3:
                case System.Windows.Input.Key.D4:
                case System.Windows.Input.Key.D5:
                case System.Windows.Input.Key.D6:
                case System.Windows.Input.Key.D7:
                case System.Windows.Input.Key.D8:
                case System.Windows.Input.Key.D9:
                case System.Windows.Input.Key.NumPad0:
                case System.Windows.Input.Key.NumPad1:
                case System.Windows.Input.Key.NumPad2:
                case System.Windows.Input.Key.NumPad3:
                case System.Windows.Input.Key.NumPad4:
                case System.Windows.Input.Key.NumPad5:
                case System.Windows.Input.Key.NumPad6:
                case System.Windows.Input.Key.NumPad7:
                case System.Windows.Input.Key.NumPad8:
                case System.Windows.Input.Key.NumPad9:
                case System.Windows.Input.Key.Delete:
                case System.Windows.Input.Key.Clear:
                case System.Windows.Input.Key.Back:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void GoUp(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void GoDown(object sender, RoutedEventArgs e)
        {
            Value--;
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int outx;
            bool success = int.TryParse(InputBox.Text, out outx);
            if(success)
                Value = outx;
        }
    }
}