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
	public sealed partial class UIElement_BringIntoView_ListView : Page
	{
		private readonly Random _randomizer = new Random();

		public UIElement_BringIntoView_ListView()
		{
			this.InitializeComponent();
		}

		private void BringRandomItemIntoView_Click(object sender, RoutedEventArgs e)
		{
			DependencyObject container;
			do
			{
				var itemIndex = _randomizer.Next(0, List.Items.Count);
				StatusTextBlock.Text = "Brought item " + (itemIndex + 1) + " into view";
				container = List.ContainerFromIndex(itemIndex);
				(container as UIElement)?.StartBringIntoView();
			} while (container is null); // Not all items will be materialized at first
		}
	}
}
