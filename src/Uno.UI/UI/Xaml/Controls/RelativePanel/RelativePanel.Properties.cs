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
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnBorderThicknessPropertyChanged))]
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
	[GeneratedDependencyProperty(ChangedCallbackName = nameof(OnPaddingPropertyChanged))]
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
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignBottomWithPanel(UIElement view) => (bool)view.GetValue(AlignBottomWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignBottomWithPanel(UIElement view, bool value) => view.SetValue(AlignBottomWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignBottomWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignBottomWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignBottomWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignLeftWithPanel(UIElement view) => (bool)view.GetValue(AlignLeftWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignLeftWithPanel(UIElement view, bool value) => view.SetValue(AlignLeftWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignLeftWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignLeftWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignLeftWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignRightWithPanel(UIElement view) => (bool)view.GetValue(AlignRightWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignRightWithPanel(UIElement view, bool value) => view.SetValue(AlignRightWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignRightWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignRightWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignRightWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignTopWithPanel(UIElement view) => (bool)view.GetValue(AlignTopWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignTopWithPanel(UIElement view, bool value) => view.SetValue(AlignTopWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignTopWithPanelProperty XAML attached property.
	/// </summary>
	public static DependencyProperty AlignTopWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignTopWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignHorizontalCenterWithPanel(UIElement view) => (bool)view.GetValue(AlignHorizontalCenterWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignHorizontalCenterWithPanel(UIElement view, bool value) => view.SetValue(AlignHorizontalCenterWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property.
	/// </summary>
	public static DependencyProperty AlignHorizontalCenterWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignHorizontalCenterWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static bool GetAlignVerticalCenterWithPanel(UIElement view) => (bool)view.GetValue(AlignVerticalCenterWithPanelProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignVerticalCenterWithPanel(UIElement view, bool value) => view.SetValue(AlignVerticalCenterWithPanelProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignVerticalCenterWithPanel XAML attached property.
	/// </summary>
	public static DependencyProperty AlignVerticalCenterWithPanelProperty { get; } =
		DependencyProperty.RegisterAttached("AlignVerticalCenterWithPanel", typeof(bool), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	#endregion

	#region Sibling Alignment relationships

	/// <summary>
	/// Gets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignBottomWith(UIElement view) => view.GetValue(AlignBottomWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignBottomWith(UIElement view, object value) => view.SetValue(AlignBottomWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignBottomWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignBottomWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignBottomWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignLeftWith(UIElement view) => (object)view.GetValue(AlignLeftWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignLeftWith(UIElement view, object value) => view.SetValue(AlignLeftWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignLeftWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignLeftWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignLeftWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignRightWith(UIElement view) => (object)view.GetValue(AlignRightWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignRightWith(UIElement view, object value) => view.SetValue(AlignRightWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignRightWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignRightWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignRightWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignTopWith(UIElement view) => (object)view.GetValue(AlignTopWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignTopWith(UIElement view, object value) => view.SetValue(AlignTopWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignTopWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignTopWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignTopWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignHorizontalCenterWith(UIElement view) => (object)view.GetValue(AlignHorizontalCenterWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignHorizontalCenterWith(UIElement view, object value) => view.SetValue(AlignHorizontalCenterWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignHorizontalCenterWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignHorizontalCenterWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignHorizontalCenterWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAlignVerticalCenterWith(UIElement view) => (object)view.GetValue(AlignVerticalCenterWithProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAlignVerticalCenterWith(UIElement view, object value) => view.SetValue(AlignVerticalCenterWithProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.AlignVerticalCenterWith XAML attached property.
	/// </summary>
	public static DependencyProperty AlignVerticalCenterWithProperty { get; } =
		DependencyProperty.RegisterAttached("AlignVerticalCenterWith", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	#endregion

	#region Sibling Positional relationships

	/// <summary>
	/// Gets the value of the RelativePanel.Above XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetAbove(UIElement view) => (object)view.GetValue(AboveProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.Above XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetAbove(UIElement view, object value) => view.SetValue(AboveProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.Above XAML attached property.
	/// </summary>
	public static DependencyProperty AboveProperty { get; } =
		DependencyProperty.RegisterAttached("Above", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.Below XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetBelow(UIElement view) => (object)view.GetValue(BelowProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.Below XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetBelow(UIElement view, object value) => view.SetValue(BelowProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.Below XAML attached property.
	/// </summary>
	public static DependencyProperty BelowProperty { get; } =
		DependencyProperty.RegisterAttached("Below", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.LeftOf XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetLeftOf(UIElement view) => (object)view.GetValue(LeftOfProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.LeftOf XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetLeftOf(UIElement view, object value) => view.SetValue(LeftOfProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.LeftOf XAML attached property.
	/// </summary>
	public static DependencyProperty LeftOfProperty { get; } =
		DependencyProperty.RegisterAttached("LeftOf", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

	/// <summary>
	/// Gets the value of the RelativePanel.RightOf XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <returns>Value.</returns>
	public static object GetRightOf(UIElement view) => (object)view.GetValue(RightOfProperty);

	/// <summary>
	/// Sets the value of the RelativePanel.RightOf XAML attached property for the target element.
	/// </summary>
	/// <param name="view">Target element.</param>
	/// <param name="value">Value.</param>
	public static void SetRightOf(UIElement view, object value) => view.SetValue(RightOfProperty, value);

	/// <summary>
	/// Identifies the RelativePanel.RightOf XAML attached property.
	/// </summary>
	public static DependencyProperty RightOfProperty { get; } =
		DependencyProperty.RegisterAttached("RightOf", typeof(object), typeof(RelativePanel), new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => OnPositioningChanged(s)));

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
