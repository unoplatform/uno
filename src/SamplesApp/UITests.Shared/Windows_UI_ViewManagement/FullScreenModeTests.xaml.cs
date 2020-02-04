using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[SampleControlInfo("Windows.UI.ViewManagement", "FullScreenMode", description: "Showcases entering/exiting full screen mode.")]
	public sealed partial class FullScreenModeTests : UserControl
    {
        public FullScreenModeTests()
        {
            this.InitializeComponent();
        }

		public void EnterFullScreen_Click(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
		}

		public void ExitFullScreen_Click(object sender, RoutedEventArgs e)
		{
			ApplicationView.GetForCurrentView().ExitFullScreenMode();
		}
	}
}
