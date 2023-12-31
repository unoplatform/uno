using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Media.ThemeShadowTests
{
	[Sample("Microsoft.UI.Xaml.Media")]
	public sealed partial class ThemeShadow_Basic : Page
	{
		public ThemeShadow_Basic()
		{
			InitializeComponent();

			SharedShadow.Receivers.Add(BackgroundGrid);
		}
	}
}
