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
	[Uno.UI.Samples.Controls.Sample("Flyouts", "MenuFlyoutItem_Click", Description: "Testing click on MenuFlyoutItem")]
	public sealed partial class MenuFlyoutItem_Click : UserControl
	{
		public MenuFlyoutItem_Click()
		{
			this.InitializeComponent();
		}

		public void FlyoutItem_Click(object sender, object args)
		{
			mfiResult.Text = "success";
		}
	}
}
