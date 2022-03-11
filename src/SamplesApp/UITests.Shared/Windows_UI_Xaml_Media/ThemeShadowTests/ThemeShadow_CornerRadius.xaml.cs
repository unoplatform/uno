using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Shapes;

namespace UITests.Windows_UI_Xaml_Media.ThemeShadowTests
{
	[Sample("Windows.UI.Xaml.Media")]
    public sealed partial class ThemeShadow_CornerRadius : Page
    {
		public ThemeShadow_CornerRadius()
		{
			InitializeComponent();

			SharedShadow.Receivers.Add(BackgroundGrid);
		}
	}
}
