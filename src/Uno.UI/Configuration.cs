using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI
{
	public static class FeatureConfiguration
	{
		public static class UIElement
		{
			/// <summary>
			/// Enables the legacy clipping behavior which only applies binding to itself and 
			/// its childen. Normal clipping only applies to a single UIElement considering its
			/// clipping parent, based on <see cref="Windows.UI.Xaml.UIElement.ClipChildrenToBounds"/>.
			/// </summary>
			public static bool UseLegacyClipping { get; set; } = true;

			/// <summary>
			/// Enable the visualization of clipping bounds (intended for diagnostic purposes).
			/// </summary>
			public static bool ShowClippingBounds { get; set; } = false;
		}

		public static class FrameworkElement
		{
			/// <summary>
			/// Enables the behavior for which the style is applied before the inherited
			/// FrameworkElement instances constructors. The UWP behavior is to apply
			/// </summary>
			public static bool UseLegacyApplyStylePhase { get; set; } = false;

			/// <summary>
			/// When changing a style on a <see cref="Windows.UI.Xaml.FrameworkElement"/> clears 
			/// the previous style setters. This property is applicable only when <see cref="UseLegacyApplyStylePhase"/>
			/// is set to <see cref="false"/>.
			/// </summary>
			public static bool ClearPreviousOnStyleChange { get; set; } = true;
		}

		public static class Style
		{
			/// <summary>
			/// Determines if Uno.UI should be using native styles for controls that have
			/// a native counterpart. (e.g. Button, Slider, ComboBox, ...)
			/// </summary>
			public static bool UseUWPDefaultStyles { get; set; } = true;
		}

		public static class Control
		{
			/// <summary>
			/// Make the default value of VerticalContentAlignment and HorizontalContentAlignment be Stretch instead of Center
			/// </summary>
			public static bool UseLegacyContentAlignment { get; set; } = false;

			/// <summary>
			/// Enables the lazy materialization of <see cref="Windows.UI.Xaml.Controls.Control"/> template. This behavior
			/// is not aligned with UWP, which materializes templates immediately, making x:Name controls available
			/// in the constructor of a control.
			/// </summary>
			public static bool UseLegacyLazyApplyTemplate { get; set; } = false;
		}

		public static class ListViewBase
		{
			/// <summary>
			/// Sets the value to use for <see cref="ItemsStackPanel.CacheLength"/> and <see cref="ItemsWrapGrid.CacheLength"/> if not set 
			/// explicitly in Xaml or code. Higher values will cache more views either side of the visible window, improving list performance 
			/// at the expense of consuming more memory. Setting this to null will leave the default value at the UWP default of 4.0.
			/// </summary>
			public static double? DefaultCacheLength = 1.0;
		}

		public static class TextBlock
		{
			/// <summary>
			/// Enable the visualization of hyperlink hit-testing layouts (intended for diagnostic purposes).
			/// </summary>
			public static bool ShowHyperlinkLayouts { get; set; } = false;
		}

		public static class Page
		{
			/// <summary>
			/// Enables reuse of <see cref="Page"/> instances. Enabling can improve performance when using <see cref="Frame"/> navigation.
			/// </summary>
			public static bool IsPoolingEnabled { get; set; } = false;
		}
	}
}
