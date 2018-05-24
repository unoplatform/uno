using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UIKit;
using Windows.UI.Xaml;

namespace Uno.UI.Controls
{
    public partial class BindableUISlider : UISlider, DependencyObject, INotifyPropertyChanged
    {
        public BindableUISlider()
        {
            this.ValueChanged += (s, e) =>
            {
                SetBindingValue(Value, nameof(Value));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            };
        }

        public override float Value
        {
            get { return base.Value; }
            set
            {
                base.Value = value;
                
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
