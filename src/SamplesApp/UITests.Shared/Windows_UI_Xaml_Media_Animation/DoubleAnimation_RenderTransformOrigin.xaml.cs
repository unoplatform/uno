using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace GenericApp.Views.Content.UITests.Animations
{
	[Sample("Animations", Name = "DoubleAnimation_RenderTransformOrigin")]
	public sealed partial class DoubleAnimation_RenderTransformOrigin : UserControl
	{
		public DoubleAnimation_RenderTransformOrigin()
		{
			this.InitializeComponent();
		}

		private void BeginAnimation(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement elt
				&& elt.Resources.TryGetValue("Storyboard", out var res)
				&& res is Storyboard storyboard)
			{
				Storyboard.SetTarget(storyboard, elt);
				storyboard.Begin();
			}
		}

		/*private void BeginAnimation(object sender, RoutedEventArgs e)
		{
			Console.WriteLine("*************************** PATH LOADED");


			var anim = new  DoubleAnimation
			{
				RepeatBehavior = RepeatBehavior.Forever,
				From = 0,
				To = 360,
				Duration = new Duration(new TimeSpan(0, 0, 2)),
			};
			var storyboard = new Storyboard
			{
				Children = {anim}
			};


			//if (!(sender is FrameworkElement elt))
			//{
			//	Console.WriteLine($"*************************** SENDER {sender} IS NOT FRAMEWORK ELEMENT");
			//	return;
			//}

			//if (!elt.Resources.TryGetValue("Storyboard", out var res))
			//{
			//	Console.WriteLine($"*************************** CANNOT FIND RESOURCE");
			//	return;
			//}

			//if (!(res is Storyboard storyboard))
			//{
			//	Console.WriteLine($"*************************** RESOURCE {res} is not a storyboard");
			//	return;
			//}


			Storyboard.SetTargetProperty(anim, nameof(CompositeTransform.Rotation));
			Storyboard.SetTarget(anim, ((FrameworkElement)sender).RenderTransform);
			storyboard.Begin();

			Console.WriteLine($"*************************** ANIMATION STARTED");
		}*/
	}
}
