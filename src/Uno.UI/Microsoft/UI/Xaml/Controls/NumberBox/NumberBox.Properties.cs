#pragma warning disable 109 // Member does not hide an inherited member; new keyword is not required

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NumberBox.properties.cpp, commit 7c3ba16da9e273c73169c062cc803868695fa864

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class NumberBox : Control
	{
		public double Minimum
		{
			get => (double)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public static DependencyProperty MinimumProperty { get; } =
			DependencyProperty.Register(
				name: nameof(Minimum),
				propertyType: typeof(double),
				ownerType: typeof(NumberBox),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: -double.MaxValue,
					propertyChangedCallback: (s, e) => (s as NumberBox)?.OnMinimumPropertyChanged(e)));


		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public static DependencyProperty MaximumProperty { get; } =
			DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumberBox), new FrameworkPropertyMetadata(double.MaxValue, (s, e) => (s as NumberBox)?.OnMaximumPropertyChanged(e)));

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set
			{
				if (!Value.IsNaN() || !value.IsNaN())
				{
					SetValue(ValueProperty, value);
				}
			}
		}

		public static DependencyProperty ValueProperty { get; } =
			DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumberBox), new FrameworkPropertyMetadata(double.NaN, (s, e) => (s as NumberBox)?.OnValuePropertyChanged(e)));



		public double SmallChange
		{
			get => (double)GetValue(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		public static DependencyProperty SmallChangeProperty { get; } =
			DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumberBox), new FrameworkPropertyMetadata(1.0, (s, e) => (s as NumberBox)?.OnSmallChangePropertyChanged(e)));

		public double LargeChange
		{
			get => (double)GetValue(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		public static DependencyProperty LargeChangeProperty { get; } =
			DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NumberBox), new FrameworkPropertyMetadata(10.0));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(nameof(Text), typeof(string), typeof(NumberBox), new FrameworkPropertyMetadata("", (s, e) => (s as NumberBox)?.OnTextPropertyChanged(e)));

		// TextBox properties

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(nameof(Header), typeof(object), typeof(NumberBox), new FrameworkPropertyMetadata(null, (s, e) => (s as NumberBox)?.OnHeaderPropertyChanged(e)));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NumberBox), new FrameworkPropertyMetadata(null, options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, (s, e) => (s as NumberBox)?.OnHeaderTemplatePropertyChanged(e)));

		public InputScope InputScope
		{
			get => (InputScope)GetValue(InputScopeProperty);
			set => SetValue(InputScopeProperty, value);
		}

		public static DependencyProperty InputScopeProperty { get; } =
			DependencyProperty.Register(nameof(InputScope), typeof(InputScope), typeof(NumberBox), new FrameworkPropertyMetadata(null));

		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumberBox), new FrameworkPropertyMetadata(string.Empty));

		[Uno.NotImplemented] // TODO:
		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		public static DependencyProperty SelectionFlyoutProperty { get; } =
			DependencyProperty.Register(nameof(SelectionFlyout), typeof(FlyoutBase), typeof(NumberBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.LogicalChild));

		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(nameof(SelectionHighlightColor), typeof(SolidColorBrush), typeof(NumberBox), new FrameworkPropertyMetadata(null));

		public new TextAlignment TextAlignment
		{
			get => (TextAlignment)GetValue(TextAlignmentProperty);
			set => SetValue(TextAlignmentProperty, value);
		}

		public static DependencyProperty TextAlignmentProperty { get; } =
			DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(NumberBox), new FrameworkPropertyMetadata(TextAlignment.Left));


		public TextReadingOrder TextReadingOrder
		{
			get => (TextReadingOrder)GetValue(TextReadingOrderProperty);
			set => SetValue(TextReadingOrderProperty, value);
		}

		public static DependencyProperty TextReadingOrderProperty { get; } =
			DependencyProperty.Register(nameof(TextReadingOrder), typeof(TextReadingOrder), typeof(NumberBox), new FrameworkPropertyMetadata(null));

		public bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
		}

		public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } =
			DependencyProperty.Register(nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), typeof(NumberBox), new FrameworkPropertyMetadata(null));

		public new object Description
		{
			get => GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(nameof(Description), typeof(object), typeof(NumberBox), new FrameworkPropertyMetadata(null));

		public NumberBoxValidationMode ValidationMode
		{
			get => (NumberBoxValidationMode)GetValue(ValidationModeProperty);
			set => SetValue(ValidationModeProperty, value);
		}

		public static DependencyProperty ValidationModeProperty { get; } =
			DependencyProperty.Register(nameof(ValidationMode), typeof(NumberBoxValidationMode), typeof(NumberBox), new FrameworkPropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten, (s, e) => (s as NumberBox)?.OnValidationModePropertyChanged(e)));

		public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
		{
			get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
			set => SetValue(SpinButtonPlacementModeProperty, value);
		}

		public static DependencyProperty SpinButtonPlacementModeProperty { get; } =
			DependencyProperty.Register(nameof(SpinButtonPlacementMode), typeof(NumberBoxSpinButtonPlacementMode), typeof(NumberBox), new FrameworkPropertyMetadata(NumberBoxSpinButtonPlacementMode.Hidden, (s, e) => (s as NumberBox)?.OnSpinButtonPlacementModePropertyChanged(e)));

		public bool IsWrapEnabled
		{
			get => (bool)GetValue(IsWrapEnabledProperty);
			set => SetValue(IsWrapEnabledProperty, value);
		}

		public static DependencyProperty IsWrapEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsWrapEnabled), typeof(bool), typeof(NumberBox), new FrameworkPropertyMetadata(false, (s, e) => (s as NumberBox)?.OnIsWrapEnabledPropertyChanged(e)));

		public bool AcceptsExpression
		{
			get => (bool)GetValue(AcceptsExpressionProperty);
			set => SetValue(AcceptsExpressionProperty, value);
		}

		public static DependencyProperty AcceptsExpressionProperty { get; } =
			DependencyProperty.Register(nameof(AcceptsExpression), typeof(bool), typeof(NumberBox), new FrameworkPropertyMetadata(false, (s, e) => (s as NumberBox)?.OnAcceptsExpressionPropertyChanged(e)));

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

		public static DependencyProperty NumberFormatterProperty { get; } =
			DependencyProperty.Register(nameof(NumberFormatter), typeof(INumberFormatter2), typeof(NumberBox), new FrameworkPropertyMetadata(null, (s, e) => (s as NumberBox)?.OnNumberFormatterPropertyChanged(e)));

		public event TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> ValueChanged;
	}
}
