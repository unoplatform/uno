using System.Diagnostics.CodeAnalysis;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class RelativePanel
{
	/// <summary>
	/// Gets or sets a value that indicates how far the background extends in relation to this element's border.
	/// </summary>
	public BackgroundSizing BackgroundSizing
	{
		get => GetBackgroundSizingValue();
		set => SetBackgroundSizingValue(value);
	}

	/// <summary>
	/// Identifies the BackgroundSizing dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = default(BackgroundSizing), ChangedCallback = true)]
	public static DependencyProperty BackgroundSizingProperty { get; } = CreateBackgroundSizingProperty();

	private void OnBackgroundSizingChanged(DependencyPropertyChangedEventArgs e) =>
		base.OnBackgroundSizingChangedInnerPanel(e);

	/// <summary>
	/// Gets or sets a brush that describes the border fill of the panel.
	/// </summary>
	public Brush BorderBrush
	{
		get => GetBorderBrushValue();
		set => SetBorderBrushValue(value);
	}

	private static Brush GetBorderBrushDefaultValue() => SolidColorBrushHelper.Transparent;

	/// <summary>
	/// Identifies the BorderBrush dependency property.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderBrushPropertyChanged), Options = FrameworkPropertyMetadataOptions.ValueInheritsDataContext)]
	public static DependencyProperty BorderBrushProperty { get; } = CreateBorderBrushProperty();

	private void OnBorderBrushPropertyChanged(Brush oldValue, Brush newValue)
	{
		BorderBrushInternal = newValue;
		OnBorderBrushChanged(oldValue, newValue);
	}

	/// <summary>
	/// Gets or sets the border thickness of the panel.
	/// </summary>
	public Thickness BorderThickness
	{
		get => GetBorderThicknessValue();
		set => SetBorderThicknessValue(value);
	}

	private static Thickness GetBorderThicknessDefaultValue() => Thickness.Empty;

	/// <summary>
	/// Identifies the BorderThickness dependency property.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderThicknessPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public static DependencyProperty BorderThicknessProperty { get; } = CreateBorderThicknessProperty();

	private void OnBorderThicknessPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		BorderThicknessInternal = newValue;
		OnBorderThicknessChanged(oldValue, newValue);
	}

	/// <summary>
	/// Gets or sets the distance between the border and its child object.
	/// </summary>
	public Thickness Padding
	{
		get => GetPaddingValue();
		set => SetPaddingValue(value);
	}

	private static Thickness GetPaddingDefaultValue() => Thickness.Empty;

	/// <summary>
	/// Identifies the Padding dependency property.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnPaddingPropertyChanged), Options = FrameworkPropertyMetadataOptions.AffectsMeasure)]
	public static DependencyProperty PaddingProperty { get; } = CreatePaddingProperty();

	private void OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		PaddingInternal = newValue;
		OnPaddingChanged(oldValue, newValue);
	}

	/// <summary>
	/// Gets or sets the radius for the corners of the panel's border.
	/// </summary>
	public CornerRadius CornerRadius
	{
		get => GetCornerRadiusValue();
		set => SetCornerRadiusValue(value);
	}

	private static CornerRadius GetCornerRadiusDefaultValue() => CornerRadius.None;

	/// <summary>
	/// Identifies the CornerRadius dependency property.
	/// </summary>
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnCornerRadiusPropertyChanged))]
	public static DependencyProperty CornerRadiusProperty { get; } = CreateCornerRadiusProperty();

	private void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		CornerRadiusInternal = newValue;
		OnCornerRadiusChanged(oldValue, newValue);
	}

	#region Panel Alignment relationships

	/// <summary>
	/// Gets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignBottomWithPanel(UIElement element) => (bool)element.GetValue(AlignBottomWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignBottomWithPanel(UIElement element, bool value) => element.SetValue(AlignBottomWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignBottomWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignBottomWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignBottomWithPanel))]
		[DynamicDependency(nameof(SetAlignBottomWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignBottomWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignLeftWithPanel(UIElement element) => (bool)element.GetValue(AlignLeftWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignLeftWithPanel(UIElement element, bool value) => element.SetValue(AlignLeftWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignLeftWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignLeftWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignLeftWithPanel))]
		[DynamicDependency(nameof(SetAlignLeftWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignLeftWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignRightWithPanel(UIElement element) => (bool)element.GetValue(AlignRightWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignRightWithPanel(UIElement element, bool value) => element.SetValue(AlignRightWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignRightWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignRightWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignRightWithPanel))]
		[DynamicDependency(nameof(SetAlignRightWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignRightWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignTopWithPanel(UIElement element) => (bool)element.GetValue(AlignTopWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignTopWithPanel(UIElement element, bool value) => element.SetValue(AlignTopWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignTopWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignTopWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignTopWithPanel))]
		[DynamicDependency(nameof(SetAlignTopWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignTopWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignHorizontalCenterWithPanel(UIElement element) => (bool)element.GetValue(AlignHorizontalCenterWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignHorizontalCenterWithPanel(UIElement element, bool value) => element.SetValue(AlignHorizontalCenterWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property.
	/// </summary>
	public static DependencyProperty AlignHorizontalCenterWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignHorizontalCenterWithPanel))]
		[DynamicDependency(nameof(SetAlignHorizontalCenterWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignHorizontalCenterWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignVerticalCenterWithPanel(UIElement element) => (bool)element.GetValue(AlignVerticalCenterWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignVerticalCenterWithPanel(UIElement element, bool value) => element.SetValue(AlignVerticalCenterWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignVerticalCenterWithPanel XAML attached property.
	/// </summary>
	public static DependencyProperty AlignVerticalCenterWithPanelProperty
	{
		[DynamicDependency(nameof(GetAlignVerticalCenterWithPanel))]
		[DynamicDependency(nameof(SetAlignVerticalCenterWithPanel))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignVerticalCenterWithPanel",
			typeof(bool),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: false, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	#endregion

	#region Sibling Alignment relationships

	/// <summary>
	/// Gets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignBottomWith(UIElement element) => element.GetValue(AlignBottomWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignBottomWith(UIElement element, object value) => element.SetValue(AlignBottomWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignBottomWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignBottomWithProperty
	{
		[DynamicDependency(nameof(GetAlignBottomWith))]
		[DynamicDependency(nameof(SetAlignBottomWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignBottomWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignLeftWith(UIElement element) => (object)element.GetValue(AlignLeftWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignLeftWith(UIElement element, object value) => element.SetValue(AlignLeftWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignLeftWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignLeftWithProperty
	{
		[DynamicDependency(nameof(GetAlignLeftWith))]
		[DynamicDependency(nameof(SetAlignLeftWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignLeftWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignRightWith(UIElement element) => (object)element.GetValue(AlignRightWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignRightWith(UIElement element, object value) => element.SetValue(AlignRightWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignRightWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignRightWithProperty
	{
		[DynamicDependency(nameof(GetAlignRightWith))]
		[DynamicDependency(nameof(SetAlignRightWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignRightWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignTopWith(UIElement element) => (object)element.GetValue(AlignTopWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignTopWith(UIElement element, object value) => element.SetValue(AlignTopWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignTopWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignTopWithProperty
	{
		[DynamicDependency(nameof(GetAlignTopWith))]
		[DynamicDependency(nameof(SetAlignTopWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignTopWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignHorizontalCenterWith(UIElement element) => (object)element.GetValue(AlignHorizontalCenterWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignHorizontalCenterWith(UIElement element, object value) => element.SetValue(AlignHorizontalCenterWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignHorizontalCenterWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignHorizontalCenterWithProperty
	{
		[DynamicDependency(nameof(GetAlignHorizontalCenterWith))]
		[DynamicDependency(nameof(SetAlignHorizontalCenterWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignHorizontalCenterWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignVerticalCenterWith(UIElement element) => (object)element.GetValue(AlignVerticalCenterWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignVerticalCenterWith(UIElement element, object value) => element.SetValue(AlignVerticalCenterWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignVerticalCenterWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignVerticalCenterWithProperty
	{
		[DynamicDependency(nameof(GetAlignVerticalCenterWith))]
		[DynamicDependency(nameof(SetAlignVerticalCenterWith))]
		get;
	} = DependencyProperty.RegisterAttached(
			"AlignVerticalCenterWith",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	#endregion

	#region Sibling Positional relationships

	/// <summary>
	/// Gets the value of the RelativePanel.Above XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAbove(UIElement element) => (object)element.GetValue(AboveProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.Above XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAbove(UIElement element, object value) => element.SetValue(AboveProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.Above XAML attached property.
	/// </summary>
	public static DependencyProperty AboveProperty
	{
		[DynamicDependency(nameof(GetAbove))]
		[DynamicDependency(nameof(SetAbove))]
		get;
	} = DependencyProperty.RegisterAttached(
			"Above",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.Below XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetBelow(UIElement element) => (object)element.GetValue(BelowProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.Below XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetBelow(UIElement element, object value) => element.SetValue(BelowProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.Below XAML attached property.
	/// </summary>
	public static DependencyProperty BelowProperty
	{
		[DynamicDependency(nameof(GetBelow))]
		[DynamicDependency(nameof(SetBelow))]
		get;
	} = DependencyProperty.RegisterAttached(
			"Below",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.LeftOf XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetLeftOf(UIElement element) => (object)element.GetValue(LeftOfProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.LeftOf XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetLeftOf(UIElement element, object value) => element.SetValue(LeftOfProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.LeftOf XAML attached property.
	/// </summary>
	public static DependencyProperty LeftOfProperty
	{
		[DynamicDependency(nameof(GetLeftOf))]
		[DynamicDependency(nameof(SetLeftOf))]
		get;
	} = DependencyProperty.RegisterAttached(
			"LeftOf",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.RightOf XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetRightOf(UIElement element) => (object)element.GetValue(RightOfProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.RightOf XAML attached property for the target element.
	/// </summary>
	/// <param name="element">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetRightOf(UIElement element, object value) => element.SetValue(RightOfProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.RightOf XAML attached property.
	/// </summary>
	public static DependencyProperty RightOfProperty
	{
		[DynamicDependency(nameof(GetRightOf))]
		[DynamicDependency(nameof(SetRightOf))]
		get;
	} = DependencyProperty.RegisterAttached(
			"RightOf",
			typeof(object),
			typeof(RelativePanel),
			new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.AffectsMeasure, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	#endregion

	// Uno specific: Instead of checking for RP property changes directly in UIElement, we notify here.
	private static void OnPositioningChanged(object s)
	{
		var element = s as FrameworkElement;

		if (element == null)
		{
			return;
		}

		var panel = element.Parent as RelativePanel;
		// Invalidate measure on the RelativePanel when the values of the
		// associated attached properties change so it can re-arrange
		// its children.
		if (panel != null)
		{
			panel.InvalidateMeasure();
		}
	}
}
