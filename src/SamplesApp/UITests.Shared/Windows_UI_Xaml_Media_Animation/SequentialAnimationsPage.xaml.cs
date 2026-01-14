using System;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace SamplesApp.Windows_UI_Xaml_Media.Animation
{
	[Sample("Animations", "Sequential Animations", IgnoreInSnapshotTests: true)]
	public sealed partial class SequentialAnimationsPage : Page
	{
		private readonly TimeSpan DURATION = TimeSpan.FromMilliseconds(250);

		public SequentialAnimationsPage()
		{
			this.InitializeComponent();

			// Simply kick-off the task
#pragma warning disable CS4014
			this.Loaded += (sender, e) => StartAnimations();
#pragma warning restore CS4014
		}

		internal void TranslateX(UIElement element, double to)
			=> Animate(element, "(UIElement.RenderTransform).(TranslateTransform.X)", to);

		internal void TranslateY(UIElement element, double to)
			=> Animate(element, "(UIElement.RenderTransform).(TranslateTransform.Y)", to);

		internal
#if __ANDROID__
		new
#endif
		void ScaleX(UIElement element, double to)
			=> Animate(element, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)", to);

		internal
#if __ANDROID__
		new
#endif
		void ScaleY(UIElement element, double to)
			=> Animate(element, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)", to);

		internal void Rotate(UIElement element, double to)
			=> Animate(element, "(UIElement.RenderTransform).(RotateTransform.Angle)", to);

		internal void Fade(UIElement element, double to)
			=> Animate(element, "Opacity", to);

		private void Animate(UIElement element, string targetProperty, double to)
		{
			var anim = new DoubleAnimation()
			{
				To = to,
				Duration = new Duration(DURATION),
				EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut }
			};

			var storyboard = new Storyboard();

			Storyboard.SetTarget(anim, element);
			Storyboard.SetTargetProperty(anim, targetProperty);

			storyboard.Children.Add(anim);
			storyboard.Begin();
		}

		private async Task StartAnimations()
		{
			Fade(RotateRectangle, 0);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 1";

			Fade(RotateRectangle, 1);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 2";

			TranslateX(TranslateRectangle, 100);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 3";

			TranslateY(TranslateRectangle, 100);
			Rotate(RotateRectangle, 360);
			ScaleX(ScaleRectangle, 1.5);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 4";

			TranslateX(TranslateRectangle, -100);
			ScaleY(ScaleRectangle, 1.5);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 5";

			TranslateY(TranslateRectangle, 0);
			Rotate(RotateRectangle, -360);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 6";

			TranslateX(TranslateRectangle, 0);
			await Task.Delay(DURATION);

			ToggleStep.IsChecked = true;
			ToggleStep.IsChecked = false;
			ToggleStep.Content = "STEP 7";
		}
	}
}
