using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation
{
	[Sample("Animations")]
	public sealed partial class ColorAnimationUsingKeyFrames_Fill : Page
	{
		public ColorAnimationUsingKeyFrames_Fill()
		{
			this.InitializeComponent();
		}

		private void GoToState(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement o)
			{
				var state = o.Tag as string;
				Console.WriteLine($"Activating state {state}");
				VisualStateManager.GoToState(this, state, true);
			}
		}
	}
}
