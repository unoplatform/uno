using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Media.ThemeShadowTests
{
	[Sample("Microsoft.UI.Xaml.Media")]
	public sealed partial class ThemeShadow_CornerRadius : Page
	{
		public ThemeShadow_CornerRadius()
		{
			InitializeComponent();

			SharedShadow.Receivers.Add(BackgroundGrid);
		}
	}
}
