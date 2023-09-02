using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI;
using Uno.Foundation.Logging;
using Uno.Collections;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

using View = AppKit.NSView;
using AppKit;
using CoreGraphics;
using Uno.Disposables;
using CoreAnimation;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class Layouter
	{
		public IEnumerable<View> GetChildren()
		{
			return (Panel as NSView).GetChildren();
		}

		protected Size MeasureChildOverride(View view, Size slotSize)
		{
			var ret = view
				.SizeThatFits(slotSize.LogicalToPhysicalPixels())
				.PhysicalToLogicalPixels()
				.ToFoundationSize();

			// With iOS, a child may return a size that fits that is larger than the suggested size.
			// We don't want that with respects to the Xaml model, so we cap the size to the input constraints.
			ret.Width = double.IsNaN(ret.Width) ? double.PositiveInfinity : Math.Min(slotSize.Width, ret.Width);
			ret.Height = double.IsNaN(ret.Height) ? double.PositiveInfinity : Math.Min(slotSize.Height, ret.Height);

			return ret;
		}

		protected void ArrangeChildOverride(View view, Rect frame)
		{
			var nativeFrame = ViewHelper.LogicalToPhysicalPixels(frame);

			if (nativeFrame != view.Frame)
			{
				LogArrange(view, nativeFrame);

				using (SettingFrame(view))
				{
					view.Frame = nativeFrame;

					UpdateClip(view);
				}
			}
		}

		private void LogArrange(View view, CGRect frame)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				LogArrange(view, (Rect)frame);
			}
		}

		private static void UpdateClip(View view)
		{
			// TODO

			//if (!FeatureConfiguration.UIElement.UseLegacyClipping)
			//{
			//	UIElement.UpdateMask(view, view.Superview);

			//	foreach (var child in view.GetChildren())
			//	{
			//		UIElement.UpdateMask(child, view);
			//	}
			//}
		}

		/// <summary>
		/// Handle the native <see cref="View.Transform"/> in the (rare) case that this is a non-IFrameworkElement view with a 
		/// non-identity transform.
		/// </summary>
		private IDisposable SettingFrame(View view)
		{
			if (view is IFrameworkElement)
			{
				// This is handled directly in IFrameworkElement.Frame setter
				return null;
			}

			var layer = view.Layer;
			if (layer == null)
			{
				// Transform is identity anyway, or Layer is null
				return null;
			}

			// If NSView.Transform is not identity, then modifying the frame will give undefined behavior. (https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIView_Class/#//apple_ref/occ/instp/NSView/transform)
			// We have either already applied the transform to the new frame, or we will reset the transform straight after.
			var transform = layer.Transform;
			if (transform.IsIdentity)
			{
				return null;
			}
			transform = CATransform3D.Identity;
			return Disposable.Create(reapplyTransform);

			void reapplyTransform()
			{
				layer.Transform = transform;
			}
		}
	}
}
