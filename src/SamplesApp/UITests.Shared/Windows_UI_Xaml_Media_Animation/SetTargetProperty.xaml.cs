using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Xaml_Media_Animation
{
	[Sample("Animations")]
	public sealed partial class SetTargetProperty : Page
	{
		private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(1000);

		private TranslateTransform _translateTransform;
		private TranslateTransform _translateTransform2;

		private Storyboard _storyboard = new Storyboard();
		private DoubleAnimation _translateAnimation;

		public SetTargetProperty()
		{
			this.InitializeComponent();
			this.Loaded += (s, e) => Setup();
		}

		private void Setup()
		{
			AnimatedRect.RenderTransform = _translateTransform = new TranslateTransform();
			AnimatedRect2.RenderTransform = _translateTransform2 = new TranslateTransform();

			_translateAnimation = new DoubleAnimation()
			{
				Duration = new Duration(AnimationDuration),
			};
			Storyboard.SetTarget(_translateAnimation, _translateTransform);
			UpdateTranslateAnimationTargetProperty();
			_storyboard.Children.Add(_translateAnimation);
		}

		private void OnDirectionOrRectChanged()
		{
			var playing = StopRunningAnimation();
			UpdateTranslateAnimationTargetProperty();
			if (playing)
			{
				PlayAnimation();
			}
		}

		private void UpdateTranslateAnimationTargetProperty()
		{
			if (_translateAnimation == null) return;

			var property = IsDirectionHorizontal() ? nameof(_translateTransform.X) : nameof(_translateTransform.Y);
			Storyboard.SetTargetProperty(_translateAnimation, property);
			Storyboard.SetTarget(_translateAnimation, AnimatedRectSwitch.IsOn ? _translateTransform2 : _translateTransform);
		}


		private void PlayAnimation()
		{
			var total = IsDirectionHorizontal() ? Container.ActualSize.X : Container.ActualSize.Y;
			var occupied = IsDirectionHorizontal() ? AnimatedRect.Width : AnimatedRect.Height;
			var length = total - occupied;

			_translateAnimation.From = 0;
			_translateAnimation.To = length;

			AnimationState.Text = "Started..";
			_storyboard.Begin();
			_storyboard.Completed += (sender, e) => AnimationState.Text = "Completed!";
		}

		private bool StopRunningAnimation()
		{
			if (_storyboard.GetCurrentState() != ClockState.Stopped)
			{
				// we want to Pause() the animation midway to avoid the jarring feeling
				// but since paused state will still yield ClockState.Active
				// we have to actually use Stop() in order to differentiate

				// pause & snapshot the animated values in the middle of animation
				_storyboard.Pause();
				var offset = TranslateOffset;

				// restore the values after stopping it
				_storyboard.Stop();
				TranslateOffset = offset;

				return true;
			}

			return false;
		}

		// helpers
		private bool IsDirectionHorizontal() => !IsDirectionHorizontalToggle.IsOn;
		private bool IsCurrentAnimationDirectionHorizontal() => Storyboard.GetTargetProperty(_translateAnimation) switch
		{
			nameof(_translateTransform.X) => true,
			nameof(_translateTransform.Y) => false,

			_ => throw new ArgumentOutOfRangeException(nameof(Storyboard.TargetPropertyProperty)),
		};

		private double TranslateOffset
		{
			get => IsCurrentAnimationDirectionHorizontal() ? _translateTransform.X : _translateTransform.Y;
			set
			{
				if (IsCurrentAnimationDirectionHorizontal()) _translateTransform.X = value;
				else _translateTransform.Y = value;
			}
		}
	}
}
