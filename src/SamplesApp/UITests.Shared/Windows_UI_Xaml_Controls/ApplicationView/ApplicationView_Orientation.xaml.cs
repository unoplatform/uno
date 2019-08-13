using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ApplicationView
{
	[SampleControlInfo("ApplicationView", "ApplicationView_Orientation")]
	public sealed partial class ApplicationView_Orientation : UserControl
	{
		public ApplicationView_Orientation()
		{
			this.InitializeComponent();

			Windows.UI.Xaml.Window.Current.SizeChanged += (s, e) => UpdateOrientation();
			UpdateOrientation();
		}

		private void UpdateOrientation()
		{
			this.Orientation.Text = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Orientation.ToString();
		}
	}
}
