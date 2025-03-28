using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Private.Infrastructure;

namespace UITests.Windows_UI_Xaml_Media_Animation
{
	[Sample("Animations", Name = "DoubleAnimation opacity final state", Description = _description)]
	public sealed partial class DoubleAnimation_FinalState_Opacity : Page
	{
		private const string _description = @"This (automated) test validates the final state of when animating opacity using a double animation.

Expected result:
  * Completed: stays at 0 if HOLD, goes back to 1 if STOP
  * Paused: stays half visible no matter the fill behavior
  * Canceled: goes back to 1 no matter the fill behavior

If 'Set From' was selected, then rollback means back to value before animation, not the 'From' value!";

		private TimeSpan _duration;

		public DoubleAnimation_FinalState_Opacity()
		{
			this.InitializeComponent();

#if DEBUG
			_duration = TimeSpan.FromSeconds(2);
#else
			_duration = TimeSpan.FromMilliseconds(400);
#endif
		}

		private async void StartAnimations(object sender, RoutedEventArgs e)
		{
			var toLetComplete = new[]
			{
				CreateAnimation(CompletedAnimationHost_Hold, FillBehavior.HoldEnd),
				CreateAnimation(CompletedAnimationHost_Stop, FillBehavior.Stop)
			};
			var toPause = new[]
			{
				CreateAnimation(PausedAnimationHost_Hold, FillBehavior.HoldEnd),
				CreateAnimation(PausedAnimationHost_Stop, FillBehavior.Stop)
			};
			var toCancel = new[]
			{
				CreateAnimation(CanceledAnimationHost_Hold, FillBehavior.HoldEnd),
				CreateAnimation(CanceledAnimationHost_Stop, FillBehavior.Stop)
			};

			Status.Text = "Animating";
			await UnitTestDispatcherCompat.From(this).RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				async () =>
				{
					var halfAnimation = (int)_duration.TotalMilliseconds / 2;
					await Task.Delay(halfAnimation);
					toPause.ForEach(s => s.Pause());
					toCancel.ForEach(s => s.Stop());

					await Task.Delay((int)(halfAnimation * 1.2));
					Status.Text = "Completed";
				});
			toLetComplete.Concat(toPause).Concat(toCancel).ForEach(s => s.Begin());
		}

		private Storyboard CreateAnimation(UIElement target, FillBehavior fill)
		{
			var animation = new DoubleAnimation
			{
				To = 0,
				Duration = new Duration(_duration),
				FillBehavior = fill
			};

			if (UseFromValue.IsChecked.GetValueOrDefault())
			{
				animation.From = .75;
			}

			Storyboard.SetTarget(animation, target);
			Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));

			return new Storyboard
			{
				Children = { animation }
			};
		}
	}
}
