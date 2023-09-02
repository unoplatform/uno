using System;
using Uno.UI.Views.Controls;
using Uno.UI.Contracts;

using Foundation;
using UIKit;

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

		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForPresentedController(UIViewController presented, UIViewController presenting, UIViewController source)
		{
			return _showTransition;
		}

		public override IUIViewControllerAnimatedTransitioning GetAnimationControllerForDismissedController(UIViewController dismissed)
		{
			return _hideTransition;
		}
	}
}

