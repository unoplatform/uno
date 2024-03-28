using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[SampleControlInfo("Animations", "DoubleAnimation_Cumulative")]
	public sealed partial class DoubleAnimation_Cumulative : UserControl
	{
		public DoubleAnimation_Cumulative()
		{
			this.InitializeComponent();
		}

		private void LaunchAnimation1(object sender, TappedRoutedEventArgs e)
		{
			var animation = new DoubleAnimation
			{
				To = 50,
				Duration = new Duration(TimeSpan.FromSeconds(10))
			};
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.Y));
			Storyboard.SetTarget(animation, _transform);

			new Storyboard
			{
				Children = { animation }
			}.Begin();
		}

		private void LaunchAnimation2(object sender, TappedRoutedEventArgs e)
		{
			var animation = new DoubleAnimation
			{
				To = 0,
				Duration = new Duration(TimeSpan.FromSeconds(10))
			};
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.Y));
			Storyboard.SetTarget(animation, _transform);

			new Storyboard
			{
				Children = { animation }
			}.Begin();
		}
	}
}
