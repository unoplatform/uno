using System.Numerics;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Media.ThemeShadowTests
{
	[Sample("Windows.UI.Xaml.Media")]
	public sealed partial class ThemeShadow_Overlap : Page
	{
		public ThemeShadow_Overlap()
		{
			InitializeComponent();

			this.Loaded += ThemeShadow_Overlap_Loaded;
		}

		private void ThemeShadow_Overlap_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			SharedShadow.Receivers.Add(BackgroundGrid);
			SharedShadow.Receivers.Add(Rectangle1);
			Rectangle1.Translation += new Vector3(0, 0, 16);
			Rectangle2.Translation += new Vector3(0, 0, 32);
		}
	}
}
