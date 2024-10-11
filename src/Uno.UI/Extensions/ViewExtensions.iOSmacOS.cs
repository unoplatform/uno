#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using System;
using Uno.UI.Controls;
using CoreAnimation;
using CoreGraphics;
using Uno.Disposables;

#if __IOS__
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#endif

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		public static string ShowLocalVisualTree(this UIElement element, int fromHeight = 0)
		{
			return UIViewExtensions.ShowLocalVisualTree(element as View, fromHeight);
		}

		internal static IEnumerable<View> GetChildren(this UIElement element) => element.ChildrenShadow;

		internal static TResult? FindLastChild<TParam, TResult>(this View group, TParam param, Func<View, TParam, TResult?> selector, out bool hasAnyChildren)
			where TResult : class
		{
			hasAnyChildren = false;
			if (group is IShadowChildrenProvider shadowProvider)
			{
				var childrenShadow = shadowProvider.ChildrenShadow;
				for (int i = childrenShadow.Count - 1; i >= 0; i--)
				{
					hasAnyChildren = true;
					var result = selector(childrenShadow[i], param);
					if (result is not null)
					{
						return result;
					}
				}

				return null;
			}

			var subviews = group.Subviews;
			for (int i = subviews.Length - 1; i >= 0; i--)
			{
				hasAnyChildren = true;
				var result = selector(subviews[i], param);
				if (result is not null)
				{
					return result;
				}
			}

			return null;
		}

#if __IOS__
		/// <summary>
		/// Handle the native <see cref="View.Transform"/> in the (rare) case that this is a non-IFrameworkElement view with a 
		/// non-identity transform.
		/// </summary>
		internal static IDisposable? SettingFrame(this View view)
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
#elif __MACOS__
		/// <summary>
		/// Handle the native <see cref="View.Transform"/> in the (rare) case that this is a non-IFrameworkElement view with a 
		/// non-identity transform.
		/// </summary>
		internal static IDisposable? SettingFrame(this View view)
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

#endif
	}
}
