using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Extensions;
using System;

#if WINAPPSDK
#elif __IOS__
using UIKit;
using Uno.UI.Controls;
#else
using Uno.UI;
using Uno.UI.Controls;
#endif

namespace Uno.UI.Samples.Behaviors
{
	/// <summary>
	/// This behavior adds features to the SplitView.
	/// </summary>
	public class SplitViewBehavior
	{
		#region OpenOnClick

		public static bool GetOpenOnClick(DependencyObject obj)
		{
			return (bool)obj.GetValue(OpenOnClickProperty);
		}

		public static void SetOpenOnClick(DependencyObject obj, bool value)
		{
			obj.SetValue(OpenOnClickProperty, value);
		}

		/// <summary>
		/// When True, opens the parent SplitView when clicked.
		/// The control attached with this property must satisfy the following.
		/// - It must be a child of the SplitView.
		/// - It must derive from ButtonBase or be an item of a ListViewBase.
		/// </summary>
		public static DependencyProperty OpenOnClickProperty { get; } =
			DependencyProperty.RegisterAttached("OpenOnClick", typeof(bool), typeof(SplitViewBehavior), new PropertyMetadata(default(bool), OnOpenOnClickChanged));

		private static void OnOpenOnClickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			RegisterToClick(dependencyObject);
		}

		#endregion

		#region CloseOnClick

		public static bool GetCloseOnClick(DependencyObject obj)
		{
			return (bool)obj.GetValue(CloseOnClickProperty);
		}

		public static void SetCloseOnClick(DependencyObject obj, bool value)
		{
			obj.SetValue(CloseOnClickProperty, value);
		}

		/// <summary>
		/// When True, closes the SplitView when clicked.
		/// The control attached with this property must satisfy the following.
		/// - It must be a child of the SplitView.
		/// - It must derive from ButtonBase or be an item of a ListViewBase.
		/// </summary>
		public static DependencyProperty CloseOnClickProperty { get; } =
			DependencyProperty.RegisterAttached("CloseOnClick", typeof(bool), typeof(SplitViewBehavior), new PropertyMetadata(default(bool), OnCloseOnClickChanged));

		private static void OnCloseOnClickChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			RegisterToClick(dependencyObject);
		}

		#endregion

		private static void RegisterToClick(DependencyObject dependencyObject)
		{
			#region ButtonBase registration
			var button = dependencyObject as ButtonBase;
			if (button != null)
			{
				void handleClick(object s, object e) => OnClicked(button);

				button.Loaded += (s, e) =>
				{
					button.Click += handleClick;
				};

				button.Unloaded += (s, e) =>
				{
					button.Click -= handleClick;
				};

				return;
			}
			#endregion

			#region ListViewBase registration
			var listBase = dependencyObject as ListViewBase;

			if (listBase != null)
			{
				void handleClick(object s, object e) => OnClicked(listBase);

				listBase.Loaded += (s, e) =>
				{
					listBase.ItemClick += handleClick;
				};

				listBase.Unloaded += (s, e) =>
				{
					listBase.ItemClick -= handleClick;
				};

				return;
			}
			#endregion
		}

		private static void OnClicked(FrameworkElement element)
		{
			#region SplitView
			var splitView = element.FindFirstParent<SplitView>();
			if (splitView != null)
			{
				if (splitView.IsPaneOpen && GetCloseOnClick(element))
				{
					splitView.IsPaneOpen = false;
				}
				else if (!splitView.IsPaneOpen && GetOpenOnClick(element))
				{
					splitView.IsPaneOpen = true;
				}

				return;
			}
			#endregion

			#region BindableDrawerLayout
#if __ANDROID__
			//This handles if projects still use BindableDrawerLayout for android.
			var bindableDrawer = element.FindFirstParent<BindableDrawerLayout>();

			if (bindableDrawer != null)
			{
				if (bindableDrawer.IsLeftPaneOpen && GetCloseOnClick(element))
				{
					bindableDrawer.IsLeftPaneOpen = false;
				}
				else if (!bindableDrawer.IsLeftPaneOpen && GetOpenOnClick(element))
				{
					bindableDrawer.IsLeftPaneOpen = true;
				}

				return;
			}
#endif
			#endregion
		}
	}
}
