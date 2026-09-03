using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Single-Storyboard composition: sequential cascade via BeginTime, plus a multi-property orchestration that layers slide, color, pop and spin.")]
public sealed partial class StoryboardCompositionShowcase : Page
{
	public StoryboardCompositionShowcase()
	{
		this.InitializeComponent();
	}

	private void RunChoreography(object sender, RoutedEventArgs e)
	{
		ChoreoStep1Transform.X = 0;
		ChoreoStep2Transform.X = 0;
		ChoreoStep3Transform.X = 0;
		ChoreographyStoryboard.Begin();
	}

	private void RunOrchestrated(object sender, RoutedEventArgs e)
	{
		OrchestratedTransform.TranslateX = 0;
		OrchestratedTransform.ScaleX = 1;
		OrchestratedTransform.ScaleY = 1;
		OrchestratedTransform.Rotation = 0;
		OrchestratedStoryboard.Begin();
	}
}
