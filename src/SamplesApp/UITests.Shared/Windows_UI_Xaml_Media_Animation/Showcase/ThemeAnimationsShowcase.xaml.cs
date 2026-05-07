using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "FadeInThemeAnimation, FadeOutThemeAnimation and RepositionThemeAnimation triggered from buttons.")]
public sealed partial class ThemeAnimationsShowcase : Page
{
	public ThemeAnimationsShowcase()
	{
		this.InitializeComponent();
	}

	private void DoFadeIn(object sender, RoutedEventArgs e)
	{
		FadeTarget.Opacity = 0;
		FadeInStoryboard.Begin();
	}

	private void DoFadeOut(object sender, RoutedEventArgs e)
	{
		FadeOutStoryboard.Begin();
	}

	private void DoReposition(object sender, RoutedEventArgs e)
	{
		RepositionStoryboard.Begin();
	}
}
