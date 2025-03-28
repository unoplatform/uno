using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// EntranceThemeTransition : Based on WinRT EntranceThemeTransition
	/// (https://msdn.microsoft.com/en-us/library/windows/apps/windows.ui.xaml.media.animation.entrancethemetransition.Aspx)
	/// </summary>
	public partial class EntranceThemeTransition : Transition
	{
		private readonly Duration _duration = new Duration(TimeSpan.FromMilliseconds(100));

		public EntranceThemeTransition()
		{
			//TODO EASING FUNCTION Task 3086
		}

		#region FromHorizontalOffset Property

		public double FromHorizontalOffset
		{
			get { return (double)this.GetValue(FromHorizontalOffsetProperty); }
			set { this.SetValue(FromHorizontalOffsetProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FromHorizontalOffset.  This enables animation, styling, binding, etc...
		public static DependencyProperty FromHorizontalOffsetProperty { get; } =
			DependencyProperty.Register("FromHorizontalOffset", typeof(double), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(40.0));

		#endregion

		#region FromVerticalOffset Property

		public double FromVerticalOffset
		{
			get { return (double)this.GetValue(FromVerticalOffsetProperty); }
			set { this.SetValue(FromVerticalOffsetProperty, value); }
		}

		// Using a DependencyProperty as the backing store for FromVerticalOffset.  This enables animation, styling, binding, etc...
		public static DependencyProperty FromVerticalOffsetProperty { get; } =
			DependencyProperty.Register("FromVerticalOffset", typeof(double), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(0.0));

		#endregion


		#region IsStaggeringEnabled Property

		public bool IsStaggeringEnabled
		{
			get { return (bool)this.GetValue(IsStaggeringEnabledProperty); }
			set { this.SetValue(IsStaggeringEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsStaggeringEnabled.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsStaggeringEnabledProperty { get; } =
			DependencyProperty.Register("IsStaggeringEnabled", typeof(bool), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(true));

		#endregion


		private void AttachOpacityAnimation(Storyboard storyBoard, IFrameworkElement element, TimeSpan beginTime)
		{
			var startingOpacity = element.Opacity;

			var animation = new DoubleAnimation();
			animation.From = 0;
			animation.To = startingOpacity;
			animation.FillBehavior = FillBehavior.Stop;
			animation.BeginTime = beginTime;

			element.Opacity = 0;
			animation.Completed += (sender, e) =>
			{
				element.Opacity = startingOpacity;
			};


			animation.Duration = TimeSpan.FromMilliseconds(_duration.TimeSpan.TotalMilliseconds / 2); ;

			Storyboard.SetTarget(animation, element);
			Storyboard.SetTargetProperty(animation, "Opacity");

			storyBoard.Children.Add(animation);
		}

		private void AttachHorizontalTranslateAnimation(Storyboard storyBoard, IFrameworkElement element, TimeSpan beginTime)
		{
			if (this.FromHorizontalOffset == 0)
			{
				return;
			}

			var animation = new DoubleAnimation();
			animation.From = FromHorizontalOffset;
			animation.To = 0;
			animation.FillBehavior = FillBehavior.Stop;
			animation.BeginTime = beginTime;

			animation.Duration = _duration;

			Storyboard.SetTarget(animation, element.RenderTransform);
			Storyboard.SetTargetProperty(animation, "X");

			storyBoard.Children.Add(animation);
		}

		private void AttachVerticalTranslateAnimation(Storyboard storyBoard, IFrameworkElement element, TimeSpan beginTime)
		{
			if (this.FromVerticalOffset == 0)
			{
				return;
			}

			var animation = new DoubleAnimation();
			animation.From = FromVerticalOffset;
			animation.To = 0;
			animation.FillBehavior = FillBehavior.Stop;
			animation.BeginTime = beginTime;

			animation.Duration = _duration;

			Storyboard.SetTarget(animation, element.RenderTransform);
			Storyboard.SetTargetProperty(animation, "Y");

			storyBoard.Children.Add(animation);
		}

		internal override void AttachToStoryboardAnimation(Storyboard sb, IFrameworkElement element, TimeSpan beginTime, int xOffset, int yOffset)
		{
			if (!IsStaggeringEnabled)
			{
				beginTime = TimeSpan.Zero;
			}

			base.AttachToStoryboardAnimation(sb, element, beginTime);


			element.RenderTransform = new TranslateTransform();

			AttachHorizontalTranslateAnimation(sb, element, beginTime);
			AttachVerticalTranslateAnimation(sb, element, beginTime);

			AttachOpacityAnimation(sb, element, beginTime);
		}
	}
}

