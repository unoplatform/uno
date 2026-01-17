using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.MenuFlyoutTests
{
	[Uno.UI.Samples.Controls.Sample("Flyouts", nameof(MenuFlyout_DroidAltTab), Description: SampleDescription, IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class MenuFlyout_DroidAltTab : UserControl
	{
		private const string SampleDescription = "[ManualTest]: tap 'Here' to open popup, switch app/go to home and come back, tap `Here` and popup should still work.";

		public MenuFlyout_DroidAltTab()
		{
			this.InitializeComponent();
		}
	}
}
