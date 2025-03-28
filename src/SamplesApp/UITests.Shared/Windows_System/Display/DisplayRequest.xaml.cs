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
using UwpDisplayRequest = Windows.System.Display.DisplayRequest;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_System.Display
{
	[SampleControlInfo("Windows.System", "Display.DisplayRequest",
		description: "Demonstrates DisplayRequest class to keep screen awake. Make sure to test without debugger attached (that keeps screen on all the time).")]
	public sealed partial class DisplayRequest : UserControl
	{
		private static int _currentlyActive = 0;
		private static UwpDisplayRequest _displayRequest = new UwpDisplayRequest();

		public DisplayRequest()
		{
			this.InitializeComponent();
		}

		private void RequestActive_Click(object sender, RoutedEventArgs e)
		{
			_displayRequest.RequestActive();
			ActiveRequestCounter.Text = $"Currently active {(++_currentlyActive).ToString()} request";

		}

		private void RequestRelease_Click(object sender, RoutedEventArgs e)
		{
			_displayRequest.RequestRelease();
			ActiveRequestCounter.Text = $"Currently active {(--_currentlyActive).ToString()} request";
		}
	}
}
