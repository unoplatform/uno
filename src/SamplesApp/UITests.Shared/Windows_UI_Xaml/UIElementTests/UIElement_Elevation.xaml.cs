using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo("UIElement")]
	public sealed partial class UIElement_Elevation : Page
	{
		public UIElement_Elevation()
		{
			this.InitializeComponent();
		}

		private void SetElevationClick(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag != null)
			{
				elevation1.Value = Convert.ToDouble(btn.Tag);
			}
		}
	}
}
