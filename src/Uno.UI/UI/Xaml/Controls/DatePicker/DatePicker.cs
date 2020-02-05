using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePicker : Control
	{
		public event EventHandler<DatePickerValueChangedEventArgs> DateChanged;

		#region DateProperty

		public DateTimeOffset Date
		{
			get { return (DateTimeOffset)this.GetValue(DateProperty); }
			set { this.SetValue(DateProperty, value); }
		}

		//#18331 If the Date property of DatePickerFlyout is two way binded, the ViewModel receives the control's default value while the ViewModel sends its default value which desynchronizes the values
		//Set initial value of DatePicker to DateTimeOffset.MinValue to avoid 2 way binding issue where the DatePicker reset Date(DateTimeOffset.MinValue) after the initial binding value.
		//We assume that this is the view model who will set the initial value just the time to fix #18331
		public static readonly DependencyProperty DateProperty =
			DependencyProperty.Register("Date", typeof(DateTimeOffset), typeof(DatePicker), new PropertyMetadata(DateTimeOffset.MinValue,
				(s, e) => ((DatePicker)s).OnDatePropertyChanged((DateTimeOffset)e.NewValue, (DateTimeOffset)e.OldValue)));

		private void OnDatePropertyChanged(DateTimeOffset newValue, DateTimeOffset oldValue)
		{
#if XAMARIN
			UpdateDisplayedDate();
#endif

			OnDateChangedPartial();
			DateChanged?.Invoke(this, new DatePickerValueChangedEventArgs(newValue, oldValue));
		}

		partial void OnDateChangedPartial();
		#endregion

		#region DayVisibleProperty
		public bool DayVisible
		{
			get { return (bool)this.GetValue(DayVisibleProperty); }
			set { this.SetValue(DayVisibleProperty, value); }
		}

		public static readonly DependencyProperty DayVisibleProperty =
			DependencyProperty.Register("DayVisible", typeof(bool), typeof(DatePicker), new PropertyMetadata(true,
				(s, e) => ((DatePicker)s).OnDayVisibleChangedPartial()));

		partial void OnDayVisibleChangedPartial();
		#endregion

		#region MonthVisibleProperty
		public bool MonthVisible
		{
			get { return (bool)this.GetValue(MonthVisibleProperty); }
			set { this.SetValue(MonthVisibleProperty, value); }
		}

		public static readonly DependencyProperty MonthVisibleProperty =
			DependencyProperty.Register("MonthVisible", typeof(bool), typeof(DatePicker), new PropertyMetadata(true,
				(s, e) => ((DatePicker)s).OnMonthVisibleChangedPartial()));

		partial void OnMonthVisibleChangedPartial();
		#endregion

		#region YearVisibleProperty
		public bool YearVisible
		{
			get { return (bool)this.GetValue(YearVisibleProperty); }
			set { this.SetValue(YearVisibleProperty, value); }
		}

		public static readonly DependencyProperty YearVisibleProperty =
			DependencyProperty.Register("YearVisible", typeof(bool), typeof(DatePicker), new PropertyMetadata(true,
				(s, e) => ((DatePicker)s).OnYearVisibleChangedPartial()));

		partial void OnYearVisibleChangedPartial();
		#endregion

		#region MaxYearProperty
		public DateTimeOffset MaxYear
		{
			get { return (DateTimeOffset)this.GetValue(MaxYearProperty); }
			set { this.SetValue(MaxYearProperty, value); }
		}

		public static readonly DependencyProperty MaxYearProperty =
			DependencyProperty.Register("MaxYear", typeof(DateTimeOffset), typeof(DatePicker), new PropertyMetadata(DateTimeOffset.MaxValue,
				(s, e) => ((DatePicker)s).OnMaxYearChangedPartial()));

		partial void OnMaxYearChangedPartial();
		#endregion

		#region MinYearProperty
		public DateTimeOffset MinYear
		{
			get { return (DateTimeOffset)this.GetValue(MinYearProperty); }
			set { this.SetValue(MinYearProperty, value); }
		}

		public static readonly DependencyProperty MinYearProperty =
			DependencyProperty.Register("MinYear", typeof(DateTimeOffset), typeof(DatePicker), new PropertyMetadata(DateTimeOffset.MinValue,
				(s, e) => ((DatePicker)s).OnMinYearChangedPartial()));

		partial void OnMinYearChangedPartial();
		#endregion

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(DatePicker),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get { return (Brush)GetValue(LightDismissOverlayBackgroundProperty); }
			set { SetValue(LightDismissOverlayBackgroundProperty, value); }
		}

		internal static readonly DependencyProperty LightDismissOverlayBackgroundProperty =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(DatePicker), new PropertyMetadata(null));

		public DatePicker()
		{
			LightDismissOverlayBackground = Resources["DatePickerLightDismissOverlayBackground"] as Brush ??
				// This is normally a no-op - the above line should retrieve the framework-level resource. This is purely to fail the build when
				// Resources/Styles are overhauled (and the above will no longer be valid)
				Uno.UI.GlobalStaticResources.DatePickerLightDismissOverlayBackground as Brush;

			InitializePartial();
		}

		partial void InitializePartial();

