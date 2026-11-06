using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Media.Animation
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
			set { this.SetValue(FromHorizontalOffsetProperty, Boxer.Box(value)); }
		}

		// Using a DependencyProperty as the backing store for FromHorizontalOffset.  This enables animation, styling, binding, etc...
		public static DependencyProperty FromHorizontalOffsetProperty { get; } =
			DependencyProperty.Register("FromHorizontalOffset", typeof(double), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(40.0));

		#endregion

		#region FromVerticalOffset Property

		public double FromVerticalOffset
		{
			get { return (double)this.GetValue(FromVerticalOffsetProperty); }
			set { this.SetValue(FromVerticalOffsetProperty, Boxer.Box(value)); }
		}

		// Using a DependencyProperty as the backing store for FromVerticalOffset.  This enables animation, styling, binding, etc...
		public static DependencyProperty FromVerticalOffsetProperty { get; } =
			DependencyProperty.Register("FromVerticalOffset", typeof(double), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(DoubleBoxes.Zero));

		#endregion


		#region IsStaggeringEnabled Property

		public bool IsStaggeringEnabled
		{
			get { return (bool)this.GetValue(IsStaggeringEnabledProperty); }
			set { this.SetValue(IsStaggeringEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsStaggeringEnabled.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsStaggeringEnabledProperty { get; } =
			DependencyProperty.Register("IsStaggeringEnabled", typeof(bool), typeof(EntranceThemeTransition), new FrameworkPropertyMetadata(BoolBoxes.True));

		#endregion


		// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d
		internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger)
			=> transitionTrigger == TransitionTrigger.Load;

		internal override void CreateStoryboard(
			UIElement element,
			global::Windows.Foundation.Rect start,
			global::Windows.Foundation.Rect destination,
			TransitionTrigger transitionTrigger,
			IList<Storyboard> storyboards,
			out TransitionParent parentForTransition)
		{
			parentForTransition = TransitionParent.ParentToCommonParent;

			global::Windows.Foundation.Point sourceoffset = new(FromHorizontalOffset, FromVerticalOffset);
			global::Windows.Foundation.Point destoffset = new(0, 0);

			var shouldRun = transitionTrigger == TransitionTrigger.Load;
			var didCheck = false;
			var context = global::DirectUI.ThemeTransitionContext.None;

			if (shouldRun)
			{
				GetTransitionContext(element, out didCheck, out context);
			}
			if (didCheck)
			{
				shouldRun &= context == global::DirectUI.ThemeTransitionContext.Entrance;
			}
			else
			{
				// if there was no context, we will only react to a true xaml::TransitionTrigger_Load
				shouldRun = transitionTrigger == TransitionTrigger.Load;
			}

			if (!shouldRun)
			{
				return;
			}

			Storyboard spSB = new();

			ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_ENTERPAGE, ThemeGenerator.TA_ENTERPAGE_TARGET, element, false, sourceoffset, destoffset, 0, spSB.Children);

			storyboards.Add(spSB);

			SetTransitionParentForStickyHeaders(element, ref parentForTransition);
		}

		internal override bool GetIsStaggeringEnabled() => IsStaggeringEnabled;

		// ThemeGenerator::GetStaggerFunction(TAS_ENTERPAGE, TA_ENTERPAGE_TARGET), PVL values dumped from Windows build 29698.
		internal override bool GetStaggerFunction(out double delay, out double delayReduce)
		{
			delay = 35;
			delayReduce = 1;
			return true;
		}

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

			Storyboard.SetTarget(animation, (DependencyObject)element);
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

