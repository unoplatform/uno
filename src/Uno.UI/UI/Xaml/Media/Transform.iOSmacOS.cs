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

		internal bool IsAnimating { get; private set; }

		internal void StartAnimation()
		{
			// While animating, we disable other properties of the transform
			View.Layer.Transform = CoreAnimation.CATransform3D.Identity;

			// Set the animation flag which means 'Transform' should no longer be altered and notify change so the RenderTransformAdapter
			// will update the 'AnchorPoint' using current 'RenderOrigin' and current size.
			IsAnimating = true;
			NotifyChanged(); 
		}

		internal void EndAnimation()
		{
			// First update the result matrix (as updates was ignored due to 'Animations' DP precedence)
			MatrixCore = ToMatrix(new Point(0, 0));

			// Then remove animation flag and notify change so the RenderTransformAdapter
			// will restore the 'AnchorPoint' to the 'RenderOrigin' and set the updated 'Transform'.
			IsAnimating = false;
			NotifyChanged();
			View.InvalidateArrange();
		}
	}
}


