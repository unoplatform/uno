using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace UITests.Windows_UI_Xaml_Controls.MenuFlyoutTests
{
	[Uno.UI.Samples.Controls.SampleControlInfo("Flyouts", nameof(MenuFlyout_DroidAltTab), description: SampleDescription, IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class MenuFlyout_DroidAltTab : UserControl
	{
		private const string SampleDescription = "[ManualTest]: tap 'Here' to open popup, switch app/go to home and come back, tap `Here` and popup should still work.";

		public MenuFlyout_DroidAltTab()
		{
			this.InitializeComponent();
		}
	}
}
