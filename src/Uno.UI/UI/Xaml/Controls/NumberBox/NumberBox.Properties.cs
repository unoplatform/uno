// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\Generated\NumberBox.properties.cpp, tag winui3/release/1.7.1, commit 5f27a786ac9

#pragma warning disable 109 // Member does not hide an inherited member; new keyword is not required

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;

namespace Microsoft.UI.Xaml.Controls;

partial class NumberBox
{
	/// <summary>
	/// Gets or sets a value that indicates whether the control accepts and evaluates a basic formulaic expression entered as input.
	/// </summary>
	public bool AcceptsExpression
	{
		get => (bool)GetValue(AcceptsExpressionProperty);
		set => SetValue(AcceptsExpressionProperty, value);
	}

	/// <summary>
	/// Identifies the AcceptsExpression dependency property.
	/// </summary>
	public static DependencyProperty AcceptsExpressionProperty { get; } =
		DependencyProperty.Register(
			nameof(AcceptsExpression),
			typeof(bool),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(false, (s, e) => (s as NumberBox)?.OnAcceptsExpressionPropertyChanged(e)));

	/// <summary>
	/// Gets or sets content that is shown below the control. The content should provide guidance about the input expected by the control.
	/// </summary>
	public new object Description
	{
		get => GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	/// <summary>
	/// Identifies the Description dependency property.
	/// </summary>
	public static DependencyProperty DescriptionProperty { get; } =
		DependencyProperty.Register(
			nameof(Description),
			typeof(object),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the content for the control's header.
	/// </summary>
	public object Header
	{
		get => (object)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null, (s, e) => (s as NumberBox)?.OnHeaderPropertyChanged(e)));


	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the control's header.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplate),
			typeof(DataTemplate),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null, options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, (s, e) => (s as NumberBox)?.OnHeaderTemplatePropertyChanged(e)));

	/// <summary>
	/// Gets or sets the input scope for the control.
	/// </summary>
	public InputScope InputScope
	{
		get => (InputScope)GetValue(InputScopeProperty);
		set => SetValue(InputScopeProperty, value);
	}

	/// <summary>
	/// Identifies the InputScope dependency property.
	/// </summary>
	public static DependencyProperty InputScopeProperty { get; } =
		DependencyProperty.Register(nameof(InputScope), typeof(InputScope), typeof(NumberBox), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that indicates whether line breaking occurs when header text extends beyond the available width of the control.
	/// </summary>
	public bool IsWrapEnabled
	{
		get => (bool)GetValue(IsWrapEnabledProperty);
		set => SetValue(IsWrapEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsWrapEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsWrapEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsWrapEnabled),
			typeof(bool),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(false, (s, e) => (s as NumberBox)?.OnIsWrapEnabledPropertyChanged(e)));

	/// <summary>
	/// Gets or sets a number that is added to or subtracted from Value when a large change is made, such as with the PageUp and PageDown keys.
	/// </summary>
	public double LargeChange
	{
		get => (double)GetValue(LargeChangeProperty);
		set => SetValue(LargeChangeProperty, value);
	}

	/// <summary>
	/// Identifies the LargeChange dependency property.
	/// </summary>
	public static DependencyProperty LargeChangeProperty { get; } =
		DependencyProperty.Register(
			nameof(LargeChange),
			typeof(double),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(10.0));

	/// <summary>
	/// Gets or sets the numerical maximum for Value.
	/// </summary>
	public double Maximum
	{
		get => (double)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	/// <summary>
	/// Identifies the Maximum dependency property.
	/// </summary>
	public static DependencyProperty MaximumProperty { get; } =
		DependencyProperty.Register(
			nameof(Maximum),
			typeof(double),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(double.MaxValue, (s, e) => (s as NumberBox)?.OnMaximumPropertyChanged(e)));

	/// <summary>
	/// Gets or sets the numerical minimum for Value.
	/// </summary>
	public double Minimum
	{
		get => (double)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	/// <summary>
	/// Identifies the Minimum dependency property.
	/// </summary>
	public static DependencyProperty MinimumProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Minimum),
			propertyType: typeof(double),
			ownerType: typeof(NumberBox),
			typeMetadata: new FrameworkPropertyMetadata(
				defaultValue: double.MinValue,
				propertyChangedCallback: (s, e) => (s as NumberBox)?.OnMinimumPropertyChanged(e)));

	/// <summary>
	/// Gets or sets the object used to specify the formatting of Value.
	/// </summary>
	public INumberFormatter2 NumberFormatter
	{
		get => (INumberFormatter2)GetValue(NumberFormatterProperty);
		set
		{
			INumberFormatter2 coercedValue = value;
			ValidateNumberFormatter(coercedValue);
			SetValue(NumberFormatterProperty, value);
		}
	}

	/// <summary>
	/// Identifies the NumberFormatter dependency property.
	/// </summary>
	public static DependencyProperty NumberFormatterProperty { get; } =
		DependencyProperty.Register(
			nameof(NumberFormatter),
			typeof(INumberFormatter2),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null, (s, e) => (s as NumberBox)?.OnNumberFormatterPropertyChanged(e)));

	/// <summary>
	/// Gets or sets the text that is displayed in the data entry field
	/// of the control until the value is changed by a user action or some other operation.
	/// </summary>
	public string PlaceholderText
	{
		get => (string)GetValue(PlaceholderTextProperty);
		set => SetValue(PlaceholderTextProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderText dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderTextProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderText),
			typeof(string),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(string.Empty));

	/// <summary>
	/// Gets or sets a value that indicates whether the on-screen keyboard is shown when the control receives focus programmatically.
	/// </summary>
	public bool PreventKeyboardDisplayOnProgrammaticFocus
	{
		get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
		set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
	}

	/// <summary>
	/// Identifies the PreventKeyboardDisplayOnProgrammaticFocus dependency property.
	/// </summary>
	public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } =
		DependencyProperty.Register(
			nameof(PreventKeyboardDisplayOnProgrammaticFocus),
			typeof(bool),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets the flyout that is shown when text is selected, or null if no flyout is shown.
	/// </summary>
	[Uno.NotImplemented] // TODO:
	public FlyoutBase SelectionFlyout
	{
		get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
		set => SetValue(SelectionFlyoutProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionFlyout dependency property.
	/// </summary>
	public static DependencyProperty SelectionFlyoutProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionFlyout),
			typeof(FlyoutBase),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.LogicalChild));

	/// <summary>
	/// Gets or sets the brush used to highlight the selected text.
	/// </summary>
	public SolidColorBrush SelectionHighlightColor
	{
		get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
		set => SetValue(SelectionHighlightColorProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionHighlightColor dependency property.
	/// </summary>
	public static DependencyProperty SelectionHighlightColorProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionHighlightColor),
			typeof(SolidColorBrush),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a number that is added to or subtracted from Value when a small change is made, such as with an arrow key or scrolling.
	/// </summary>
	public double SmallChange
	{
		get => (double)GetValue(SmallChangeProperty);
		set => SetValue(SmallChangeProperty, value);
	}

	/// <summary>
	/// Identifies the SmallChange dependency property.
	/// </summary>
	public static DependencyProperty SmallChangeProperty { get; } =
		DependencyProperty.Register(
			nameof(SmallChange),
			typeof(double),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(1.0, (s, e) => (s as NumberBox)?.OnSmallChangePropertyChanged(e)));

	/// <summary>
	/// Gets or sets a value that indicates the placement of buttons used to increment or decrement the Value property.
	/// </summary>
	public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
	{
		get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
		set => SetValue(SpinButtonPlacementModeProperty, value);
	}

	/// <summary>
	/// Identifies the SpinButtonPlacementMode dependency property.
	/// </summary>
	public static DependencyProperty SpinButtonPlacementModeProperty { get; } =
		DependencyProperty.Register(
			nameof(SpinButtonPlacementMode),
			typeof(NumberBoxSpinButtonPlacementMode),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(NumberBoxSpinButtonPlacementMode.Hidden, (s, e) => (s as NumberBox)?.OnSpinButtonPlacementModePropertyChanged(e)));

	/// <summary>
	/// Gets or sets the string type representation of the Value property.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(NumberBox),
			new FrameworkPropertyMetadata("", (s, e) => (s as NumberBox)?.OnTextPropertyChanged(e)));

	/// <summary>
	/// Gets or sets a value that indicates how the text in the NumberBox is aligned.
	/// </summary>
	public new TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	/// <summary>
	/// Identifies the TextAlignment dependency property.
	/// </summary>
	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumberBox), new FrameworkPropertyMetadata(TextAlignment.Left));

	/// <summary>
	/// Gets or sets a value that indicates how the reading order is determined for the NumberBox.
	/// </summary>
	public TextReadingOrder TextReadingOrder
	{
		get => (TextReadingOrder)GetValue(TextReadingOrderProperty);
		set => SetValue(TextReadingOrderProperty, value);
	}

	/// <summary>
	/// Identifies the TextReadingOrder dependency property.
	/// </summary>
	public static DependencyProperty TextReadingOrderProperty { get; } =
		DependencyProperty.Register(
			nameof(TextReadingOrder),
			typeof(TextReadingOrder),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(TextReadingOrder.Default));

	/// <summary>
	/// Gets or sets a value that specifies the input validation behavior to invoke when invalid input is entered.
	/// </summary>
	public NumberBoxValidationMode ValidationMode
	{
		get => (NumberBoxValidationMode)GetValue(ValidationModeProperty);
		set => SetValue(ValidationModeProperty, value);
	}

	/// <summary>
	/// Identifies the ValidationMode dependency property.
	/// </summary>
	public static DependencyProperty ValidationModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ValidationMode),
			typeof(NumberBoxValidationMode),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten, (s, e) => (s as NumberBox)?.OnValidationModePropertyChanged(e)));

	/// <summary>
	/// Gets or sets the numeric value of a NumberBox.
	/// </summary>
	public double Value
	{
		get => (double)GetValue(ValueProperty);
		set
		{
			// When using two way bindings to Value using x:Bind, we could end up with a stack overflow because
			// nan != nan. However in this case, we are using nan as a value to represent value not set (cleared)
			// and that can happen quite often. We can avoid the stack overflow by breaking the cycle here. This is possible
			// for x:Bind since the generated code goes through this property setter. This is not the case for Binding
			// unfortunately. x:Bind is recommended over Binding anyway due to its perf and debuggability benefits.
			if (!value.IsNaN() || !Value.IsNaN())
			{
				SetValue(ValueProperty, value);
			}
		}
	}

	/// <summary>
	/// Identifies the Value dependency property.
	/// </summary>
	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(
			nameof(Value),
			typeof(double),
			typeof(NumberBox),
			new FrameworkPropertyMetadata(double.NaN, (s, e) => (s as NumberBox)?.OnValuePropertyChanged(e)));

	/// <summary>
	/// Occurs after the user triggers evaluation of new input by pressing the Enter key, clicking a spin button, or by changing focus.
	/// </summary>
	public event TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> ValueChanged;
}
