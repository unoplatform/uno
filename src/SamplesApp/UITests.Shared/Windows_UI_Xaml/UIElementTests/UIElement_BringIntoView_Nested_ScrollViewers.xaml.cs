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
