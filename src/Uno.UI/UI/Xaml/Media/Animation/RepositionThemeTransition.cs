using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class RepositionThemeTransition : Transition
	{
		private readonly Duration _duration = new Duration(TimeSpan.FromMilliseconds(120));

		// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d
		internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger)
			=> transitionTrigger is TransitionTrigger.Layout or TransitionTrigger.Reparent;

		internal override void CreateStoryboard(
			UIElement element,
			global::Windows.Foundation.Rect start,
			global::Windows.Foundation.Rect destination,
			TransitionTrigger transitionTrigger,
			IList<Storyboard> storyboards,
			out TransitionParent parentForTransition)
		{
			global::Windows.Foundation.Point sourceoffset = new(start.X - destination.X, start.Y - destination.Y);
			global::Windows.Foundation.Point destoffset = new(0, 0);

			Storyboard spSB = new();

			ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_REPOSITION, ThemeGenerator.TA_REPOSITION_TARGET, element, false, sourceoffset, destoffset, 0, spSB.Children);

			storyboards.Add(spSB);

			parentForTransition = transitionTrigger == TransitionTrigger.Reparent ? TransitionParent.ParentToRoot : TransitionParent.ParentToCommonParent;
		}

		internal override bool GetIsStaggeringEnabled() => IsStaggeringEnabled;

		// ThemeGenerator::GetStaggerFunction(TAS_REPOSITION, TA_REPOSITION_TARGET), PVL values dumped from Windows build 29698.
		internal override bool GetStaggerFunction(out double delay, out double delayReduce)
		{
			delay = 33;
			delayReduce = 1;
			return true;
		}

		internal override void AttachToStoryboardAnimation(Storyboard sb, IFrameworkElement element, TimeSpan beginTime, int xOffset, int yOffset)
		{

			element.RenderTransform = new TranslateTransform()
			{
				X = xOffset,
				Y = yOffset,
			};

			AttachHorizontalTranslateAnimation(sb, element, beginTime, xOffset);

			AttachVerticalTranslateAnimation(sb, element, beginTime, yOffset);

		}

		private void AttachHorizontalTranslateAnimation(Storyboard storyBoard, IFrameworkElement element, TimeSpan beginTime, int xOffset)
		{
			if (xOffset == 0)
			{
				return;
			}

			var animation = new DoubleAnimation();
			animation.From = xOffset;
			animation.To = 0;
			animation.FillBehavior = FillBehavior.HoldEnd;
			animation.BeginTime = beginTime;

			animation.Duration = _duration;

			Storyboard.SetTarget(animation, element.RenderTransform);
			Storyboard.SetTargetProperty(animation, "X");

			storyBoard.Children.Add(animation);
		}

		private void AttachVerticalTranslateAnimation(Storyboard storyBoard, IFrameworkElement element, TimeSpan beginTime, int yOffset)
		{
			if (yOffset == 0)
			{
				return;
			}

			var animation = new DoubleAnimation();
			animation.From = yOffset;
			animation.To = 0;
			animation.FillBehavior = FillBehavior.HoldEnd;
			animation.BeginTime = beginTime;

			animation.Duration = _duration;

			Storyboard.SetTarget(animation, element.RenderTransform);
			Storyboard.SetTargetProperty(animation, "Y");

			storyBoard.Children.Add(animation);
		}
	}
}
