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

namespace UITests.Shared.Windows_UI_Xaml_Controls.MenuFlyoutTests
{
	[Uno.UI.Samples.Controls.SampleControlInfo("Flyouts", nameof(MenuFlyout_IosNative), description: "ios native MenuFlyout")]
	public sealed partial class MenuFlyout_IosNative : UserControl
	{
		public MenuFlyout_IosNative()
		{
			this.InitializeComponent();
		}
	}
}
