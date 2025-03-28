#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Uno.UI.Extensions;
using System.Drawing;
using Windows.UI.Core;
using System.Threading.Tasks;
using Android.Views.Animations;
using Windows.UI.Xaml.Controls;
using Uno.UI.Controls;

namespace Uno.UI
{
	public static class ViewExtensions
	{
		public static void Measure(this View view, Size size)
		{
			view.Measure(
				ViewHelper.MakeMeasureSpec(size.Width, MeasureSpecMode.AtMost),
				ViewHelper.MakeMeasureSpec(size.Height, MeasureSpecMode.AtMost)
			);
		}

		public static bool HasParent(this View view)
		{
			if (view is DependencyObject provider)
			{
				// This value is set in OnLoaded, which avoids 
				// interacting with JNI, for performance.
				return provider.GetParent() != null;
			}

			return view.Parent != null;
		}

		/// <summary>
		/// Return First parent of the view of specified T type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <returns>First parent of the view of specified T type.</returns>
		public static T? FindFirstParent<T>(this IViewParent view) where T : class => FindFirstParent<T>(view, includeCurrent: false);

		public static T? FindFirstParent<T>(this IViewParent view, bool includeCurrent) where T : class => FindFirstParentOfView<T>(view as View, includeCurrent);

		/// <summary>
		/// Return First parent of the view of specified T type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="view"></param>
		/// <returns>First parent of the view of specified T type.</returns>
		public static T? FindFirstParentOfView<T>(this View? childView) where T : class => FindFirstParentOfView<T>(childView, includeCurrent: false);

