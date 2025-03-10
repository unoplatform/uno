using System;
using Windows.Globalization;
using Windows.UI.Text;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView : global::Windows.UI.Xaml.Controls.Control
	{
		public global::Windows.UI.Xaml.HorizontalAlignment HorizontalFirstOfMonthLabelAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.HorizontalAlignment)this.GetValue(HorizontalFirstOfMonthLabelAlignmentProperty);
			}
			set
			{
				this.SetValue(HorizontalFirstOfMonthLabelAlignmentProperty, value);
			}
		}

		public global::Windows.UI.Xaml.HorizontalAlignment HorizontalDayItemAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.HorizontalAlignment)this.GetValue(HorizontalDayItemAlignmentProperty);
			}
			set
			{
				this.SetValue(HorizontalDayItemAlignmentProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush FocusBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(FocusBorderBrushProperty);
			}
			set
			{
				this.SetValue(FocusBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Text.FontWeight FirstOfYearDecadeLabelFontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(FirstOfYearDecadeLabelFontWeightProperty);
			}
			set
			{
				this.SetValue(FirstOfYearDecadeLabelFontWeightProperty, value);
			}
		}

		public global::Windows.UI.Text.FontStyle FirstOfYearDecadeLabelFontStyle
		{
			get
			{
				return (global::Windows.UI.Text.FontStyle)this.GetValue(FirstOfYearDecadeLabelFontStyleProperty);
			}
			set
			{
				this.SetValue(FirstOfYearDecadeLabelFontStyleProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.FontFamily MonthYearItemFontFamily
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FontFamily)this.GetValue(MonthYearItemFontFamilyProperty);
			}
			set
			{
				this.SetValue(MonthYearItemFontFamilyProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.FontFamily FirstOfYearDecadeLabelFontFamily
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FontFamily)this.GetValue(FirstOfYearDecadeLabelFontFamilyProperty);
			}
			set
			{
				this.SetValue(FirstOfYearDecadeLabelFontFamilyProperty, value);
			}
		}

		public global::Windows.UI.Text.FontWeight FirstOfMonthLabelFontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(FirstOfMonthLabelFontWeightProperty);
			}
			set
			{
				this.SetValue(FirstOfMonthLabelFontWeightProperty, value);
			}
		}

		public global::Windows.UI.Text.FontStyle FirstOfMonthLabelFontStyle
		{
			get
			{
				return (global::Windows.UI.Text.FontStyle)this.GetValue(FirstOfMonthLabelFontStyleProperty);
			}
			set
			{
				this.SetValue(FirstOfMonthLabelFontStyleProperty, value);
			}
		}

		public double FirstOfMonthLabelFontSize
		{
			get
			{
				return (double)this.GetValue(FirstOfMonthLabelFontSizeProperty);
			}
			set
			{
				this.SetValue(FirstOfMonthLabelFontSizeProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.FontFamily FirstOfMonthLabelFontFamily
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FontFamily)this.GetValue(FirstOfMonthLabelFontFamilyProperty);
			}
			set
			{
				this.SetValue(FirstOfMonthLabelFontFamilyProperty, value);
			}
		}

		public global::Windows.Globalization.DayOfWeek FirstDayOfWeek
		{
			get
			{
				return (global::Windows.Globalization.DayOfWeek)this.GetValue(FirstDayOfWeekProperty);
			}
			set
			{
				this.SetValue(FirstDayOfWeekProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush BlackoutForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(BlackoutForegroundProperty);
			}
			set
			{
				this.SetValue(BlackoutForegroundProperty, value);
			}
		}

		public string DayOfWeekFormat
		{
			get
			{
				return (string)this.GetValue(DayOfWeekFormatProperty) ?? "";
			}
			set
			{
				this.SetValue(DayOfWeekFormatProperty, value);
			}
		}

		public global::Windows.UI.Text.FontWeight DayItemFontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(DayItemFontWeightProperty);
			}
			set
			{
				this.SetValue(DayItemFontWeightProperty, value);
			}
		}

		public global::Windows.UI.Text.FontStyle DayItemFontStyle
		{
			get
			{
				return (global::Windows.UI.Text.FontStyle)this.GetValue(DayItemFontStyleProperty);
			}
			set
			{
				this.SetValue(DayItemFontStyleProperty, value);
			}
		}

		public double DayItemFontSize
		{
			get
			{
				return (double)this.GetValue(DayItemFontSizeProperty);
			}
			set
			{
				this.SetValue(DayItemFontSizeProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.FontFamily DayItemFontFamily
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.FontFamily)this.GetValue(DayItemFontFamilyProperty);
			}
			set
			{
				this.SetValue(DayItemFontFamilyProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush SelectedPressedBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(SelectedPressedBorderBrushProperty);
			}
			set
			{
				this.SetValue(SelectedPressedBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush CalendarItemForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(CalendarItemForegroundProperty);
			}
			set
			{
				this.SetValue(CalendarItemForegroundProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Thickness CalendarItemBorderThickness
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(CalendarItemBorderThicknessProperty);
			}
			set
			{
				this.SetValue(CalendarItemBorderThicknessProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush CalendarItemBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(CalendarItemBorderBrushProperty);
			}
			set
			{
				this.SetValue(CalendarItemBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush CalendarItemBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(CalendarItemBackgroundProperty);
			}
			set
			{
				this.SetValue(CalendarItemBackgroundProperty, value);
			}
		}

		public string CalendarIdentifier
		{
			get
			{
				return (string)this.GetValue(CalendarIdentifierProperty);
			}
			set
			{
				this.SetValue(CalendarIdentifierProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Controls.CalendarViewDisplayMode DisplayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CalendarViewDisplayMode)this.GetValue(DisplayModeProperty);
			}
			set
			{
				this.SetValue(DisplayModeProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush HoverBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(HoverBorderBrushProperty);
			}
			set
			{
				this.SetValue(HoverBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush PressedBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(PressedBorderBrushProperty);
			}
			set
			{
				this.SetValue(PressedBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.VerticalAlignment VerticalDayItemAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.VerticalAlignment)this.GetValue(VerticalDayItemAlignmentProperty);
			}
			set
			{
				this.SetValue(VerticalDayItemAlignmentProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush TodayForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(TodayForegroundProperty);
			}
			set
			{
				this.SetValue(TodayForegroundProperty, value);
			}
		}

		public global::Windows.UI.Text.FontWeight TodayFontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(TodayFontWeightProperty);
			}
			set
			{
				this.SetValue(TodayFontWeightProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Controls.CalendarViewSelectionMode SelectionMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.CalendarViewSelectionMode)this.GetValue(SelectionModeProperty);
			}
			set
			{
				this.SetValue(SelectionModeProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Style CalendarViewDayItemStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(CalendarViewDayItemStyleProperty);
			}
			set
			{
				this.SetValue(CalendarViewDayItemStyleProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush SelectedHoverBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(SelectedHoverBorderBrushProperty);
			}
			set
			{
				this.SetValue(SelectedHoverBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush SelectedForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(SelectedForegroundProperty);
			}
			set
			{
				this.SetValue(SelectedForegroundProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush SelectedBorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(SelectedBorderBrushProperty);
			}
			set
			{
				this.SetValue(SelectedBorderBrushProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush PressedForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(PressedForegroundProperty);
			}
			set
			{
				this.SetValue(PressedForegroundProperty, value);
			}
		}

		public global::Windows.UI.Xaml.VerticalAlignment VerticalFirstOfMonthLabelAlignment
		{
			get
			{
				return (global::Windows.UI.Xaml.VerticalAlignment)this.GetValue(VerticalFirstOfMonthLabelAlignmentProperty);
			}
			set
			{
				this.SetValue(VerticalFirstOfMonthLabelAlignmentProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush OutOfScopeForeground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(OutOfScopeForegroundProperty);
			}
			set
			{
				this.SetValue(OutOfScopeForegroundProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Media.Brush OutOfScopeBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(OutOfScopeBackgroundProperty);
			}
			set
			{
				this.SetValue(OutOfScopeBackgroundProperty, value);
			}
		}

		public int NumberOfWeeksInView
		{
			get
			{
				return (int)this.GetValue(NumberOfWeeksInViewProperty);
			}
			set
			{
				this.SetValue(NumberOfWeeksInViewProperty, value);
			}
		}

		public global::Windows.UI.Text.FontWeight MonthYearItemFontWeight
		{
			get
			{
				return (global::Windows.UI.Text.FontWeight)this.GetValue(MonthYearItemFontWeightProperty);
			}
			set
			{
				this.SetValue(MonthYearItemFontWeightProperty, value);
			}
		}

		public global::Windows.UI.Text.FontStyle MonthYearItemFontStyle
		{
			get
			{
				return (global::Windows.UI.Text.FontStyle)this.GetValue(MonthYearItemFontStyleProperty);
			}
			set
			{
				this.SetValue(MonthYearItemFontStyleProperty, value);
			}
		}

		public double MonthYearItemFontSize
		{
			get
			{
				return (double)this.GetValue(MonthYearItemFontSizeProperty);
			}
			set
			{
				this.SetValue(MonthYearItemFontSizeProperty, value);
			}
		}

		public double FirstOfYearDecadeLabelFontSize
		{
			get
			{
				return (double)this.GetValue(FirstOfYearDecadeLabelFontSizeProperty);
			}
			set
			{
				this.SetValue(FirstOfYearDecadeLabelFontSizeProperty, value);
			}
		}

		public global::System.DateTimeOffset MinDate
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(MinDateProperty);
			}
			set
			{
				this.SetValue(MinDateProperty, value);
			}
		}

		public global::System.DateTimeOffset MaxDate
		{
			get
			{
				return (global::System.DateTimeOffset)this.GetValue(MaxDateProperty);
			}
			set
			{
				this.SetValue(MaxDateProperty, value);
			}
		}

		public bool IsTodayHighlighted
		{
			get
			{
				return (bool)this.GetValue(IsTodayHighlightedProperty);
			}
			set
			{
				this.SetValue(IsTodayHighlightedProperty, value);
			}
		}

		public bool IsOutOfScopeEnabled
		{
			get
			{
				return (bool)this.GetValue(IsOutOfScopeEnabledProperty);
			}
			set
			{
				this.SetValue(IsOutOfScopeEnabledProperty, value);
			}
		}

		public bool IsGroupLabelVisible
		{
			get
			{
				return (bool)this.GetValue(IsGroupLabelVisibleProperty);
			}
			set
			{
				this.SetValue(IsGroupLabelVisibleProperty, value);
			}
		}

		public global::System.Collections.Generic.IList<global::System.DateTimeOffset> SelectedDates
		{
			get
			{
				return (global::System.Collections.Generic.IList<global::System.DateTimeOffset>)this.GetValue(SelectedDatesProperty);
			}
			private set
			{
				this.SetValue(SelectedDatesProperty, value);
			}
		}

		public global::Windows.UI.Xaml.Controls.Primitives.CalendarViewTemplateSettings TemplateSettings
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.CalendarViewTemplateSettings)this.GetValue(TemplateSettingsProperty);
			}
			private set
			{
				this.SetValue(TemplateSettingsProperty, value);
			}
		}

		#region Uno-only Properties

		/// <summary>
		/// Uno specific property
		/// </summary>
		public global::Windows.UI.Xaml.CornerRadius DayItemCornerRadius
		{
			get
			{
				return (global::Windows.UI.Xaml.CornerRadius)this.GetValue(DayItemCornerRadiusProperty);
			}
			set
			{
				this.SetValue(DayItemCornerRadiusProperty, value);
			}
		}

		/// <summary>
		/// Uno specific property
		/// </summary>
		public global::Windows.UI.Xaml.CornerRadius CalendarItemCornerRadius
		{
			get
			{
				return (global::Windows.UI.Xaml.CornerRadius)this.GetValue(CalendarItemCornerRadiusProperty);
			}
			set
			{
				this.SetValue(CalendarItemCornerRadiusProperty, value);
			}
		}

		/// <summary>
		/// Uno specific property
		/// </summary>
		public global::Windows.UI.Xaml.Media.Brush SelectedBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(SelectedBackgroundProperty);
			}
			set
			{
				this.SetValue(SelectedBackgroundProperty, value);
			}
		}

		/// <summary>
		/// Uno specific property
		/// </summary>
		public global::Windows.UI.Xaml.Media.Brush TodayBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(TodayBackgroundProperty);
			}
			set
			{
				this.SetValue(TodayBackgroundProperty, value);
			}
		}

		/// <summary>
		/// Uno specific property
		/// </summary>
		public global::Windows.UI.Xaml.Media.Brush TodaySelectedBackground
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(TodaySelectedBackgroundProperty);
			}
			set
			{
				this.SetValue(TodaySelectedBackgroundProperty, value);
			}
		}
		#endregion

		public static global::Windows.UI.Xaml.DependencyProperty BlackoutForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(BlackoutForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarIdentifierProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarIdentifier), typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata("GregorianCalendar"));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarItemBackgroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarItemBackground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarItemBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarItemBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarItemBorderThicknessProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarItemBorderThickness), typeof(global::Windows.UI.Xaml.Thickness),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(Thickness.Empty));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarItemForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarItemForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty CalendarViewDayItemStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarViewDayItemStyle), typeof(global::Windows.UI.Xaml.Style),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));

		public static global::Windows.UI.Xaml.DependencyProperty DayItemFontFamilyProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayItemFontFamily), typeof(global::Windows.UI.Xaml.Media.FontFamily),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata((global::Windows.UI.Xaml.Media.FontFamily)"XamlAutoFontFamily"));

		public static global::Windows.UI.Xaml.DependencyProperty DayItemFontSizeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayItemFontSize), typeof(double),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(20.0));

		public static global::Windows.UI.Xaml.DependencyProperty DayItemFontStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayItemFontStyle), typeof(global::Windows.UI.Text.FontStyle),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontStyle.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty DayItemFontWeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayItemFontWeight), typeof(global::Windows.UI.Text.FontWeight),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontWeights.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty DayOfWeekFormatProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayOfWeekFormat), typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(string)));

		public static global::Windows.UI.Xaml.DependencyProperty DisplayModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DisplayMode), typeof(global::Windows.UI.Xaml.Controls.CalendarViewDisplayMode),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.CalendarViewDisplayMode)));

		public static global::Windows.UI.Xaml.DependencyProperty FirstDayOfWeekProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstDayOfWeek), typeof(global::Windows.Globalization.DayOfWeek),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.Globalization.DayOfWeek)));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfMonthLabelFontFamilyProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfMonthLabelFontFamily), typeof(global::Windows.UI.Xaml.Media.FontFamily),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata((global::Windows.UI.Xaml.Media.FontFamily)"XamlAutoFontFamily"));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfMonthLabelFontSizeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfMonthLabelFontSize), typeof(double),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(12.0));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfMonthLabelFontStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfMonthLabelFontStyle), typeof(global::Windows.UI.Text.FontStyle),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontStyle.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfMonthLabelFontWeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfMonthLabelFontWeight), typeof(global::Windows.UI.Text.FontWeight),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontWeights.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfYearDecadeLabelFontFamilyProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfYearDecadeLabelFontFamily), typeof(global::Windows.UI.Xaml.Media.FontFamily),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata((global::Windows.UI.Xaml.Media.FontFamily)"XamlAutoFontFamily"));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfYearDecadeLabelFontSizeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfYearDecadeLabelFontSize), typeof(double),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(12.0));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfYearDecadeLabelFontStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfYearDecadeLabelFontStyle), typeof(global::Windows.UI.Text.FontStyle),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontStyle.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty FirstOfYearDecadeLabelFontWeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FirstOfYearDecadeLabelFontWeight), typeof(global::Windows.UI.Text.FontWeight),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontWeights.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty FocusBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FocusBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty HorizontalDayItemAlignmentProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HorizontalDayItemAlignment), typeof(global::Windows.UI.Xaml.HorizontalAlignment),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(HorizontalAlignment.Center));

		public static global::Windows.UI.Xaml.DependencyProperty HorizontalFirstOfMonthLabelAlignmentProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HorizontalFirstOfMonthLabelAlignment), typeof(global::Windows.UI.Xaml.HorizontalAlignment),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(HorizontalAlignment.Center));

		public static global::Windows.UI.Xaml.DependencyProperty HoverBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(HoverBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty IsGroupLabelVisibleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsGroupLabelVisible), typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(false));

		public static global::Windows.UI.Xaml.DependencyProperty IsOutOfScopeEnabledProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsOutOfScopeEnabled), typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(true));

		public static global::Windows.UI.Xaml.DependencyProperty IsTodayHighlightedProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsTodayHighlighted), typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(true));

		public static global::Windows.UI.Xaml.DependencyProperty MaxDateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MaxDate), typeof(global::System.DateTimeOffset),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(GetDefaultMaxDate()));

		private static Calendar _gregorianCalendar;

		private const int DEFAULT_MIN_MAX_DATE_YEAR_OFFSET = 100;

		private static Calendar GetOrCreateGregorianCalendar()
		{
			if (_gregorianCalendar is null)
			{
				var tempCalendar = new Calendar();
				_gregorianCalendar = new Calendar(
					tempCalendar.Languages,
					"GregorianCalendar",
					tempCalendar.GetClock());
			}

			return _gregorianCalendar;
		}

		private static DateTime ClampDate(
			DateTime date,
			DateTime minDate,
			DateTime maxDate)
		{
			return date.UniversalTime < minDate.UniversalTime ? minDate : date.UniversalTime > maxDate.UniversalTime ? maxDate : date;
		}

		private static DateTimeOffset GetDefaultMaxDate()
		{
			// default maxdate is December 31st 100 years in the future ==> That the comment on UWP ... but actually it's 200 years!
			// cf. DependencyProperty.cpp

			var calendar = GetOrCreateGregorianCalendar();
			calendar.SetToMin();
			var minCalendarDate = calendar.GetDateTime();
			calendar.SetToMax();
			var maxCalendarDate = calendar.GetDateTime();

			//Default value is today's date plus 100 Gregorian years.
			calendar.SetToday();
			calendar.AddYears(DEFAULT_MIN_MAX_DATE_YEAR_OFFSET);
			calendar.Month = calendar.LastMonthInThisYear;
			calendar.Day = calendar.LastDayInThisMonth;
			var maxDate = calendar.GetDateTime();

			return ClampDate(maxDate, minCalendarDate, maxCalendarDate);
		}

		public static global::Windows.UI.Xaml.DependencyProperty MinDateProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MinDate), typeof(global::System.DateTimeOffset),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(GetDefaultMinDate()));

		private static DateTimeOffset GetDefaultMinDate()
		{
			// default mindate is January 1st, 100 years ago
			// cf. DependencyProperty.cpp

			var calendar = GetOrCreateGregorianCalendar();
			calendar.SetToMin();
			var minCalendarDate = calendar.GetDateTime();
			calendar.SetToMax();
			var maxCalendarDate = calendar.GetDateTime();

			//Default value is today's date minus 100 Gregorian years.
			calendar.SetToday();
			calendar.AddYears(-DEFAULT_MIN_MAX_DATE_YEAR_OFFSET);
			calendar.Month = calendar.FirstMonthInThisYear;
			calendar.Day = calendar.FirstDayInThisMonth;
			var minDate = calendar.GetDateTime();

			return ClampDate(minDate, minCalendarDate, maxCalendarDate);
		}

		public static global::Windows.UI.Xaml.DependencyProperty MonthYearItemFontFamilyProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MonthYearItemFontFamily), typeof(global::Windows.UI.Xaml.Media.FontFamily),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata((global::Windows.UI.Xaml.Media.FontFamily)"XamlAutoFontFamily"));

		public static global::Windows.UI.Xaml.DependencyProperty MonthYearItemFontSizeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MonthYearItemFontSize), typeof(double),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(20.0));

		public static global::Windows.UI.Xaml.DependencyProperty MonthYearItemFontStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MonthYearItemFontStyle), typeof(global::Windows.UI.Text.FontStyle),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontStyle.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty MonthYearItemFontWeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(MonthYearItemFontWeight), typeof(global::Windows.UI.Text.FontWeight),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontWeights.Normal));

		public static global::Windows.UI.Xaml.DependencyProperty NumberOfWeeksInViewProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(NumberOfWeeksInView), typeof(int),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(s_defaultNumberOfWeeks));

		public static global::Windows.UI.Xaml.DependencyProperty OutOfScopeBackgroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(OutOfScopeBackground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty OutOfScopeForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(OutOfScopeForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty PressedBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PressedBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty PressedForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PressedForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectedBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectedDatesProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedDates), typeof(global::System.Collections.Generic.IList<global::System.DateTimeOffset>),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::System.Collections.Generic.IList<global::System.DateTimeOffset>)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectedForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectedHoverBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedHoverBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectedPressedBorderBrushProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedPressedBorderBrush), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty SelectionModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectionMode), typeof(global::Windows.UI.Xaml.Controls.CalendarViewSelectionMode),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(global::Windows.UI.Xaml.Controls.CalendarViewSelectionMode.Single));

		public static global::Windows.UI.Xaml.DependencyProperty TemplateSettingsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TemplateSettings), typeof(global::Windows.UI.Xaml.Controls.Primitives.CalendarViewTemplateSettings),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.CalendarViewTemplateSettings)));

		public static global::Windows.UI.Xaml.DependencyProperty TodayFontWeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TodayFontWeight), typeof(global::Windows.UI.Text.FontWeight),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(FontWeights.SemiBold));

		public static global::Windows.UI.Xaml.DependencyProperty TodayForegroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TodayForeground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		public static global::Windows.UI.Xaml.DependencyProperty VerticalDayItemAlignmentProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(VerticalDayItemAlignment), typeof(global::Windows.UI.Xaml.VerticalAlignment),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(VerticalAlignment.Center));

		public static global::Windows.UI.Xaml.DependencyProperty VerticalFirstOfMonthLabelAlignmentProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(VerticalFirstOfMonthLabelAlignment), typeof(global::Windows.UI.Xaml.VerticalAlignment),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(VerticalAlignment.Top));

		#region Uno-only DependencyProperties
		/// <summary>
		/// Uno specific property
		/// </summary>
		public static global::Windows.UI.Xaml.DependencyProperty DayItemCornerRadiusProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DayItemCornerRadius), typeof(global::Windows.UI.Xaml.CornerRadius),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(CornerRadius.None));

		/// <summary>
		/// Uno specific property
		/// </summary>
		public static global::Windows.UI.Xaml.DependencyProperty CalendarItemCornerRadiusProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CalendarItemCornerRadius), typeof(global::Windows.UI.Xaml.CornerRadius),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(CornerRadius.None));

		/// <summary>
		/// Uno specific property
		/// </summary>
		public static global::Windows.UI.Xaml.DependencyProperty SelectedBackgroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectedBackground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		/// <summary>
		/// Uno specific property
		/// </summary>
		public static global::Windows.UI.Xaml.DependencyProperty TodayBackgroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TodayBackground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));

		/// <summary>
		/// Uno specific property
		/// </summary>
		public static global::Windows.UI.Xaml.DependencyProperty TodaySelectedBackgroundProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(TodaySelectedBackground), typeof(global::Windows.UI.Xaml.Media.Brush),
			typeof(global::Windows.UI.Xaml.Controls.CalendarView),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endregion
	}
}
