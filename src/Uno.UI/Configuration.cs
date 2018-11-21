using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

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

		public static class Page
		{
			/// <summary>
			/// Enables reuse of <see cref="Page"/> instances. Enabling can improve performance when using <see cref="Frame"/> navigation.
			/// </summary>
			public static bool IsPoolingEnabled { get; set; } = false;
		}

		public static class AutomationPeer
		{
			/// <summary>
			/// Enable a mode that simplifies accessibility by automatically grouping accessible elements into top-level accessible elements. The default value is false.
			/// </summary>
			/// <remarks>
			/// When enabled, the accessibility name of top-level accessible elements (elements that return a non-null AutomationPeer in <see cref="UIElement.OnCreateAutomationPeer()"/> and/or have <see cref="AutomationProperties.Name" /> set to a non-empty string) 
			/// will be an aggregate of the accessibility name of all child accessible elements.
			/// 
			/// For example, if you have a <see cref="Button"/> that contains 3 <see cref="TextBlock"/> "A" "B" "C", the accessibility name of the <see cref="Button"/> will be "A, B, C". 
			/// These 3 <see cref="TextBlock"/> will also be automatically excluded from accessibility focus.
			/// 
			/// This greatly facilitates accessibility, as you would need to do this manually on UWP.
			/// 
			/// A limitation of this strategy is that you can't nest interactive elements, as children of an accessible elements are excluded from accessibility focus.
			/// For example, if you put a <see cref="Button"/> inside another <see cref="Button"/>, only the parent <see cref="Button"/> will be focusable.
			/// This happens to match a limitation of iOS, which does this by default and forces developers to make elements as siblings instead of nesting them.
			/// 
			/// To prevent a top-level accessible element from being accessible and make its children accessibility focusable, you can set <see cref="AutomationProperties.AccessibilityViewProperty"/> to <see cref="AccessibilityView.Raw"/>.
			/// 
			/// Note: This is incompatible with the way accessibility works on UWP.
			/// </remarks>
			public static bool UseSimpleAccessibility { get; set; } = false;
		}

		public static class Font
		{
			/// <summary>
			/// Defines the default font to be used when displaying symbols, such as in SymbolIcon.
			/// </summary>
			public static string SymbolsFont { get; set; } =
#if !ANDROID
			"Symbols";
#else
			"ms-appx:///Assets/Fonts/winjs-symbols.ttf#Symbols";
#endif
		}
	}
}
