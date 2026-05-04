using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	[Sample("UIElement")]
	public sealed partial class UIElement_BringIntoView_Nested_ScrollViewers : Page
	{
		public UIElement_BringIntoView_Nested_ScrollViewers()
		{
			this.InitializeComponent();
		}

		private void BringItemIntoView_Click(object sender, RoutedEventArgs e)
		{
			Item.StartBringIntoView();
		}
	}
}
