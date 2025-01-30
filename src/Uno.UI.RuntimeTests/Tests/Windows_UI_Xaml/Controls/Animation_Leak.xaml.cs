using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class Animation_Leak : Page
	{
		public Animation_Leak()
		{
			InitializeComponent();

			// Launch a fade-out of the rectangle
			var storyboard = new Storyboard();
			var animation = new DoubleAnimation
			{
				To = 0,
				Duration = new Duration(TimeSpan.FromSeconds(0.15)),
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
			};

			Storyboard.SetTarget(animation, sut);
			Storyboard.SetTargetProperty(animation, "Opacity");

			storyboard.Children.Add(animation);
			storyboard.Begin();
		}
	}
}
