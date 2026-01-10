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

namespace UITests.Shared.Windows_UI_Xaml_Controls.MenuFlyoutTests
{
	[Uno.UI.Samples.Controls.Sample("Flyouts", "MenuFlyoutItem_Hierarchy")]
	public sealed partial class MenuFlyoutItem_Hierarchy : UserControl
	{
		public MenuFlyoutItem_Hierarchy()
		{
			this.InitializeComponent();
		}

		public void OnItemClicked(object sender, object args)
		{
			if (sender is MenuFlyoutItem item)
			{
				results.Text = item.Text;
			}
		}
	}
}
