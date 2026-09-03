using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Polished real-world micro-animations: Aurora gradient, pulse dot, loading-dot cascade, indeterminate progress, hover card.")]
public sealed partial class AnimationGalleryShowcase : Page
{
	public AnimationGalleryShowcase()
	{
		this.InitializeComponent();
		Loaded += (_, _) => StartAll(this, null);
		Unloaded += (_, _) => StopAll(this, null);
	}

	private void StartAll(object sender, RoutedEventArgs e)
	{
		PulseStoryboard.Begin();
		ProgressStoryboard.Begin();
		AuroraStoryboard.Begin();
		DotsStoryboard.Begin();
	}

	private void StopAll(object sender, RoutedEventArgs e)
	{
		PulseStoryboard.Stop();
		ProgressStoryboard.Stop();
		AuroraStoryboard.Stop();
		DotsStoryboard.Stop();
	}

	private void OnCardEntered(object sender, PointerRoutedEventArgs e)
	{
		CardLeaveStoryboard.Stop();
		CardEnterStoryboard.Begin();
	}

	private void OnCardExited(object sender, PointerRoutedEventArgs e)
	{
		CardEnterStoryboard.Stop();
		CardLeaveStoryboard.Begin();
	}
}
