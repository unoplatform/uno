using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTipService
{
	public static DependencyProperty ToolTipProperty
	{
		[DynamicDependency(nameof(GetToolTip))]
		[DynamicDependency(nameof(SetToolTip))]
		get;
	} = DependencyProperty.RegisterAttached(
			"ToolTip",
			typeof(object),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default, OnToolTipChanged));

	public static object GetToolTip(DependencyObject element) => element.GetValue(ToolTipProperty);

	public static void SetToolTip(DependencyObject element, object value) => element.SetValue(ToolTipProperty, value);

	public static DependencyProperty PlacementProperty
	{
		[DynamicDependency(nameof(GetPlacement))]
		[DynamicDependency(nameof(SetPlacement))]
		get;
	} = DependencyProperty.RegisterAttached(
			"Placement",
			typeof(PlacementMode),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(PlacementMode.Top, OnPlacementChanged));

	public static PlacementMode GetPlacement(DependencyObject element) => (PlacementMode)element.GetValue(PlacementProperty);

	public static void SetPlacement(DependencyObject element, PlacementMode value) => element.SetValue(PlacementProperty, value);

	/// <summary>
	/// Gets or sets the object relative to which the tooltip is positioned.
	/// </summary>
	public static DependencyProperty PlacementTargetProperty
	{
		[DynamicDependency(nameof(GetPlacementTarget))]
		[DynamicDependency(nameof(SetPlacementTarget))]
		get;
	} = DependencyProperty.RegisterAttached(
			"PlacementTarget",
			typeof(UIElement),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default(UIElement)));

	/// <summary>
	/// Gets the ToolTipService.PlacementTarget XAML attached property value for the specified target element.
	/// </summary>
	/// <param name="element">The target element for the attached property value.</param>
	/// <returns>The visual element that the tooltip is positioned relative to.</returns>
	public static UIElement GetPlacementTarget(DependencyObject element) => (UIElement)element.GetValue(PlacementTargetProperty);

	/// <summary>
	/// Sets the ToolTipService.PlacementTarget XAML attached property value for the specified target element.
	/// </summary>
	/// <param name="element">The target element for the attached property value.</param>
	/// <param name="value">The visual element that should be the placement target for the tooltip.</param>
	public static void SetPlacementTarget(DependencyObject element, UIElement value) => element.SetValue(PlacementTargetProperty, value);

	internal static DependencyProperty ToolTipReferenceProperty
	{
		[DynamicDependency(nameof(GetToolTipReference))]
		[DynamicDependency(nameof(SetToolTipReference))]
		get;
	} = DependencyProperty.RegisterAttached(
			"ToolTipReference",
			typeof(ToolTip),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default(ToolTip)));

	internal static ToolTip GetToolTipReference(DependencyObject element) => (ToolTip)element.GetValue(ToolTipReferenceProperty);

	internal static void SetToolTipReference(DependencyObject element, ToolTip value) => element.SetValue(ToolTipReferenceProperty, value);

	internal static DependencyProperty KeyboardAcceleratorToolTipProperty
	{
		[DynamicDependency(nameof(GetKeyboardAcceleratorToolTip))]
		[DynamicDependency(nameof(SetKeyboardAcceleratorToolTip))]
		get;
	} = DependencyProperty.RegisterAttached(
			"KeyboardAcceleratorToolTip",
			typeof(object),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default, OnToolTipChanged));

	internal static object GetKeyboardAcceleratorToolTip(DependencyObject element) => element.GetValue(KeyboardAcceleratorToolTipProperty);

	internal static void SetKeyboardAcceleratorToolTip(DependencyObject element, object value) => element.SetValue(KeyboardAcceleratorToolTipProperty, value);

	internal static DependencyProperty KeyboardAcceleratorToolTipObjectProperty
	{
		[DynamicDependency(nameof(GetKeyboardAcceleratorToolTipObject))]
		[DynamicDependency(nameof(SetKeyboardAcceleratorToolTipObject))]
		get;
	} = DependencyProperty.RegisterAttached(
			"KeyboardAcceleratorToolTipObject",
			typeof(ToolTip),
			typeof(ToolTipService),
			new FrameworkPropertyMetadata(default));

	internal static ToolTip GetKeyboardAcceleratorToolTipObject(DependencyObject element) => (ToolTip)element.GetValue(KeyboardAcceleratorToolTipObjectProperty);

	internal static void SetKeyboardAcceleratorToolTipObject(DependencyObject element, ToolTip value) => element.SetValue(KeyboardAcceleratorToolTipObjectProperty, value);
}
