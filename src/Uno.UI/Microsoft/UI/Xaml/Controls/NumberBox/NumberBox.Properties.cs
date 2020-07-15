#pragma warning disable 109

using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.Helpers.WinUI;
using Windows.Globalization.NumberFormatting;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class NumberBox : Control
	{
		public double Minimum
		{
			get => (double)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public static DependencyProperty MinimumProperty { get ; } =
			DependencyProperty.Register(
				name: nameof(Minimum),
				propertyType: typeof(double),
				ownerType: typeof(NumberBox),
				typeMetadata: new PropertyMetadata(
					defaultValue: -double.MaxValue,
					propertyChangedCallback: (s, e) => (s as NumberBox)?.OnMinimumPropertyChanged(e)));


		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public static DependencyProperty MaximumProperty { get ; } =
			DependencyProperty.Register("Maximum", typeof(double), typeof(NumberBox), new PropertyMetadata(double.MaxValue, (s, e) => (s as NumberBox)?.OnMaximumPropertyChanged(e)));

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public static DependencyProperty ValueProperty { get ; } =
			DependencyProperty.Register("Value", typeof(double), typeof(NumberBox), new PropertyMetadata(0.0, (s, e) => (s as NumberBox)?.OnValuePropertyChanged(e)));



		public double SmallChange
		{
			get => (double)GetValue(SmallChangeProperty);
			set => SetValue(SmallChangeProperty, value);
		}

		public static DependencyProperty SmallChangeProperty { get ; } =
			DependencyProperty.Register("SmallChange", typeof(double), typeof(NumberBox), new PropertyMetadata(1.0, (s, e) => (s as NumberBox)?.OnSmallChangePropertyChanged(e)));

		public double LargeChange
		{
			get => (double)GetValue(LargeChangeProperty);
			set => SetValue(LargeChangeProperty, value);
		}

		public static DependencyProperty LargeChangeProperty { get ; } =
			DependencyProperty.Register("LargeChange", typeof(double), typeof(NumberBox), new PropertyMetadata(10.0));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public static DependencyProperty TextProperty { get ; } =
			DependencyProperty.Register("Text", typeof(string), typeof(NumberBox), new PropertyMetadata("", (s, e) => (s as NumberBox)?.OnTextPropertyChanged(e)));

		// TextBox properties

		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get ; } =
			DependencyProperty.Register("Header", typeof(object), typeof(NumberBox), new PropertyMetadata(null));

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get ; } =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(NumberBox), new PropertyMetadata(null));

		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		public static DependencyProperty PlaceholderTextProperty { get ; } =
			DependencyProperty.Register("PlaceholderText", typeof(string), typeof(NumberBox), new PropertyMetadata(null));

		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		public static DependencyProperty SelectionFlyoutProperty { get ; } =
			DependencyProperty.Register("SelectionFlyout", typeof(FlyoutBase), typeof(NumberBox), new PropertyMetadata(null));

		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorProperty { get ; } =
			DependencyProperty.Register("SelectionHighlightColor", typeof(SolidColorBrush), typeof(NumberBox), new PropertyMetadata(null));

		public TextReadingOrder TextReadingOrder
		{
			get => (TextReadingOrder)GetValue(TextReadingOrderProperty);
			set => SetValue(TextReadingOrderProperty, value);
		}

		public static DependencyProperty TextReadingOrderProperty { get ; } =
			DependencyProperty.Register("TextReadingOrder", typeof(TextReadingOrder), typeof(NumberBox), new PropertyMetadata(null));

		public bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
		}

		public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get ; } =
			DependencyProperty.Register("PreventKeyboardDisplayOnProgrammaticFocus", typeof(bool), typeof(NumberBox), new PropertyMetadata(null));

		public new object Description
		{
			get => (object)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get ; } =
			DependencyProperty.Register("Description", typeof(object), typeof(NumberBox), new PropertyMetadata(null));

		public NumberBoxValidationMode ValidationMode
		{
			get => (NumberBoxValidationMode)GetValue(ValidationModeProperty);
			set => SetValue(ValidationModeProperty, value);
		}

		public static DependencyProperty ValidationModeProperty { get ; } =
			DependencyProperty.Register("ValidationMode", typeof(NumberBoxValidationMode), typeof(NumberBox), new PropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten, (s, e) => (s as NumberBox)?.OnValidationModePropertyChanged(e)));

		public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
		{
			get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
			set => SetValue(SpinButtonPlacementModeProperty, value);
		}

		public static DependencyProperty SpinButtonPlacementModeProperty { get ; } =
			DependencyProperty.Register("SpinButtonPlacementMode", typeof(NumberBoxSpinButtonPlacementMode), typeof(NumberBox), new PropertyMetadata(NumberBoxSpinButtonPlacementMode.Hidden, (s, e) => (s as NumberBox)?.OnSpinButtonPlacementModePropertyChanged(e)));

		public bool IsWrapEnabled
		{
			get => (bool)GetValue(IsWrapEnabledProperty);
			set => SetValue(IsWrapEnabledProperty, value);
		}

		public static DependencyProperty IsWrapEnabledProperty { get ; } =
			DependencyProperty.Register("IsWrapEnabled", typeof(bool), typeof(NumberBox), new PropertyMetadata(false, (s, e) => (s as NumberBox)?.OnIsWrapEnabledPropertyChanged(e)));

		public bool AcceptsExpression
		{
			get => (bool)GetValue(AcceptsExpressionProperty);
			set => SetValue(AcceptsExpressionProperty, value);
		}

		public static DependencyProperty AcceptsExpressionProperty { get ; } =
			DependencyProperty.Register("AcceptsExpression", typeof(bool), typeof(NumberBox), new PropertyMetadata(false /* ,UNO TODO (s, e) => (s as NumberBox)?.OnAcceptsExpressionPropertyChanged(e)*/));

		public INumberFormatter2 NumberFormatter
		{
			get => (INumberFormatter2)GetValue(NumberFormatterProperty);
			set => SetValue(NumberFormatterProperty, value);
		}

		public static DependencyProperty NumberFormatterProperty { get ; } =
			DependencyProperty.Register("NumberFormatter", typeof(INumberFormatter2), typeof(NumberBox), new PropertyMetadata(null, (s, e) => (s as NumberBox)?.OnNumberFormatterPropertyChanged(e)));

		public event Windows.Foundation.TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> ValueChanged;
	}
}
