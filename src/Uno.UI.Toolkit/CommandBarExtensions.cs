using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Toolkit
{
#if !NET6_0_OR_GREATER // Moved to the linker definition file
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
#endif
	public static class CommandBarExtensions
	{
		#region Subtitle

		public static DependencyProperty SubtitleProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Subtitle",
				typeof(string),
				typeof(CommandBarExtensions),
				new PropertyMetadata(null)
			);

		public static void SetSubtitle(this CommandBar commandBar, string subtitle)
		{
			commandBar.SetValue(SubtitleProperty, subtitle);
		}

		public static string GetSubtitle(this CommandBar commandBar)
		{
			return (string)commandBar.GetValue(SubtitleProperty);
		}

		#endregion

		#region NavigationCommand

		public static DependencyProperty NavigationCommandProperty { get; } =
			DependencyProperty.RegisterAttached(
				"NavigationCommand",
				typeof(AppBarButton),
				typeof(CommandBarExtensions),
#if XAMARIN
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueInheritsDataContext)
#else
				new PropertyMetadata(null)
#endif
			);

		public static void SetNavigationCommand(this CommandBar commandBar, AppBarButton navigationCommand)
		{
			commandBar.SetValue(NavigationCommandProperty, navigationCommand);
		}

		public static AppBarButton GetNavigationCommand(this CommandBar commandBar)
		{
			return (AppBarButton)commandBar.GetValue(NavigationCommandProperty);
		}

		#endregion

		#region BackButtonTitle

		public static DependencyProperty BackButtonTitleProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BackButtonTitle",
				typeof(string),
				typeof(CommandBarExtensions),
				new PropertyMetadata(null)
			);

		public static void SetBackButtonTitle(this CommandBar commandBar, string backButtonTitle)
		{
			commandBar.SetValue(BackButtonTitleProperty, backButtonTitle);
		}

		public static string GetBackButtonTitle(this CommandBar commandBar)
		{
			return (string)commandBar.GetValue(BackButtonTitleProperty);
		}

		#endregion

		#region BackButtonVisibility

		public static DependencyProperty BackButtonVisibilityProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BackButtonVisibility",
				typeof(Visibility),
				typeof(CommandBarExtensions),
				new PropertyMetadata(Visibility.Collapsed)
			);

		public static void SetBackButtonVisibility(this CommandBar commandBar, Visibility BackButtonVisibility)
		{
			commandBar.SetValue(BackButtonVisibilityProperty, BackButtonVisibility);
		}

		public static Visibility GetBackButtonVisibility(this CommandBar commandBar)
		{
			return (Visibility)commandBar.GetValue(BackButtonVisibilityProperty);
		}

		#endregion

		#region BackButtonForeground

		public static DependencyProperty BackButtonForegroundProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BackButtonForeground",
				typeof(Brush),
				typeof(CommandBarExtensions),
#if XAMARIN
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueInheritsDataContext)
#else
				new PropertyMetadata(null)
#endif
			);

		public static void SetBackButtonForeground(this CommandBar commandBar, Brush backButtonForeground)
		{
			commandBar.SetValue(BackButtonForegroundProperty, backButtonForeground);
		}

		public static Brush GetBackButtonForeground(this CommandBar commandBar)
		{
			return (Brush)commandBar.GetValue(BackButtonForegroundProperty);
		}

		#endregion

		#region BackButtonIcon

		public static DependencyProperty BackButtonIconProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BackButtonIcon",
				typeof(IconElement),
				typeof(CommandBarExtensions),
				new PropertyMetadata(null)
			);

		public static void SetBackButtonIcon(this CommandBar commandBar, IconElement backButtonIcon)
		{
			commandBar.SetValue(BackButtonIconProperty, backButtonIcon);
		}

		public static IconElement GetBackButtonIcon(this CommandBar commandBar)
		{
			return (IconElement)commandBar.GetValue(BackButtonIconProperty);
		}

		#endregion
	}
}
