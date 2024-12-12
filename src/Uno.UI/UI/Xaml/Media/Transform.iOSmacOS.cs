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
				// We don't use the NotifyChanged() since it will filters out changes when IsAnimating
				// Note: we also bypass the MatrixCore update which is actually irrelevant until animation completes.
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}

		internal void EndAnimation()
		{
			if (--_runningAnimations == 0)
			{
				// Notify a change so the result matrix will be updated (as updates were ignored due to 'Animations' DP precedence),
				// and the NativeRenderTransformAdapter will then apply this final matrix.
				NotifyChanged();

			}
		}
	}
}

