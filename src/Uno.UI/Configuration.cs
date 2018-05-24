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
			/// Enable the visualization of clipping bounds.
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
		}
	}
}
