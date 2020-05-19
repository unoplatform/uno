using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static bool InnerTryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			var focusedView = GetFocusedElement() as UIView;

			if (focusedView == null)
			{
				return false;
			}

			var nextFocused = InnerFindNextFocusableElement(focusNavigationDirection);

			if (nextFocused == null)
			{
				//In the case where there a no other views to the right or bottom of the currently focused view we want to resign the focus.
				focusedView.ResignFirstResponder();
			}

			if (nextFocused != null)
			{
				// Bring the element into view
				var window = nextFocused.FindFirstParent<Uno.UI.Controls.Window>(predicate: _ => true);
				window?.MakeVisible(nextFocused, BringIntoViewMode.BottomRightOfViewPort, useForcedAnimation: false);

				// Set as focused element
				nextFocused.BecomeFirstResponder();
				return true;
			}

			// nothing was found
			return false;
		}

		/// <summary>
		/// Enumerates focusable views ordered by "cousin level".
		/// "Sister" views will be returned first, then first cousins, then second cousins, and so on.
		/// </summary>
		private static IEnumerable<UIView> SearchOtherFocusableViews(UIView currentView)
		{
			var parent = currentView.Superview;
			if (parent != null)
			{
				// Search down (cousins and their children (recursively))
				foreach (var view in parent.Subviews)
				{
					if (view != currentView)
					{
						var siblings = view.FindSubviews(selector: IsFocusableView, maxDepth: 100);

						foreach (var sibling in siblings)
						{
							yield return sibling;
						}
					}
				}

				// Stop going up at Page level (we don't want to focus things on other pages)
				if (!(parent is Page))
				{
					// Search up (next ancestor)
					var nextParentResults = SearchOtherFocusableViews(parent);
					foreach (var view in nextParentResults)
					{
						yield return view;
					}
				}
			}
		}

		private static bool IsFocusableView(UIView view)
		{
			var isTextField = view is UISearchBar ||
							  view is UITextView ||
							  view is UITextField ||
							  view is TextBox;

			if (!isTextField)
			{
				return false;
			}

			var frameworkElement = view as FrameworkElement;
			if (frameworkElement != null)
			{
				return frameworkElement.Visibility == Visibility.Visible &&
					frameworkElement.IsEnabled &&
					frameworkElement.IsHitTestVisible;
			}
			return view.UserInteractionEnabled;
		}

		public static UIView InnerFindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			var focusedView = GetFocusedElement() as UIView;
			var absoluteFocusedFrame = focusedView.ConvertRectToView(focusedView.Bounds, UIApplication.SharedApplication.KeyWindow);

			var focusableViews = SearchOtherFocusableViews(focusedView);

			switch (focusNavigationDirection)
			{
				case FocusNavigationDirection.Next:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);

								if (IsInBetweenOrEqual(absoluteFrame.Top, absoluteFocusedFrame.Top, absoluteFocusedFrame.Top + 2))
								{
									// If multiple views are on the same y, go right.
									return absoluteFrame.Left > absoluteFocusedFrame.Left;
								}
								else
								{
									// Otherwise we want a "lower" view on the y axis.
									return absoluteFrame.Top > absoluteFocusedFrame.Top;
								}
							});

				case FocusNavigationDirection.Previous:
					//We need to reverse the order of focusable views because they are enumerated from left to right and we need from right to left.
					focusableViews = focusableViews.Reverse();
					var leftFocusable = focusableViews
						.FirstOrDefault(v =>
						{
							var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
							if (IsInBetweenOrEqual(absoluteFrame.Top, absoluteFocusedFrame.Top, absoluteFocusedFrame.Top + 2))
							{
								return absoluteFrame.Left < absoluteFocusedFrame.Left;
							}
							return false;
						});

					return leftFocusable ?? focusableViews
						.FirstOrDefault(v =>
						{
							var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
							return absoluteFrame.Top < absoluteFocusedFrame.Top;
						});

				case FocusNavigationDirection.Up:
					return focusableViews
							.Reverse()
							.FirstOrDefault(v =>
							{
								var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
								if (absoluteFrame.Left == absoluteFocusedFrame.Left)
								{
									return absoluteFrame.Top < absoluteFocusedFrame.Top;
								}
								return false;
							});

				case FocusNavigationDirection.Down:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
								if (absoluteFrame.Left == absoluteFocusedFrame.Left)
								{
									return absoluteFrame.Top > absoluteFocusedFrame.Top;
								}
								return false;
							});

				case FocusNavigationDirection.Left:
					return focusableViews
							.Reverse()
							.FirstOrDefault(v =>
							{
								var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
								if (IsInBetweenOrEqual(absoluteFrame.Top, absoluteFocusedFrame.Top, absoluteFocusedFrame.Top + 2))
								{
									// If multiple views are on the same y, go Left.
									return absoluteFrame.Left < absoluteFocusedFrame.Left;
								}
								return false;
							});

				case FocusNavigationDirection.Right:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absoluteFrame = v.ConvertRectToView(v.Bounds, UIApplication.SharedApplication.KeyWindow);
								if (IsInBetweenOrEqual(absoluteFrame.Top, absoluteFocusedFrame.Top, absoluteFocusedFrame.Top + 2))
								{
									// If multiple views are on the same y, go Right.
									return absoluteFrame.Left > absoluteFocusedFrame.Left;
								}
								return false;
							});

				case FocusNavigationDirection.None:
					return null;

				default:
					return null;
			}
		}

		private static DependencyObject InnerFindFirstFocusableElement(DependencyObject searchScope)
		{
			if (searchScope == null)
			{
				searchScope = Window.Current.Content;
			}

			if (!(searchScope is UIView searchView))
			{
				return null;
			}

			return searchView.FindSubviews(selector: IsFocusableView, maxDepth: 100).FirstOrDefault() as DependencyObject;
		}

		private static DependencyObject InnerFindLastFocusableElement(DependencyObject searchScope)
		{
			if (searchScope == null)
			{
				searchScope = Window.Current.Content;
			}

			if (!(searchScope is UIView searchView))
			{
				return null;
			}

			return searchView.FindSubviewsReverse(selector: IsFocusableView, maxDepth: 100).FirstOrDefault() as DependencyObject;
		}

		private static void FocusNative(Control control) => control.BecomeFirstResponder();

		//We need to validate this difference because focused elements don't have the same absolute position once focused
		private static bool IsInBetweenOrEqual(nfloat number, nfloat lowerlimit, nfloat highlimit)
		{
			if (number <= highlimit && number >= lowerlimit)
			{
				return true;
			}
			return false;
		}
	}
}
