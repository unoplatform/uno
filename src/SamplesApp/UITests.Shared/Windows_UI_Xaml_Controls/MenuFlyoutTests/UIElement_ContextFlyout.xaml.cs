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
	[Uno.UI.Samples.Controls.SampleControlInfo("Flyouts")]
	public sealed partial class UIElement_ContextFlyout : UserControl
	{
		public UIElement_ContextFlyout()
		{
			this.InitializeComponent();
		}

		public void OnMenuItemClick(object sender, object args)
		{
			if (sender is MenuFlyoutItem item)
			{
				result.Text = "click: " + item.Text;
			}
		}
	}
}
