using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using System.Drawing;
using Uno.UI;
using CoreAnimation;

#if __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#endif

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform: iOS part
	/// </summary>
	public abstract partial class Transform
	{
		private bool _needsUpdate;

		public Transform()
		{
		}

		protected void SetNeedsUpdate()
		{
			if (_needsUpdate)
			{
				return;
			}
			_needsUpdate = true;
			Dispatcher.RunAsync(
				Core.CoreDispatcherPriority.Normal,
				() =>
				{
					Update();
					_needsUpdate = false;
				}
			);
		}

		/// <summary>
		/// Returns the native CGAffineTransform associated with this Transform.
		/// </summary>
		/// <param name="size">The size of the view.</param>
		/// <param name="withCenter">Whether Center (CenterX/CenterY) information should be part of the transform. Providing false is useful if a transform is applied to an element Center information is already part of the </param>
		/// <returns></returns>
#if __IOS__
		internal virtual CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			throw new NotImplementedException(nameof(ToNativeTransform) + " not implemented for " + this.GetType().ToString());
		}
#elif __MACOS__
		internal virtual CATransform3D ToNativeTransform(CGSize size, bool withCenter = true)
		{
			throw new NotImplementedException(nameof(ToNativeTransform) + " not implemented for " + this.GetType().ToString());
		}
#endif

		internal static double ToRadians(double angle) => MathEx.ToRadians(angle);

		partial void OnDetachedFromViewPartial(_View view)
		{
#if __IOS__
			view.Transform = CGAffineTransform.MakeIdentity();
#elif __MACOS__
			view.Layer.Transform = CATransform3D.Identity;
#endif

			if (view is FrameworkElement fe)
			{
				fe.SizeChanged -= Fe_SizeChanged;
			}
		}

		partial void OnAttachedToViewPartial(_View view)
		{
#if __MACOS__
			view.WantsLayer = true;
#endif
			if (view is FrameworkElement fe)
			{
				fe.SizeChanged += Fe_SizeChanged;
			}
		}

		private void Fe_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			Update();
		}

		partial void UpdatePartial()
		{
			if (View != null)
			{
				var size = GetViewSize(View);
				SetAnchorPoint(GetAnchorPoint(size), View);

#if __IOS__
				// Center (CenterX/CenterY) is already part of AnchorPoint, we don't want to apply it twice.
				View.Transform = ToNativeTransform(size, withCenter: false);
#elif __MACOS__
				// Center (CenterX/CenterY) is already part of AnchorPoint, we don't want to apply it twice.
				View.Layer.Transform = ToNativeTransform(size, withCenter: false);
#endif


			}
		}

		/// <summary>
		/// Change a view's anchor point without moving it 
		/// </summary>
		/// <remarks>
		/// Source: https://stackoverflow.com/a/5666430
		/// </remarks>
		private static void SetAnchorPoint(CGPoint anchorPoint, _View view)
		{
			var newPoint = new CGPoint(view.Bounds.Size.Width * anchorPoint.X, view.Bounds.Size.Height * anchorPoint.Y);
			var oldPoint = new CGPoint(view.Bounds.Size.Width * view.Layer.AnchorPoint.X, view.Bounds.Size.Height * view.Layer.AnchorPoint.Y);

#if __IOS__
			newPoint = view.Transform.TransformPoint(newPoint);
			oldPoint = view.Transform.TransformPoint(oldPoint);
#elif __MACOS__
			// macOS TODO
			//newPoint = view.Layer.Transform.TransformPoint(newPoint);
			//oldPoint = view.Layer.Transform.TransformPoint(oldPoint);
#endif


			var position = view.Layer.Position;

			position.X -= oldPoint.X;
			position.X += newPoint.X;

			position.Y -= oldPoint.Y;
			position.Y += newPoint.Y;

			view.Layer.AnchorPoint = anchorPoint;
			view.Layer.Position = position;
		}

		/// <summary>
		/// Get size of the view before any transform is applied.
		/// </summary>
		protected static CGSize GetViewSize(_View view)
		{
			CGSize? appliedFrame = (view as IFrameworkElement)?.AppliedFrame.Size;
			return appliedFrame ?? view.Frame.Size;
		}

		/// <summary>
		/// Gets the AnchorPoint to be applied to the UIView's Layer, 
		/// considering both UIElement's RenderTransformOrigin and Transform's Center (CenterX/CenterY).
		/// </summary>
		private Foundation.Point GetAnchorPoint(Foundation.Size size)
		{
			if (size.Width == 0 || size.Height == 0)
			{
				return Origin;
			}

			var center = GetCenter();

			return new Foundation.Point(
				Origin.X + (center.X / size.Width),
				Origin.Y + (center.Y / size.Height)
			);
		}

		private Foundation.Point GetCenter()
		{
			switch (this)
			{
				case RotateTransform rotateTransform:
					return new Foundation.Point(rotateTransform.CenterX, rotateTransform.CenterY);
				case ScaleTransform scaleTransform:
					return new Foundation.Point(scaleTransform.CenterX, scaleTransform.CenterY);
				case SkewTransform skewTransform:
					return new Foundation.Point(skewTransform.CenterX, skewTransform.CenterY);
				case CompositeTransform compositeTransform:
					return new Foundation.Point(compositeTransform.CenterX, compositeTransform.CenterY);
				default:
					return new Foundation.Point(0, 0);
			}
		}
	}
}