		public static T? FindFirstParentOfView<T>(this View? childView, bool includeCurrent)
			where T : class
		{
			var view = includeCurrent ? childView as IViewParent : null;
			view ??= childView?.Parent;

			while (view != null)
			{
				if (view is T parent)
				{
					return parent;
				}

				view = view.Parent;
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
		public static T? FindFirstParent<T>(this IViewParent? view, Func<T, bool> predicate)
			where T : class
		{
			view = view?.Parent;

			while (view != null)
			{
				if (view is T typed && predicate(typed))
				{
					return typed;
				}

				view = view.Parent;
			}

			return null;
		}

		/// <summary>
		/// Enumerates all the parents for a view.
		/// </summary>
		public static IEnumerable<View> GetParents(this View view)
		{
			var parent = view.Parent;

			while (parent != null)
			{
				if (parent is View p)
				{
					yield return p;
				}

				if (parent == view.Parent)
				{
					break;
				}

				parent = view.Parent;
			}
		}


		/// <summary>
		/// Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group">View group</param>
		/// <returns>Children in default order</returns>
		public static IEnumerable<View> GetChildren(this ViewGroup group)
		{
			if (group is IShadowChildrenProvider shadowProvider)
			{
				// To avoid calling ChildCount/GetChildAt too much during enumeration, use
				// a fast path that relies on a shadowed list of the children in BindableView.
				return shadowProvider.ChildrenShadow;
			}
			else
			{
				return GetChildrenSlow(group);
			}
		}

		internal static TResult? FindLastChild<TParam, TResult>(this ViewGroup group, TParam param, Func<View, TParam, TResult?> selector, out bool hasAnyChildren)
			where TResult : class
		{
			hasAnyChildren = false;
			if (group is IShadowChildrenProvider shadowProvider)
			{
				// To avoid calling ChildCount/GetChildAt too much during enumeration, use
				// a fast path that relies on a shadowed list of the children in BindableView.
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

			// Slow path if the current view doesn't implement IShadowChildrenProvider
			var count = group.ChildCount;

			for (int i = count - 1; i >= 0; i--)
			{
				hasAnyChildren = true;
				var child = group.GetChildAt(i)!;
				var result = selector(child, param);
				if (result is not null)
				{
					return result;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets an reverse enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group">View group</param>
		/// <returns>Children in reverse order</returns>
		public static IEnumerable<View> GetChildrenReverse(this ViewGroup group)
		{
			if (group is IShadowChildrenProvider shadowProvider)
			{
				// To avoid calling ChildCount/GetChildAt too much during enumeration, use
				// a fast path that relies on a shadowed list of the children in BindableView.
				for (int i = shadowProvider.ChildrenShadow.Count - 1; i >= 0; i--)
				{
					yield return shadowProvider.ChildrenShadow[i];
				}
			}
			else
			{
				foreach (var child in GetChildrenReverseSlow(group))
				{
					yield return child;
				}
			}
		}

		/// <summary>
		/// Finds the first child <see cref="View"/> of the provided <see cref="ViewGroup"/>.
		/// </summary>
		/// <param name="group">The <see cref="ViewGroup"/> to search into.</param>
		/// <returns>A <see cref="View"/> if any, otherwise null.</returns>
		public static View? FindFirstChild(this ViewGroup group)
		{
			if (group is IShadowChildrenProvider shadowProvider)
			{
				// To avoid calling ChildCount/GetChildAt too much during enumeration, use
				// a fast path that relies on a shadowed list of the children in BindableView.
				if (shadowProvider.ChildrenShadow.Count != 0)
				{
					return shadowProvider.ChildrenShadow[0];
				}
			}
			else
			{
				if (group.ChildCount != 0)
				{
					return group.GetChildAt(0);
				}
			}

			return null;
		}

		/// <summary>
		/// A enumerator for ViewGroup children that uses interop calls.
		/// </summary>
		private static IEnumerable<View> GetChildrenSlow(ViewGroup group)
		{
			var count = group.ChildCount;

			for (int i = 0; i < count; i++)
			{
				yield return group.GetChildAt(i)!;
			}
		}

		/// <summary>
		/// A reverse enumerator for ViewGroup children that uses interop calls.
		/// </summary>
		private static IEnumerable<View> GetChildrenReverseSlow(ViewGroup group)
		{
			var count = group.ChildCount;

			for (int i = count - 1; i >= 0; i++)
			{
				yield return group.GetChildAt(i)!;
			}
		}

		/// <summary>
		/// Gets a filtered enumerator containing children of the view group
		/// </summary>
		/// <param name="group">The group</param>
		/// <param name="filter">The filter</param>
		/// <returns>An enumerator of the children</returns>
		public static IEnumerable<View> GetChildren(this ViewGroup group, Func<View, bool> filter)
		{
			foreach (var child in GetChildren(group))
			{
				if (filter(child))
				{
					yield return child;
				}
			}
		}


		/// <summary>
		/// Enumerates all the children for a specified view group.
		/// </summary>
		/// <param name="view">The view group to get the children from</param>
		/// <param name="selector">The selector function</param>
		/// <param name="maxDepth">The depth to stop looking for children.</param>
		/// <returns>A lazy enumerable of views</returns>
		public static IEnumerable<View> EnumerateAllChildren(this ViewGroup view, Func<View, bool> selector, int maxDepth = 20)
		{
			foreach (var sub in view.GetChildren())
			{
				if (selector(sub))
				{
					yield return sub;
				}
				else if (maxDepth > 0)
				{
					if (sub is ViewGroup childGroup)
					{
						foreach (var subResult in childGroup.EnumerateAllChildren(selector, maxDepth - 1))
						{
							yield return subResult;
						}
					}
				}
			}
		}

		/// <summary>
		/// Enumerates all the children for a specified view group in reverse order.
		/// </summary>
		/// <param name="view">The view group to get the children from</param>
		/// <param name="selector">The selector function</param>
		/// <param name="maxDepth">The depth to stop looking for children.</param>
		/// <returns>A lazy enumerable of views in reverse order</returns>
		public static IEnumerable<View> EnumerateAllChildrenReverse(this ViewGroup view, Func<View, bool> selector, int maxDepth = 20)
		{
			foreach (var sub in view.GetChildrenReverse())
			{
				if (selector(sub))
				{
					yield return sub;
				}
				else if (maxDepth > 0)
				{
					if (sub is ViewGroup childGroup)
					{
						foreach (var subResult in childGroup.EnumerateAllChildrenReverse(selector, maxDepth - 1))
						{
							yield return subResult;
						}
					}
				}
			}
		}

		/// <summary>
		/// Enumerates all the children for a specified view group.
		/// </summary>
		/// <param name="view">The view group to get the children from</param>
		/// <param name="maxDepth">The depth to stop looking for children.</param>
		/// <returns>A lazy enumerable of views</returns>
		public static IEnumerable<View> EnumerateAllChildren(this ViewGroup view, int maxDepth = 20)
		{
			foreach (var sub in view.GetChildren())
			{
				yield return sub;

				if (maxDepth > 0)
				{
					if (sub is ViewGroup childGroup)
					{
						foreach (var subResult in childGroup.EnumerateAllChildren(maxDepth - 1))
						{
							yield return subResult;
						}
					}
				}
			}
		}
		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		public static T? FindFirstChild<T>(this ViewGroup view, int? childLevelLimit = null, bool includeCurrent = true)
			where T : View
		{
			return view.FindFirstChild<T>(null, childLevelLimit, includeCurrent);
		}

		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="selector">Additional selector for the child</param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		public static T? FindFirstChild<T>(this ViewGroup view, Func<T, bool>? selector, int? childLevelLimit = null, bool includeCurrent = true)
			where T : View
		{
			Func<View?, bool> childSelector;
			if (selector == null)
			{
				childSelector = child => child is T;
			}
			else
			{
				childSelector = child => child is T t && selector(t);
			}

			if (includeCurrent
				&& childSelector(view))
			{
				return view as T;
			}

			var maxDepth = childLevelLimit ?? int.MaxValue;

			return (T?)view.EnumerateAllChildren(childSelector, maxDepth).FirstOrDefault();
		}

		/// <summary>
		/// Add view to parent.
		/// </summary>
		/// <param name="parent">Parent view</param>
		/// <param name="child">Child view to add</param>
		public static void AddChild(this ViewGroup parent, View child)
		{
			// Remove from existing parent (for compatibility with other platforms).
			(child.Parent as ViewGroup)?.RemoveView(child);

			parent.AddView(child);
		}

		/// <summary>
		/// Get the parent view in the visual tree. This may differ from the logical <see cref="FrameworkElement.Parent"/>.
		/// </summary>
		public static ViewGroup? GetVisualTreeParent(this View child)
		{
			var visualParent = child?.Parent as ViewGroup;

			// In edge cases, the native Parent is null,
			// for example for list items. For those situations
			// we use our managed visual parent instead.
			if (visualParent is null &&
				child is FrameworkElement fe)
			{
				visualParent = fe.VisualParent;
			}

			return visualParent;
		}

		/// <summary>
		/// Removes a child view from the specified view, and disposes it if the specified view is the owner.
		/// </summary>
		/// <param name="view">The ViewGroup to remove the child from.</param>
		/// <param name="child">The child view to remove</param>
		public static void RemoveViewAndDispose(this ViewGroup view, View child)
		{
			if (view is Controls.BindableView bindableView)
			{
				// Use the C# implementation of RemoveView so that it is
				// executed faster. See UnoViewGroup for details.
				bindableView.RemoveView(child);
			}
			else
			{
				view.RemoveView(child);
			}
		}

		/// <summary>
		/// Invalidates the layout of the selected view. For android, calls the RequestLayout method.
		/// </summary>
		/// <param name="view">The view to invalidate.</param>
		public static void InvalidateMeasure(this View view)
		{
			if (view is Controls.BindableView bindableView)
			{
				// Use the C# implementation of RequestLayout so that it is
				// executed faster. See UnoViewGroup for details.
				bindableView.RequestLayout();
			}
			else
			{
				view.RequestLayout();
			}
		}

		/// <summary>
		/// Invalidates the layout of the selected view. For android, calls the RequestLayout method.
		/// </summary>
		/// <param name="view">The view to invalidate.</param>
		public static void InvalidateArrange(this View view)
		{
			InvalidateMeasure(view);
		}

		/// <summary>
		/// Registers safely on the ViewAttachedToWindow on ViewDetachedFromWindow.
		/// </summary>
		/// <param name="view">The view to observe</param>
		/// <param name="attachedHandler">The method to be called when the view is attached.</param>
		/// <param name="detachedHandler">The method to be called when the view is detached.</param>
		/// <returns></returns>
		/// <remarks>
		/// This method wraps the AttachStateChangeListener for
		/// proper lifecycle management, particularly when views are disposed.
		/// </remarks>
		public static IDisposable RegisterViewAttachedStateChanged(
			this View view,
			Action<View> attachedHandler,
			Action<View> detachedHandler
		)
		{
			var listener = ViewAttachedStateChangedHelper.CreateChangedListener(attachedHandler, detachedHandler);
			view.AddOnAttachStateChangeListener(listener);

			return Disposable.Create(() =>
			{
				view.RunIfNativeInstanceAvailable(v => v.RemoveOnAttachStateChangeListener(listener));

				ViewAttachedStateChangedHelper.ReleaseChangedListener(listener);
			});
		}

		public static IDisposable RegisterTouch(this View view, EventHandler<View.TouchEventArgs> handler)
		{
			view.Touch += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.Touch -= handler));
		}

		public static IDisposable RegisterSystemUiVisibilityChange(this View view, EventHandler<View.SystemUiVisibilityChangeEventArgs> handler)
		{
			view.SystemUiVisibilityChange += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.SystemUiVisibilityChange -= handler));
		}

		public static IDisposable RegisterLongClick(this View view, EventHandler<View.LongClickEventArgs> handler)
		{
			view.LongClick += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.LongClick -= handler));
		}

