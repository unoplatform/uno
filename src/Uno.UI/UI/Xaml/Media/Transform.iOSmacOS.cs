using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Media
{
	partial class Transform : GeneralTransform
	{
		// Currently we support only one view par transform.
		// But we can declare a Transform as a static resource and use it on multiple views.
		// Note: This is now used only for animations

		private int _runningAnimations;

		internal virtual bool IsAnimating => _runningAnimations > 0;

		internal void StartAnimation()
		{
			// Set the animation flag which means 'Transform' should no longer be altered and notify change so the RenderTransformAdapter
			// will update the 'AnchorPoint' using current 'RenderOrigin' and current size.
			if (++_runningAnimations == 1)
			{
				// While animating, we disable other properties of the transform
				View.Layer.Transform = CoreAnimation.CATransform3D.Identity;

				NotifyChanged();
			}
		}

		internal void EndAnimation()
		{
			if (--_runningAnimations == 0)
			{
				// First update the result matrix (as updates was ignored due to 'Animations' DP precedence)
				MatrixCore = ToMatrix(new Point(0, 0));

				NotifyChanged();

				// Let the RenderTransformAdapter restore the 'AnchorPoint' to the 'RenderOrigin' and set the updated 'Transform'.
				View.InvalidateArrange();
			}
		}
	}
}


