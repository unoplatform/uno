using System;
using Uno.UI.Views.Controls;
using Uno.UI.Contracts;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
#endif

namespace Uno.UI.Controls
{
	public class ViewControllerTransitioningDeligate : UIViewControllerTransitioningDelegate
	{
		readonly IUIViewControllerAnimatedTransitioning _showTransition;

		readonly IUIViewControllerAnimatedTransitioning _hideTransition;

		public ViewControllerTransitioningDeligate(IUIViewControllerAnimatedTransitioning show, IUIViewControllerAnimatedTransitioning hide)
		{
			this._showTransition = show;
			_hideTransition = hide;
		}
#if XAMARIN_IOS_UNIFIED
		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForPresentedController(UIViewController presented, UIViewController presenting, UIViewController source)
#else
		public override IUIViewControllerAnimatedTransitioning PresentingController(UIViewController presented, UIViewController presenting, UIViewController source)
#endif
		{
			return _showTransition;
		}
		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
		{
			return _hideTransition;
		}
	}
}

