using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.BitmapIconTests
{
	[Sample("Icons", Description = "This sample showcases BitmapIcon "
		+ "in different conditions with regards to ForegroundProperty and ShowAsMonochromeColor. "
		+ "Additionally, turn dark mode on and off to see the rendering in different themes.")]
	public sealed partial class BitmapIcon_Foreground : UserControl
	{
		public BitmapIcon_Foreground()
		{
			this.InitializeComponent();
		}
	}
}
