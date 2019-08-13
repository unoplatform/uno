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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.UIElement
{
	[SampleControlInfo("UIElement", "UIElement_BringIntoView")]
	public sealed partial class UIElement_BringIntoView : UserControl
	{
		public UIElement_BringIntoView()
		{
			this.InitializeComponent();

			ComboBox.ItemsSource = new[]
			{
				"TextBlock",
				"TextBox",
				"Button",
				"Border",
				"Grid",
				"Rectangle",
				"Image",
				"ToggleSwitch"
			};
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.AddedItems
				.OfType<string>()
				.Select(FindName)
				.OfType<Windows.UI.Xaml.UIElement>()
				.FirstOrDefault()?
#if XAMARIN
				.StartBringIntoView();
#else
			.ToString();
#endif
		}
	}
}
