using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Shared.Windows_UI_Xaml_Controls.BitmapIconTests
{
	[Sample("Icons")]
	public sealed partial class BitmapIcon_Generic : UserControl
	{
		public BitmapIcon_Generic()
		{
			this.InitializeComponent();
		}

		private void OnClick(object sender, object args)
		{
			icon1.Foreground = new SolidColorBrush(Colors.Yellow);
			icon2.Foreground = new SolidColorBrush(Colors.Green);
		}
	}
}
