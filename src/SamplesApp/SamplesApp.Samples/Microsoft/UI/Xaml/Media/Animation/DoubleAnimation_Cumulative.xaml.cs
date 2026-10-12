using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[Sample("Animations", Name = "DoubleAnimation_Cumulative")]
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
