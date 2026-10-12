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

namespace Uno.UI.RuntimeTests
{
	public sealed partial class When_MenuFlyout_DataContext_Changes_In_Opening : UserControl
	{
		public When_MenuFlyout_DataContext_Changes_In_Opening()
		{
			InitializeComponent();

			btn.DataContext = new DataContextClass() { Text = "1" };
			btn2.DataContext = new DataContextClass() { Text = "2" };
		}

		private void MenuFlyout_Opening(object sender, object e)
		{
			// WinUI parity: a MenuFlyout has no DataContext of its own; the placement target's DataContext is
			// forwarded to the presenter and items when the flyout is shown, so no manual seeding is needed here.
		}

		private class DataContextClass
		{
			public string Text { get; set; }
		}
	}
}
