using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
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

#if __MACOS__
		private bool _isDisposed;
		private bool _isDeferring;
		private IDisposable _deferredInitialization;
#endif

		partial void Initialized()
		{
#if __MACOS__
			Owner.WantsLayer = true;

			// On MAC OS, if we set the Transform before the NSView has been rendered at least once,
			// it will be definitively ignored (even if updated).
			// So here we hide the control (using Layer.Opacity to avoid not alter the Element itself),
			// and wait for the control to be loaded to set the transform and restore the visibility.
			if (Owner is FrameworkElement element && !element.IsLoaded)
			{
				_isDeferring = true;
				var layer = element.Layer;
				layer.Opacity = 0;
				element.Loaded += DeferredInitialize;

				_deferredInitialization = Disposable.Create(CompleteInitialization);

				return;

				void DeferredInitialize(object sender, RoutedEventArgs e)
				{
					element.Loaded -= DeferredInitialize;

					// Note: Deferring to the loaded is not enough ... we must wait for the next dispatcher loop to set the Transform!
					_ = element.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, CompleteInitialization);
				}

				void CompleteInitialization()
				{
					// Note: we unsubscribe from the event a second time in case of this adapter is being disposed before the load (cf. _deferredInitialization)
					element.Loaded -= DeferredInitialize;

					_isDeferring = false;
					layer.Opacity = 1;

					if (!_isDisposed)
					{
						InitializeOrigin();
						Update();
					}
				}
			}
#else
			// On íOS and MacOS Transform are applied by default on the center on the view
			// so make sure to reset it when the transform is attached to the view
			InitializeOrigin();

			// Apply the transform as soon as its been declared
			Update();
#endif
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
#if __MACOS__
			if (_isDeferring)
			{
				return;
			}
#endif
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
#if __MACOS__
			_isDisposed = true;
			_deferredInitialization?.Dispose();
#endif
			Owner.Layer.Transform = CATransform3D.Identity;
		}
	}
}
