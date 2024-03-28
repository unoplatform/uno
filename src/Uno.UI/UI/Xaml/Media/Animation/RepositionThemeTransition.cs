using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	public partial class RepositionThemeTransition : Transition
	{
		private readonly Duration _duration = new Duration(TimeSpan.FromMilliseconds(120));

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