		public static IDisposable RegisterLayoutChange(this View view, EventHandler<View.LayoutChangeEventArgs> handler)
		{
			view.LayoutChange += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.LayoutChange -= handler));
		}

		public static IDisposable RegisterKeyPress(this View view, EventHandler<View.KeyEventArgs> handler)
		{
			view.KeyPress += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.KeyPress -= handler));
		}

		public static IDisposable RegisterHover(this View view, EventHandler<View.HoverEventArgs> handler)
		{
			view.Hover += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.Hover -= handler));
		}

		public static IDisposable RegisterGenericMotion(this View view, EventHandler<View.GenericMotionEventArgs> handler)
		{
			view.GenericMotion += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.GenericMotion -= handler));
		}

		public static IDisposable RegisterFocusChange(this View view, EventHandler<View.FocusChangeEventArgs> handler)
		{
			view.FocusChange += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.FocusChange -= handler));
		}

		public static IDisposable RegisterDrag(this View view, EventHandler<View.DragEventArgs> handler)
		{
			view.Drag += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.Drag -= handler));
		}

		public static IDisposable RegisterContextMenuCreated(this View view, EventHandler<View.CreateContextMenuEventArgs> handler)
		{
			view.ContextMenuCreated += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.ContextMenuCreated -= handler));
		}

		public static IDisposable RegisterClick(this View view, EventHandler handler)
		{
			view.Click += handler;
			return Disposable.Create(() => view.RunIfNativeInstanceAvailable(v => v.Click -= handler));
		}

		public static Task AnimateAsync(this View view, Animation animation)
		{
			var tcs = new TaskCompletionSource<object?>();

			void OnAnimationEnd(object? s, Animation.AnimationEndEventArgs? e)
			{
				animation.AnimationEnd -= OnAnimationEnd;
				tcs.SetResult(null);
			}

			animation.AnimationEnd += OnAnimationEnd;

			view.StartAnimation(animation);

			return tcs.Task;
		}

		/// <summary>
		/// Returns the root of the view's local visual tree.
		/// </summary>
		public static ViewGroup? GetTopLevelParent(this View view)
		{
			var current = view as ViewGroup;

			while (current != null)
			{
				if (current.Parent is ViewGroup visualParent)
				{
					current = visualParent;
				}
				else
				{
					return current;
				}
			}

			return null;
		}

		/// <summary>
		/// Displays all the visual descendants of <paramref name="viewGroup"/> for diagnostic purposes. 
		/// </summary>
		public static string ShowDescendants(this ViewGroup viewGroup, StringBuilder? sb = null, string spacing = "", ViewGroup? viewOfInterest = null)
		{
			sb ??= new StringBuilder();

			Inner(viewGroup, spacing);
			return sb.ToString();

			void Inner(ViewGroup vg, string s)
			{
				AppendView(vg, s);
				s += "  ";
				for (var i = 0; i < vg.ChildCount; i++)
				{
					var child = vg.GetChildAt(i);
					if (child is ViewGroup childViewGroup)
					{
						Inner(childViewGroup, s);
					}
					else if (child is not null)
					{
						AppendView(child, s);
					}
				}
			}

			void AppendView(View innerView, string s)
			{
				var name = (innerView as IFrameworkElement)?.Name;
				var namePart = string.IsNullOrEmpty(name) ? "" : $"-'{name}'";

				var fe = innerView as IFrameworkElement;
				var u = innerView as UIElement;
				var vg = innerView as ViewGroup;

				sb
					.Append(s)
					.Append(innerView == viewOfInterest ? "*>" : ">")
					.Append(innerView.ToString() + namePart)
					.Append($"-({ViewHelper.PhysicalToLogicalPixels(innerView.Width):F1}x{ViewHelper.PhysicalToLogicalPixels(innerView.Height):F1})@({ViewHelper.PhysicalToLogicalPixels(innerView.Left):F1},{ViewHelper.PhysicalToLogicalPixels(innerView.Top):F1})")
					.Append($"  {innerView.Visibility}")
					.Append(fe is not null ? $" HA={fe.HorizontalAlignment},VA={fe.VerticalAlignment}" : "")
					.Append(fe is not null && (!double.IsNaN(fe.Width) || !double.IsNaN(fe.Height)) ? $"FE.Width={fe.Width},FE.Height={fe.Height}" : "")
					.Append(fe is not null && fe.Margin != default ? $" Margin={fe.Margin}" : "")
					.Append(fe is not null && fe.Opacity < 1 ? $" Opacity={fe.Opacity}" : "")
					.Append(fe is not null && fe.TryGetBorderThickness(out var b) && b != default ? $" Border={b}" : "")
					.Append(fe is not null && fe.TryGetPadding(out var p) && p != default ? $" Padding={p}" : "")
					.Append(u is not null ? $" DesiredSize={u.DesiredSize.ToString("F1")}" : "")
					.Append(u is { NeedsClipToSlot: true } ? " CLIPPED_TO_SLOT" : "")
					.Append(vg is { ClipToOutline: true } ? " vg.CLIP_OUTLINE" : "")
					.Append(vg is { ClipChildren: true } ? " vg.CLIP_CHILDREN" : "")
					.Append(vg is { ClipToPadding: true } ? " vg.CLIP_TO_PADDING" : "")
					.Append(u is { Clip: not null } ? $" Clip={u.Clip.Rect}" : "")
					.Append(u == null && vg is not null ? $" ClipChildren={vg.ClipChildren}" : "")
					.Append(fe is { Background: not null } ? $" Background={fe.Background}" : "")
					.Append(u?.GetElementSpecificDetails())
					.Append(u?.GetElementGridOrCanvasDetails())
					.Append(u?.RenderTransform.GetTransformDetails())
					.Append(u?.GetLayoutDetails())
					.Append(innerView is TextBlock textBlock ? $" Text=\"{textBlock.Text}\"" : "")
					.AppendLine();
			}
		}

		/// <summary>
		/// Displays the visual tree in the vicinity of <paramref name="viewGroup"/> for diagnostic purposes.
		/// </summary>
		/// <param name="viewGroup">The view to display tree for.</param>
		/// <param name="fromHeight">How many levels above <paramref name="viewGroup"/> should be included in the displayed subtree.</param>
		/// <returns>A formatted string representing the visual tree around <paramref name="viewGroup"/>.</returns>
		public static string ShowLocalVisualTree(this ViewGroup viewGroup, int fromHeight = 0)
		{
			var root = viewGroup;
			for (int i = 0; i < fromHeight; i++)
			{
				if (root.Parent is ViewGroup parent)
				{
					root = parent;
				}
				else
				{
					break;
				}
			}

			return ShowDescendants(root, viewOfInterest: viewGroup);
		}
	}

}
