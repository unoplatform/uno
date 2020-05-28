using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Uno.UI;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Android.Graphics;
using Windows.UI.ViewManagement;

namespace Windows.UI.Xaml.Input
{
	public partial class FocusManager
	{
		private static View InnerGetFocusedElement()
		{
			var currentActivity = ContextHelper.Current as Activity;
			var currentFocusedView = currentActivity?.CurrentFocus;
			return currentFocusedView;
		}

		private static bool InnerTryMoveFocus(FocusNavigationDirection focusNavigationDirection)
		{
			var focusedView = InnerGetFocusedElement();

			if (focusedView == null)
			{
				return false;
			}

			var nextFocused = InnerFindNextFocusableElement(focusNavigationDirection);

			if (nextFocused == null)
			{
				//In the case where there a no other views to the right or bottom of the currently focused view we want to dismiss the keyboard.
				var activity = ContextHelper.Current as Activity;

				var inputManager = activity?.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
				inputManager?.HideSoftInputFromWindow(activity?.CurrentFocus?.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
			}

			return nextFocused?.RequestFocus(FocusSearchDirection.Forward) ?? false;
		}

		/// <summary>
		/// Enumerates focusable views ordered by "cousin level".
		/// "Sister" views will be returned first, then first cousins, then second cousins, and so on.
		/// </summary>
		private static IEnumerable<View> SearchOtherFocusableViews(View currentView)
		{
			var parent = currentView.ParentForAccessibility as ViewGroup;
			if (parent != null)
			{
				// Search down (cousins and their children (recursively))
				foreach (var view in parent.GetChildren())
				{
					if (view != currentView)
					{
						var siblings = (view as ViewGroup)?.EnumerateAllChildren(IsFocusableView, maxDepth: 300).ToList();

						if (siblings != null)
						{
							foreach (var sibling in siblings)
							{
								yield return sibling;
							}
						}
					}
				}

				// Stop going up at Page level (we don't want to focus things on other pages)
				if (!(parent is Page))
				{
					// Search up (next ancestor)
					var nextParentResults = SearchOtherFocusableViews(parent).ToList();
					foreach (var view in nextParentResults)
					{
						yield return view;
					}
				}
			}
		}


		private static bool IsFocusableView(View view)
		{
			var isTextField = view is TextBoxView;

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

			return view.Focusable && view.Enabled;
		}

		private static int[] GetAbsolutePosition(View v)
		{
			var absoluteFocusedPosition = new[] { 0, 0 };
			v.GetLocationOnScreen(absoluteFocusedPosition);
			return absoluteFocusedPosition;
		}

		public static View InnerFindNextFocusableElement(FocusNavigationDirection focusNavigationDirection)
		{
			var focusedView = InnerGetFocusedElement();
			var absoluteFocusedPosition = GetAbsolutePosition(focusedView);
			var focusableViews = SearchOtherFocusableViews(focusedView);

			switch (focusNavigationDirection)
			{
				case FocusNavigationDirection.Next:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								if (absolutePosition[1] == absoluteFocusedPosition[1])
								{
									// If multiple views are on the same y, go right.
									return absolutePosition[0] > absoluteFocusedPosition[0];
								}
								else
								{
									// Otherwise we want a "lower" view on the y axis.
									return absolutePosition[1] > absoluteFocusedPosition[1];
								}
							});

				case FocusNavigationDirection.Previous:
					//We need to reverse the order of focusable views because they are enumerated from left to right and we need from right to left.
					focusableViews = focusableViews.Reverse();
					var leftFocusable = focusableViews
						.FirstOrDefault(v =>
						{
							var absolutePosition = GetAbsolutePosition(v);
							if (absolutePosition[1] == absoluteFocusedPosition[1])
							{
								return absolutePosition[0] < absoluteFocusedPosition[0];
							}
							return false;
						});

					return leftFocusable ?? focusableViews
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								return absolutePosition[1] < absoluteFocusedPosition[1];
							});

				case FocusNavigationDirection.Up:
					return focusableViews
							.Reverse()
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								if (absolutePosition[0] == absoluteFocusedPosition[0])
								{
									return absolutePosition[1] < absoluteFocusedPosition[1];
								}
								return false;
							});

				case FocusNavigationDirection.Down:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								if (absolutePosition[0] == absoluteFocusedPosition[0])
								{
									return absolutePosition[1] > absoluteFocusedPosition[1];
								}
								return false;
							});

				case FocusNavigationDirection.Left:
					return focusableViews
							.Reverse()
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								if (absolutePosition[1] == absoluteFocusedPosition[1])
								{
									return absolutePosition[0] < absoluteFocusedPosition[0];
								}
								return false;
							});

				case FocusNavigationDirection.Right:
					return focusableViews
							.FirstOrDefault(v =>
							{
								var absolutePosition = GetAbsolutePosition(v);
								if (absolutePosition[1] == absoluteFocusedPosition[1])
								{
									return absolutePosition[0] > absoluteFocusedPosition[0];
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

			if (!(searchScope is ViewGroup searchViewGroup))
			{
				return null;
			}

			return searchViewGroup.EnumerateAllChildren(IsFocusableView, maxDepth: 300).FirstOrDefault() as DependencyObject;			
		}

		private static DependencyObject InnerFindLastFocusableElement(DependencyObject searchScope)
		{
			if (searchScope == null)
			{
				searchScope = Window.Current.Content;
			}

			if (!(searchScope is ViewGroup searchViewGroup))
			{
				return null;
			}

			return searchViewGroup.EnumerateAllChildrenReverse(IsFocusableView, maxDepth: 300).FirstOrDefault() as DependencyObject;
		}

		private static void FocusNative(Control control)
		{
			control.RequestFocus();

			// Forcefully try to bring the control into view when keyboard is open to accommodate adjust nothing mode
			if (InputPane.GetForCurrentView().Visible)
			{
				control.StartBringIntoView();
			}
		}
	}
}
