#if XAMARIN_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.UI;
using Uno.Logging;
using Uno.Collections;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

using View = UIKit.UIView;
using UIKit;
using CoreGraphics;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class Layouter
	{
		public IEnumerable<View> GetChildren()
		{
			return (Panel as UIView).GetChildren();
		}

		/// <summary>
		/// Provides the desired size of the element, from the last measure phase.
		/// </summary>
		/// <param name="view">The element to get the measured with</param>
		/// <returns>The measured size</returns>
		Size ILayouter.GetDesiredSize(View view)
		{
			return DesiredChildSize(view);
		}

		protected Size DesiredChildSize(View view)
		{
			var uiElement = view as UIElement;

			if (uiElement != null)
			{
				return uiElement.DesiredSize;
			}
			else
			{
				return _layoutProperties.GetValue(view, "desiredSize", () => default(Size));
			}
		}

		partial void SetDesiredChildSize(View view, Size desiredSize)
		{
			var uiElement = view as UIElement;

			if (uiElement != null)
			{
				uiElement.DesiredSize = desiredSize;
			}
			else
			{
				_layoutProperties.SetValue(view, "desiredSize", desiredSize);
			}
		}

		private static UnsafeWeakAttachedDictionary<View, string> _layoutProperties = new UnsafeWeakAttachedDictionary<View, string>();

		protected Size MeasureChildOverride(View view, Size slotSize)
		{
			var ret = view
				.SizeThatFits(slotSize.LogicalToPhysicalPixels())
				.PhysicalToLogicalPixels()
				.ToFoundationSize();

			// With iOS, a child may return a size that fits that is larger than the suggested size.
			// We don't want that with respects to the Xaml model, so we cap the size to the input constraints.
			if (nfloat.IsNaN((nfloat)ret.Width) || nfloat.IsNaN((nfloat)ret.Height))
			{
				ret.ToString();
			}



			ret.Width = nfloat.IsNaN((nfloat)ret.Width) ? double.PositiveInfinity : Math.Min(slotSize.Width, ret.Width);
			ret.Height = nfloat.IsNaN((nfloat)ret.Height) ? double.PositiveInfinity : Math.Min(slotSize.Height, ret.Height);

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
				}
			}
		}

		private void LogArrange(View view, CGRect frame)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				LogArrange(view, (Rect)frame);
			}
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

			if (view.Transform.IsIdentity)
			{
				// Transform is identity anyway
				return null;
			}

			// If UIView.Transform is not identity, then modifying the frame will give undefined behavior. (https://developer.apple.com/library/ios/documentation/UIKit/Reference/UIView_Class/#//apple_ref/occ/instp/UIView/transform)
			// We have either already applied the transform to the new frame, or we will reset the transform straight after.
			var transform = view.Transform;
			view.Transform = CGAffineTransform.MakeIdentity();
			return Disposable.Create(reapplyTransform);

			void reapplyTransform()
			{
				view.Transform = transform;
			}
		}
	}
}
#endif
