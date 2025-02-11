using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using CoreAnimation;
using CoreGraphics;
using Uno.Disposables;
using Uno.UI.Extensions;

namespace Uno.UI.Media
{
	partial class NativeRenderTransformAdapter
	{
		private CATransform3D _transform = CATransform3D.Identity;
		private bool _wasAnimating;

		partial void Initialized()
		{
			// On íOS and MacOS Transform are applied by default on the center on the view
			// so make sure to reset it when the transform is attached to the view
			InitializeOrigin();

			// Apply the transform as soon as its been declared
			Update();
		}

		private void InitializeOrigin()
		{
			var layer = Owner.Layer;
			var oldFrame = layer.Frame;

			layer.AnchorPoint = CurrentOrigin;

			// Restore the old frame to correct the offset potentially introduced by changing AnchorPoint. This is safe to do since we know
			// that the transform is currently identity.
			layer.Frame = oldFrame;
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			var layer = Owner.Layer;
			if (Transform.IsAnimating)
			{
				// While animating the transform, we let the Animator apply the transform by itself, so do not update the Transform
				if (!_wasAnimating)
				{
					// At the beginning of the animation make sure we disable all properties of the transform
					layer.AnchorPoint = GPUFloatValueAnimator.GetAnchorForAnimation(Transform, CurrentOrigin, CurrentSize);
					layer.Transform = CoreAnimation.CATransform3D.Identity;

					_wasAnimating = true;
				}

				return;
			}
			else if (_wasAnimating)
			{
				// Make sure to fully restore the transform at the end of an animation

				layer.AnchorPoint = CurrentOrigin;
				layer.Transform = _transform = Transform.MatrixCore.ToTransform3D();

				_wasAnimating = false;

				return;
			}

			if (isSizeChanged)
			{
				// As we use the 'AnchorPoint', the transform matrix is independent of the size of the control
				// But the layouter expects from us that we restore the transform on each measuring phase
				layer.Transform = _transform;
			}
			else if (isOriginChanged)
			{
				layer.AnchorPoint = CurrentOrigin;
			}
			else
			{
				layer.Transform = _transform = Transform.MatrixCore.ToTransform3D();
			}
		}

		partial void Cleanup()
		{
			Owner.Layer.Transform = CATransform3D.Identity;
		}
	}
}
