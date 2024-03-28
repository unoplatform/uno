using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation
{
	[Sample("Animations")]
	public sealed partial class FadeInOutThemeAnimationPage : Page
	{
		public FadeInOutThemeAnimationPage()
		{
			this.InitializeComponent();
		}

		private void Fadein(object sender, object args)
		{
			fadein.Begin();
		}
		private void Fadeout(object sender, object args)
		{
			fadeout.Begin();
		}
	}
}
