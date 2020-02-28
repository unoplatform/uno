using Uno.Disposables;
using Uno;
using Uno.Extensions;
using Uno.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Windows.UI.Xaml;
using System.Diagnostics.CodeAnalysis;
using Uno.Logging;
using Windows.UI.Core;
using Uno.UI.Controls;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
using _View = UIKit.UIView;
using _Controller = UIKit.UIViewController;
using _Responder = UIKit.UIResponder;
using _Color = UIKit.UIColor;
using _Event = UIKit.UIEvent;
#elif __MACOS__
using Foundation;
using AppKit;
using CoreGraphics;
using _View = AppKit.NSView;
using _Controller = AppKit.NSViewController;
using _Responder = AppKit.NSResponder;
using _Color = AppKit.NSColor;
using _Event = AppKit.NSEvent;
#endif

#if XAMARIN_IOS_UNIFIED
namespace UIKit
#elif __MACOS__
namespace AppKit
#endif
{
	public static class UIViewExtensions
	{
		public static bool HasParent(this _View view)
		{
			return view.Superview != null;
		}

		/// <summary>
		/// Return First parent of the view of specified T type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <param name="selector">Selector that will be proved against the parents of type T</param>
		/// <returns>Return the first parent of the view of specified T type that also holds true with the selector</returns>
		public static T FindFirstParent<T>(this _View view, Func<_View, T> selector)
			where T : _View
		{
			view = view?.Superview;
			while (view != null)
			{
				if (selector(view) != null)
				{
					return view as T;
				}

				view = view.Superview;
			}

			return null;
		}
		/// <summary>
		/// Return First parent of the view of specified T type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <param name="predicate">Predicate that will be proved against the parents of type T</param>
		/// <returns>First parent of the view of specified T type that also holds true with the predicate</returns>
		public static T FindFirstParent<T>(this _View view, Func<T, bool> predicate)
			where T : _View
		{
			view = view?.Superview;
			while (view != null)
			{
				var typed = view as T;
				if (typed != null && predicate(typed))
				{
					return typed;
				}

				view = view.Superview;
			}

			return null;
		}

		/// <summary>
		/// Return First parent of the view of specified T type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <returns>First parent of the view of specified T type.</returns>
		public static T FindFirstParent<T>(this _View view)
			where T : class
		{
			view = view?.Superview;
			while (view != null)
			{
				var typed = view as T;
				if (typed != null)
				{
					return typed;
				}
				view = view.Superview;
			}
			return null;
		}

		public static T FindFirstChild<T>(this _View view, Func<T, bool> selector = null, int? childLevelLimit = null, bool includeCurrent = true)
			where T : _View
		{
			Func<_View, bool> viewSelector;
			if (selector == null)
			{
				viewSelector = v => v is T;
			}
			else
			{
				viewSelector = v =>
				{
					var t = v as T;
					return t != null && selector(t);
				};
			}

			if (includeCurrent
				&& viewSelector(view))
			{
				return (T)view;
			}

			var maxDepth = childLevelLimit.HasValue
				? childLevelLimit.Value
				: int.MaxValue;

			return (T)(view.FindSubviews(viewSelector, maxDepth).FirstOrDefault());
		}

		/// <summary>
		/// Removes the provided child from this view.
		/// </summary>
		/// <param name="child">The child to remove</param>
		public static void RemoveChild(this _View view, _View child)
		{
			if (child.Superview != view)
			{
				throw new Exception("This child not part of this view");
			}

			child.RemoveFromSuperview();
		}

		/// <summary>
		/// Invalidates the layout of the selected view. For iOS, calls the SetNeedsLayout method.
		/// </summary>
		/// <param name="view">The view to invalidate.</param>
		public static void InvalidateMeasure(this _View view)
		{
			view.SetNeedsLayout();
		}

#if __MACOS__
		public static void SetNeedsLayout(this _View view)
		{
			view.NeedsLayout = true;
		}
#endif

		/// <summary>
		/// Invalidates the layout of the selected view. For iOS, calls the SetNeedsLayout method.
		/// </summary>
		/// <param name="view">The view to invalidate.</param>
		public static void InvalidateArrange(this _View view)
		{
			InvalidateMeasure(view);
		}

		public static IEnumerable<_View> FindSubviews(this _View view, Func<_View, bool> selector, int maxDepth = 20)
		{
			foreach (var sub in view.Subviews)
			{
				if (selector(sub))
				{
					yield return sub;
				}
				else if (maxDepth > 0)
				{
					foreach (var subResult in sub.FindSubviews(selector, maxDepth - 1))
					{
						yield return subResult;
					}
				}
			}
		}

		/// <summary>
		/// Enumerates all the children for a specified view.
		/// </summary>
		/// <param name="view">The view group to get the children from</param>
		/// <param name="maxDepth">The depth to stop looking for children.</param>
		/// <returns>A lazy enumerable of views</returns>
		public static IEnumerable<_View> EnumerateAllChildren(this _View view, int maxDepth = 20)
		{
			foreach (var subview in view.Subviews)
			{
				yield return subview;

				if (maxDepth > 0)
				{
					foreach (var subResult in subview.EnumerateAllChildren(maxDepth - 1))
					{
						yield return subResult;
					}
				}
			}
		}

		/// <summary>
		/// Add view to parent.
		/// </summary>
		/// <param name="parent">Parent view</param>
		/// <param name="child">Child view to add</param>
		public static void AddChild(this _View parent, _View child)
		{
			parent.AddSubview(child);
		}

		/// <summary>
		/// Get the parent view in the visual tree. This may differ from the logical <see cref="FrameworkElement.Parent"/>.
		/// </summary>
		public static _View GetVisualTreeParent(this _View child) => child?.Superview;

		public static IEnumerable<T> FindSubviewsOfType<T>(this _View view, int maxDepth = 20) where T : class
		{
			return FindSubviews(view, (v) => v as T != null, maxDepth)
				.OfType<T>();
		}

		/// <summary>
		/// Gets a list of superviews, optionally filtered
		/// </summary>
		/// <param name="view">The view to get the parents from</param>
		/// <param name="predicate">The predicate used to stop the superview search</param>
		/// <returns>An enumerable of _View instances.</returns>
		public static IEnumerable<_View> FindSuperviews(this _View view, Func<_View, bool> predicate = null)
		{
			predicate = predicate ?? Funcs.Create((_View v) => true);

			while (view != null)
			{
				if (predicate(view))
				{
					yield return view;
				}
				else
				{
					yield break;
				}

				view = view.Superview;
			}
		}

		/// <summary>
		/// Gets a list of superviews of type T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <returns></returns>
		public static IEnumerable<T> FindSuperviewsOfType<T>(this _View view)
		{
			return view.FindSuperviews().OfType<T>();
		}

		public static void SetPosition(this _View view, float? x = null, float? y = null)
		{
			view.Frame = new CGRect(x ?? view.Frame.X, y ?? view.Frame.Y, view.Frame.Width, view.Frame.Height);
		}

		public static void SetDimensions(this _View view, nfloat? width = null, nfloat? height = null)
		{
			if (nfloat.IsNaN(width ?? 0) || nfloat.IsNaN(height ?? 0))
			{
				view.Log().ErrorFormat("A view Frame must not be set to [{0};{1}], overriding to [0;0]", width, height);

				width = 0;
				height = 0;
			}

			view.Frame = new CGRect(view.Frame.X, view.Frame.Y, width ?? view.Frame.Width, height ?? view.Frame.Height);
		}

		/// <summary>
		/// Finds first responder in view
		/// </summary>
		/// <param name="view">View</param>
		/// <returns>First responder view</returns>
		public static _View FindFirstResponder(this _View view) =>
			Uno.Extensions.UIViewExtensions.FindFirstResponder(view);		

		/// <summary>
		/// Finds the nearest view controller for this _View.
		/// </summary>
		/// <returns>A _ViewController instance, otherwise null.</returns>
		public static _Controller FindViewController(this _View view)
		{
			if (view?.NextResponder == null)
			{
				// Sometimes, a view is not part of the visual tree (or doesn't have a next responder) but is part of the logical tree.
				// Here, we substitute the view with the first logical parent that's part of the visual tree (or has a next responder).
				view = (view as DependencyObject)
					.GetParents()
					.OfType<_View>()
					.Where(parent => parent.NextResponder != null)
					.FirstOrDefault();
			}

			_Responder responder = view;

			do
			{
				if (responder is _View nativeView)
				{
					responder = nativeView.NextResponder;
				}
				else if (responder is _Controller controller)
				{
					return controller;
				}

			} while (responder != null);

			return null;
		}

		public static _View FindSuperviewOfType(this _View view, _View stopAt, Type type)
		{
			if (view.Superview != null)
			{
				if (type.IsInstanceOfType(view.Superview))
				{
					return view.Superview;
				}

				if (view.Superview != stopAt)
				{
					return view.Superview.FindSuperviewOfType(stopAt, type);
				}
			}

			return null;
		}

		public static T FindSuperviewOfType<T>(this _View view, _View stopAt) where T : _View
		{
			if (view.Superview != null)
			{
				var t = view.Superview as T;
				if (t != null)
				{
					return t;
				}

				if (view.Superview != stopAt)
				{
					return view.Superview.FindSuperviewOfType<T>(stopAt);
				}
			}

			return null;
		}

		public static nfloat StackSubViews(this _View thisView)
		{
			return ViewHelper.StackSubViews(thisView.Subviews);
		}

		public static nfloat StackSubViews(this _View thisView, float topPadding, float spaceBetweenElements)
		{
			return ViewHelper.StackSubViews(thisView, topPadding, spaceBetweenElements);
		}

		public static void ResignFirstResponderOnSubviews(this _View thisView)
		{
			thisView.ResignFirstResponder();
			foreach (var view in thisView.Subviews)
			{
				view.ResignFirstResponderOnSubviews();
			}
		}

		public static void AddToBottom(this _View thisView, _View toAdd)
		{
			if (thisView.Subviews.Any())
			{
				var bottom = thisView.Subviews.Max(v => v.Frame.Bottom);
				toAdd.Frame = toAdd.Frame.IncrementY((nfloat)bottom);
			}
			thisView.AddSubview(toAdd);
		}

		public static void AddToViewBottom(this _View thisView, _View toAddTo)
		{
			toAddTo.AddToBottom(thisView);
		}

		public static T AddTo<T>(this T thisView, _View toAddTo) where T : _View
		{
			toAddTo.AddSubview(thisView);
			return thisView;
		}

		public static void AddBorder(this _View thisButton, float borderThickness = 1, _Color borderColor = null)
		{
			if (borderColor != null)
			{
				thisButton.Layer.BorderColor = borderColor.CGColor;
			}
			if (borderThickness > 0)
			{
				if (Math.Abs(borderThickness - 1f) < float.Epsilon && ViewHelper.IsRetinaDisplay)
				{
					borderThickness = (float)ViewHelper.OnePixel;
				}
				thisButton.Layer.BorderWidth = borderThickness;
			}
		}

		public static void RoundCorners(this _View thisButton, float radius = 2, float borderThickness = 0, _Color borderColor = null)
		{
			thisButton.AddBorder(borderThickness, borderColor);
			thisButton.Layer.CornerRadius = radius;
		}

		public static _View HitTestOutsideFrame(
			this _View thisView
			, CGPoint point
#if __IOS__
			, _Event uievent
#endif
		)
		{
			// All touches that are on this view (and not its subviews) are ignored
			if (!thisView.Hidden && thisView.GetNativeAlpha() > 0)
			{
				foreach (var subview in thisView.Subviews.Safe().Reverse())
				{
					var subPoint = subview.ConvertPointFromView(point, thisView);
#if __IOS__
					var result = subview.HitTest(subPoint, uievent);
#elif __MACOS__
					var result = subview.HitTest(subPoint);
#endif
					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}

		public static nfloat GetNativeAlpha(this _View view)
		{
#if __MACOS__
			return view.AlphaValue;
#elif __IOS__
			return view.Alpha;
#endif
		}

		/// <summary>
		/// Gets an identifier that can be used for logging
		/// </summary>
		public static string GetDebugIdentifier(this _View element)
		{
			if (element == null)
			{
				return "--NULL--";
			}

			var name = (element as IFrameworkElement)?.Name;
			return name.HasValue()
				? element.GetType().Name + "_" + name + "_" + element.GetHashCode()
				: element.GetType().Name + "_" + element.GetHashCode();
		}

		/// <summary>
		/// Enumerates the children for the specified instance, either using _View.Subviews or using IShadowChildrenProvider.
		/// </summary>
		/// <param name="view"></param>
		/// <returns></returns>
		public static IEnumerable<_View> GetChildren(this _View view)
		{
			if (view is IShadowChildrenProvider shadow)
			{
				return shadow.ChildrenShadow;
			}
			else
			{
				return view.Subviews;
			}
		}

		/// <summary>
		/// Finds the first child <see cref="_View"/> of the provided <see cref="_View"/>.
		/// </summary>
		/// <param name="view">The <see cref="_View"/> to search into.</param>
		/// <returns>A <see cref="_View"/> if any, otherwise null.</returns>
		public static _View FindFirstChild(this _View view)
		{
			var shadow = view as IShadowChildrenProvider;

			if (shadow != null)
			{
				if (shadow.ChildrenShadow.Count != 0)
				{
					return shadow.ChildrenShadow[0];
				}
			}
			else
			{
				var subviews = view.Subviews;
				if (subviews.Length != 0)
				{
					return subviews[0];
				}
			}

			return null;
		}
		
		/// <summary>
		/// Returns the root of the view's local visual tree.
		/// </summary>
		public static _View GetTopLevelParent(this _View view)
		{
			var current = view;
			while (current != null)
			{
				if (current.Superview == null)
				{
					return current;
				}
				current = current.Superview;
			}

			throw new ArgumentNullException(nameof(view));
		}

		/// <summary>
		/// Displays all the visual descendants of <paramref name="view"/> for diagnostic purposes. 
		/// </summary>
		public static string ShowDescendants(this _View view, StringBuilder sb = null, string spacing = "", _View viewOfInterest = null)
		{
			sb = sb ?? new StringBuilder();
			AppendView(view);
			spacing += "  ";
			foreach (var child in view.Subviews)
			{
				ShowDescendants(child, sb, spacing, viewOfInterest);
			}

			return sb.ToString();

			StringBuilder AppendView(_View innerView)
			{
				var name = (innerView as IFrameworkElement)?.Name;
				var namePart = string.IsNullOrEmpty(name) ? "" : $"-'{name}'";

				var uiElement = innerView as UIElement;

				return sb
						.Append(spacing)
						.Append(innerView == viewOfInterest ? "*>" : ">")
						.Append(innerView.ToString() + namePart)
						.Append($"-({innerView.Frame.Width}x{innerView.Frame.Height})@({innerView.Frame.X},{innerView.Frame.Y})")

#if __IOS__
						.Append($" {(innerView.Hidden ? "Hidden" : "Visible")}")
#endif
						.Append(uiElement?.NeedsClipToSlot ?? false ? " CLIPPED_TO_SLOT" : "")
						.AppendLine();
			}
		}

		/// <summary>
		/// Displays the visual tree in the vicinity of <paramref name="view"/> for diagnostic purposes.
		/// </summary>
		/// <param name="view">The view to display tree for.</param>
		/// <param name="fromHeight">How many levels above <paramref name="view"/> should be included in the displayed subtree.</param>
		/// <returns>A formatted string representing the visual tree around <paramref name="view"/>.</returns>
		public static string ShowLocalVisualTree(this _View view, int fromHeight = 0)
		{
			var root = view;
			for (int i = 0; i < fromHeight; i++)
			{
				if (root.Superview != null)
				{
					root = root.Superview;
				}
				else
				{
					break;
				}
			}
			return ShowDescendants(root, viewOfInterest: view);
		}

		public static void SetNeedsDisplay(this _View view)
		{
#if __IOS__
			view.SetNeedsDisplay();
#elif __MACOS__
			view.NeedsDisplay = true;
#endif
		} 
	}
}
