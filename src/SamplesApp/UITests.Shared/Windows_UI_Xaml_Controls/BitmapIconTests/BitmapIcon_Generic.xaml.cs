using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.BitmapIconTests
{
	[SampleControlInfo("BitmapIcon", nameof(BitmapIcon_Generic))]
	public sealed partial class BitmapIcon_Generic : UserControl
    {
        public BitmapIcon_Generic()
        {
            this.InitializeComponent();
        }

		private void OnClick(object sender, object args)
		{
			icon1.Foreground = new SolidColorBrush(Colors.Yellow);
			icon2.Foreground = new SolidColorBrush(Colors.Green);
		}
	}
}
