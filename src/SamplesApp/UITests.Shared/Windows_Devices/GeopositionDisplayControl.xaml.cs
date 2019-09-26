using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Devices
{
    public sealed partial class GeopositionDisplayControl : UserControl
    {
        public GeopositionDisplayControl()
        {
            this.InitializeComponent();
        }
			   
		public Geoposition Geoposition
		{
			get { return (Geoposition)GetValue(GeopositionProperty); }
			set { SetValue(GeopositionProperty, value); }
		}

		public static readonly DependencyProperty GeopositionProperty =
			DependencyProperty.Register(nameof(Geoposition), typeof(Geoposition), typeof(GeopositionDisplayControl), new PropertyMetadata(null));
	}
}
