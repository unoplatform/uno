using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using CoreAnimation;
using CoreGraphics;
using Uno.UI.Extensions;

namespace Uno.UI.Media
{
	partial class NativeRenderTransformAdapter
	{
		private CATransform3D _transform = CATransform3D.Identity;
		private bool _wasAnimating = false;

		partial void Initialized()
		{
			// On íOS and MacOS Transform are applied by default on the center on the view
			// so make sure to reset it when the transform is attached to the view
			InitializeOrigin();

#if __MACOS__
			// On MAC OS, if we set the Transform before the NSView has been rendered at least once,
			// it will be definitively ignored (even if updated).
			// So here we hide the control (using Layer.Opacity to avoid not alter the Element itself),
			// and wait for the control to be loaded to set the transform and restore the visibility.
			if (Owner is FrameworkElement element && !element.IsLoaded)
			{
				_isDeferring = true;
				element.Loaded += DeferredInitialize;
				element.Layer.Opacity = 0;

				return;
			}
#endif

			// Apply the transform as soon as its been declared
			Update();
		}

#if __MACOS__
		private bool _isDeferring;

		private static void DeferredInitialize(object sender, RoutedEventArgs e)
		{
			var fwElt = (FrameworkElement)sender;
			var adapter = fwElt._renderTransform;

			fwElt.Loaded -= DeferredInitialize;
			fwElt.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				// Note: Deferring to the loaded is not enough ... we must wait for the next dispatcher loop to set the Transform!
				adapter._isDeferring = false;
				adapter.Update(isOriginChanged: true);
				adapter.Update();
				fwElt.Layer.Opacity = 1;
			});
		}

		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
			if (_isDeferring)
			{
				return;
			}
#else
		partial void Apply(bool isSizeChanged, bool isOriginChanged)
		{
#endif
			if (Transform.IsAnimating)
			{
				// While animating the transform, we let the Animator apply the transform by itself, so do not update the Transform
				if (!_wasAnimating)
				{
					// At the beginning of the animation make sure we disable all properties of the transform
					Owner.Layer.AnchorPoint = GPUFloatValueAnimator.GetAnchorForAnimation(Transform, CurrentOrigin, CurrentSize);
					Owner.Layer.Transform = CoreAnimation.CATransform3D.Identity;

					_wasAnimating = true;
				}

				return;
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

		private void InitializeOrigin()
		{
#if __MACOS__
			Owner.WantsLayer = true;
#endif

			var oldFrame = Owner.Layer.Frame;

			Owner.Layer.AnchorPoint = CurrentOrigin;

			// Restore the old frame to correct the offset potentially introduced by changing AnchorPoint. This is safe to do since we know
			// that the transform is currently identity.
			Owner.Layer.Frame = oldFrame;
		}

		partial void Cleanup()
		{
			Owner.Layer.Transform = CATransform3D.Identity;
		}
	}
}
