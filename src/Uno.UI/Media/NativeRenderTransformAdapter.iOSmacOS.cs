using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI.Xaml.Media.Animation;
using CoreAnimation;
using CoreGraphics;
using Uno.UI.Extensions;

namespace Uno.UI.Media
{
	partial class NativeRenderTransformAdapter
	{
		partial void Initialized()
		{
			// On íOS and MacOS Transform are applied by default on the center on the view
			// so make sure to reset it when the transform is attached to the view
			Apply(isSizeChanged: false, isOriginChanged: true);
		}

		private CATransform3D _transform = CATransform3D.Identity;
		private bool _wasAnimating = false;

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			if (Transform.IsAnimating)
			{
				// While animating the transform, we let the Animator apply the transform by itself, so do not update the Transform
#if __IOS__
				Owner.Layer.AnchorPoint = GPUFloatValueAnimator.GetAnchorForAnimation(Transform, CurrentOrigin, CurrentSize);

				_wasAnimating = true;

				return;
#else
				throw new InvalidOperationException("Should not be 'IsAnimating' on this platform");
#endif
			}
			else if (_wasAnimating)
			{
				// Make sure to fully restore the transform at the end of an animation

				Owner.Layer.AnchorPoint = CurrentOrigin;
				Owner.Layer.Transform = _transform = Transform.MatrixCore.ToTransform3D();

				_wasAnimating = false;

				return;
			}

			if (isSizeChanged)
			{
				// As we use the 'AnchorPoint', the transform matrix is independent of the size of the control
				// But the layouter expects from us that we restore the transform on each measuring phase
				Owner.Layer.Transform = _transform;
			}
			else if (isOriginChanged)
			{
				Owner.Layer.AnchorPoint = CurrentOrigin;
			}
			else
			{
				Owner.Layer.Transform = _transform = Transform.MatrixCore.ToTransform3D();
			}
		}

		partial void Cleanup()
		{
			Owner.Layer.Transform = CATransform3D.Identity;
		}
	}
}
