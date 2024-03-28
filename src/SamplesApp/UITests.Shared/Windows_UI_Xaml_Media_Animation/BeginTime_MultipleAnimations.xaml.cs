using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation
{
	[Sample(
		"Animations",
		IsManualTest = true,
		Description =
			"When running the animation, the 2 squares should end at the same X except the blue will start after. "
			+ "Both must go smoothly to their end location without any jump.")]
	public sealed partial class BeginTime_MultipleAnimations : Page
	{
		public BeginTime_MultipleAnimations()
		{
			this.InitializeComponent();
		}

		private void StartAnimation(object sender, RoutedEventArgs e)
		{
			var redAnimation = new DoubleAnimation
			{
				From = 0,
				To = 500,
				Duration = new Duration(TimeSpan.FromSeconds(10))
			};
			Storyboard.SetTarget(redAnimation, TheRedSquareTransform);
			Storyboard.SetTargetProperty(redAnimation, nameof(TheRedSquareTransform.X));

			var blueAnimation = new DoubleAnimation
			{
				From = 0,
				To = 500,
				Duration = new Duration(TimeSpan.FromSeconds(10)),
				BeginTime = TimeSpan.FromSeconds(5)
			};
			Storyboard.SetTarget(blueAnimation, TheBlueSquareTransform);
			Storyboard.SetTargetProperty(blueAnimation, nameof(TheBlueSquareTransform.X));

			new Storyboard { Children = { redAnimation, blueAnimation } }.Begin();
		}
	}
}
