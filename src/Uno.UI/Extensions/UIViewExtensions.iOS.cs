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
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace UIKit
{
	public static class UIViewExtensions
	{
		public static bool HasParent(this UIView view)
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
		public static T FindFirstParent<T>(this UIView view, Func<UIView, T> selector)
			where T : UIView
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
		public static T FindFirstParent<T>(this UIView view, Func<T, bool> predicate)
			where T : UIView
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
		public static T FindFirstParent<T>(this UIView view)
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

		public static T FindFirstChild<T>(this UIView view, Func<T, bool> selector = null, int? childLevelLimit = null, bool includeCurrent = true)
			where T : UIView
		{
			Func<UIView, bool> viewSelector;
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
		public static void RemoveChild(this UIView view, UIView child)
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
		public static void InvalidateMeasure(this UIView view)
		{
			view.SetNeedsLayout();
		}

		/// <summary>
		/// Invalidates the layout of the selected view. For iOS, calls the SetNeedsLayout method.
		/// </summary>
		/// <param name="view">The view to invalidate.</param>
		public static void InvalidateArrange(this UIView view)
		{
			InvalidateMeasure(view);
		}

		public static IEnumerable<UIView> FindSubviews(this UIView view, Func<UIView, bool> selector, int maxDepth = 20)
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
		public static IEnumerable<UIView> EnumerateAllChildren(this UIView view, int maxDepth = 20)
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

		public static IEnumerable<T> FindSubviewsOfType<T>(this UIView view, int maxDepth = 20) where T : class
		{
			return FindSubviews(view, (v) => v as T != null, maxDepth)
				.OfType<T>();
		}

		/// <summary>
		/// Gets a list of superviews, optionally filtered
		/// </summary>
		/// <param name="view">The view to get the parents from</param>
		/// <param name="predicate">The predicate used to stop the superview search</param>
		/// <returns>An enumerable of UIView instances.</returns>
		public static IEnumerable<UIView> FindSuperviews(this UIView view, Func<UIView, bool> predicate = null)
		{
			predicate = predicate ?? Funcs.Create((UIView v) => true);

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
		public static IEnumerable<T> FindSuperviewsOfType<T>(this UIView view)
		{
			return view.FindSuperviews().OfType<T>();
		}

		public static void SetPosition(this UIView view, float? x = null, float? y = null)
		{
			view.Frame = new CGRect(x ?? view.Frame.X, y ?? view.Frame.Y, view.Frame.Width, view.Frame.Height);
		}

		public static void SetDimensions(this UIView view, nfloat? width = null, nfloat? height = null)
		{
			if (nfloat.IsNaN(width ?? 0) || nfloat.IsNaN(height ?? 0))
			{
				view.Log().ErrorFormat("A view Frame must not be set to [{0};{1}], overriding to [0;0]", width, height);

				width = 0;
				height = 0;
			}

			view.Frame = new CGRect(view.Frame.X, view.Frame.Y, width ?? view.Frame.Width, height ?? view.Frame.Height);
		}

		public static UIView FindFirstResponder(this UIView view)
		{
			if (view.IsFirstResponder)
			{
				return view;
			}
			foreach (UIView subView in view.Subviews)
			{
				var firstResponder = subView.FindFirstResponder();
				if (firstResponder != null)
				{
					return firstResponder;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the nearest view controller for this UIView.
		/// </summary>
		/// <returns>A UIViewController instance, otherwise null.</returns>
		public static UIViewController FindViewController(this UIView view)
		{
			if (view?.NextResponder == null)
			{
				// Sometimes, a view is not part of the visual tree (or doesn't have a next responder) but is part of the logical tree.
				// Here, we substitute the view with the first logical parent that's part of the visual tree (or has a next responder).
				view = (view as DependencyObject)
					.GetParents()
					.OfType<UIView>()
					.Where(parent => parent.NextResponder != null)
					.FirstOrDefault();
			}

			UIResponder responder = view;

			do
			{
				if (responder is UIView uiView)
				{
					responder = uiView.NextResponder;
				}
				else if (responder is UIViewController uiViewController)
				{
					return uiViewController;
				}

			} while (responder != null);

			return null;
		}

		public static UIView FindSuperviewOfType(this UIView view, UIView stopAt, Type type)
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

		public static T FindSuperviewOfType<T>(this UIView view, UIView stopAt) where T : UIView
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

		public static nfloat StackSubViews(this UIView thisView)
		{
			return ViewHelper.StackSubViews(thisView.Subviews);
		}

		public static nfloat StackSubViews(this UIView thisView, float topPadding, float spaceBetweenElements)
		{
			return ViewHelper.StackSubViews(thisView, topPadding, spaceBetweenElements);
		}

		public static void ResignFirstResponderOnSubviews(this UIView thisView)
		{
			thisView.ResignFirstResponder();
			foreach (var view in thisView.Subviews)
			{
				view.ResignFirstResponderOnSubviews();
			}
		}

		public static void AddToBottom(this UIView thisView, UIView toAdd)
		{
			if (thisView.Subviews.Any())
			{
				var bottom = thisView.Subviews.Max(v => v.Frame.Bottom);
				toAdd.Frame = toAdd.Frame.IncrementY((nfloat)bottom);
			}
			thisView.AddSubview(toAdd);
		}

		public static void AddToViewBottom(this UIView thisView, UIView toAddTo)
		{
			toAddTo.AddToBottom(thisView);
		}

		public static T AddTo<T>(this T thisView, UIView toAddTo) where T : UIView
		{
			toAddTo.AddSubview(thisView);
			return thisView;
		}

		public static void AddBorder(this UIView thisButton, float borderThickness = 1, UIColor borderColor = null)
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

		public static void RoundCorners(this UIView thisButton, float radius = 2, float borderThickness = 0, UIColor borderColor = null)
		{
			thisButton.AddBorder(borderThickness, borderColor);
			thisButton.Layer.CornerRadius = radius;
		}

		public static UIView HitTestOutsideFrame(this UIView thisView, CGPoint point, UIEvent uievent)
		{
			// All touches that are on this view (and not its subviews) are ignored
			if (!thisView.Hidden && thisView.Alpha > 0)
			{
				foreach (var subview in thisView.Subviews.Safe().Reverse())
				{
					var subPoint = subview.ConvertPointFromView(point, thisView);
					var result = subview.HitTest(subPoint, uievent);
					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets an identifier that can be used for logging
		/// </summary>
		public static string GetDebugIdentifier(this UIView element)
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
		/// Enumerates the children for the specified instance, either using UIView.Subviews or using IShadowChildrenProvider.
		/// </summary>
		/// <param name="view"></param>
		/// <returns></returns>
		public static IEnumerable<UIView> GetChildren(this UIView view)
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
		/// Finds the first child <see cref="UIView"/> of the provided <see cref="UIView"/>.
		/// </summary>
		/// <param name="view">The <see cref="UIView"/> to search into.</param>
		/// <returns>A <see cref="UIView"/> if any, otherwise null.</returns>
		public static UIView FindFirstChild(this UIView view)
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
		public static UIView GetTopLevelParent(this UIView view)
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
		public static string ShowDescendants(this UIView view, StringBuilder sb = null, string spacing = "", UIView viewOfInterest = null)
		{
			sb = sb ?? new StringBuilder();
			AppendView(view);
			spacing += "  ";
			foreach (var child in view.Subviews)
			{
				ShowDescendants(child, sb, spacing, viewOfInterest);
			}

			return sb.ToString();

			void AppendView(UIView innerView)
			{
				sb.AppendLine($"{spacing}{(innerView == viewOfInterest ? "*" : "")}>{innerView.ToString()}-({innerView.Frame.Width}x{innerView.Frame.Height})");
			}
		}

		/// <summary>
		/// Displays the visual tree in the vicinity of <paramref name="view"/> for diagnostic purposes.
		/// </summary>
		/// <param name="view">The view to display tree for.</param>
		/// <param name="fromHeight">How many levels above <paramref name="view"/> should be included in the displayed subtree.</param>
		/// <returns>A formatted string representing the visual tree around <paramref name="view"/>.</returns>
		public static string ShowLocalVisualTree(this UIView view, int fromHeight = 0)
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
	}
}