#if XAMARIN

		#region Template parts
		public const string DayTextBlockPartName = "DayTextBlock";
		public const string MonthTextBlockPartName = "MonthTextBlock";
		public const string YearTextBlockPartName = "YearTextBlock";

		public const string FlyoutButtonGridPartName = "FlyoutButtonContentGrid";
		public const string DayColumnPartName = "DayColumn";
		public const string MonthColumnPartName = "MonthColumn";
		public const string YearColumnPartName = "YearColumn";

		public const string FlyoutButtonPartName = "FlyoutButton";
		#endregion

		private Button _flyoutButton;

		private TextBlock _dayTextBlock;
		private TextBlock _monthTextBlock;
		private TextBlock _yearTextBlock;
		private bool _isLoaded;
		private bool _isViewReady;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_flyoutButton = this.GetTemplateChild(FlyoutButtonPartName) as Button;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			if (flyoutContent != null)
			{
				_dayTextBlock = flyoutContent.GetTemplateChild(DayTextBlockPartName) as TextBlock;
				_monthTextBlock = flyoutContent.GetTemplateChild(MonthTextBlockPartName) as TextBlock;
				_yearTextBlock = flyoutContent.GetTemplateChild(YearTextBlockPartName) as TextBlock;
				if (_dayTextBlock != null && _monthTextBlock != null && _yearTextBlock != null)
				{
					InitializeTextBlocks(flyoutContent);
					UpdateDisplayedDate();
				}
			}

			_isViewReady = true;

			SetupFlyoutButton();

			OnApplyTemplatePartial();

			OnDayVisibleChangedPartial();
			OnMonthVisibleChangedPartial();
			OnYearVisibleChangedPartial();
			OnMinYearChangedPartial();
			OnMaxYearChangedPartial();
		}

		partial void OnApplyTemplatePartial();

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_isLoaded = true;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			if (flyoutContent != null)
			{
				SetupFlyoutButton();
			}
		}

		private void SetupFlyoutButton()
		{
			if (!_isViewReady || !_isLoaded)
			{
				return;
			}

			if (_flyoutButton != null)
			{
#if __IOS__ || __ANDROID__
				_flyoutButton.Flyout = new DatePickerFlyout()
				{
#if __IOS__
					Placement = FlyoutPlacement,
#endif
					Date = Date,
					MinYear = MinYear,
					MaxYear = MaxYear
				};

				BindToFlyout(nameof(Date));
				BindToFlyout(nameof(MinYear));
				BindToFlyout(nameof(MaxYear));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));
#endif
			}
		}

		private void BindToFlyout(string propertyName)
		{
			this.Binding(propertyName, propertyName, _flyoutButton.Flyout, BindingMode.TwoWay);
		}

		private void InitializeTextBlocks(IFrameworkElement container)
		{
			var items = GetOrderedTextBlocksForCulture(CultureInfo.CurrentCulture);
			var flyoutButtonGrid = container.GetTemplateChild(FlyoutButtonGridPartName) as Grid;

			Grid.SetColumn(items[0], GetColumnIndex(flyoutButtonGrid, DayColumnPartName));
			Grid.SetColumn(items[1], GetColumnIndex(flyoutButtonGrid, MonthColumnPartName));
			Grid.SetColumn(items[2], GetColumnIndex(flyoutButtonGrid, YearColumnPartName));
		}

		private TextBlock[] GetOrderedTextBlocksForCulture(CultureInfo culture)
		{
			var currentDateFormat = culture.DateTimeFormat.ShortDatePattern;

			var dayIndex = currentDateFormat.IndexOf("d", StringComparison.InvariantCultureIgnoreCase);
			var monthIndex = currentDateFormat.IndexOf("m", StringComparison.InvariantCultureIgnoreCase);
			var yearIndex = currentDateFormat.IndexOf("y", StringComparison.InvariantCultureIgnoreCase);

			return new[]
				{
					new { Index = dayIndex, Item = _dayTextBlock },
					new { Index = monthIndex, Item = _monthTextBlock },
					new { Index = yearIndex, Item = _yearTextBlock }
				}
				.OrderBy(x => x.Index)
				.Select(x => x.Item)
				.ToArray();
		}

		private int GetColumnIndex(Grid grid, string columnName)
		{
			switch (columnName)
			{
				case DayColumnPartName:
					return 0;
				case MonthColumnPartName:
					return 2;
				case YearColumnPartName:
					return 4;
				default:
					throw new ArgumentOutOfRangeException("This name is not supported", "columnName");
			}

			//The name set in the template for the ColumnDefinition is only used in the generated c# code, we have no way of accessing it from here.
			//Right now, we'll consider that the template is always built the same way
			//Ideally, it should be something like this :
			//var column = grid.GetTemplateChild(columnName) as ColumnDefinition;

			//return grid.ColumnDefinitions.IndexOf(column);
		}

		private void UpdateDisplayedDate()
		{
			if (_dayTextBlock != null)
			{
				_dayTextBlock.Text = Date.Day.ToStringInvariant();
			}
			if (_monthTextBlock != null)
			{
				_monthTextBlock.Text = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames[Date.Month - 1];
			}
			if (_yearTextBlock != null)
			{
				_yearTextBlock.Text = Date.Year.ToStringInvariant();
			}
		}
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new DatePickerAutomationPeer(this);
		}
	}
}

