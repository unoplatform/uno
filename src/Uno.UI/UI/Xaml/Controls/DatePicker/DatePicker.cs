using System;
using System.Globalization;
using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePicker : Control
	{
		public event EventHandler<DatePickerValueChangedEventArgs> DateChanged;
		public event TypedEventHandler<DatePicker, DatePickerSelectedValueChangedEventArgs> SelectedDateChanged;

		private static readonly DateTimeOffset UnsetDateValue = DateTimeOffset.MinValue;

		public DatePicker()
		{
			DefaultStyleKey = typeof(DatePicker);
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "DatePickerLightDismissOverlayBackground", isThemeResourceExtension: true);

			InitializePartial();
		}

		partial void InitializePartial();

		#region DateProperty

		public DateTimeOffset Date
		{
			get => (DateTimeOffset)this.GetValue(DateProperty);
			set => this.SetValue(DateProperty, value);
		}

		//#18331 If the Date property of DatePickerFlyout is two way binded, the ViewModel receives the control's default value while the ViewModel sends its default value which desynchronizes the values
		//Set initial value of DatePicker to DateTimeOffset.MinValue to avoid 2 way binding issue where the DatePicker reset Date(DateTimeOffset.MinValue) after the initial binding value.
		//We assume that this is the view model who will set the initial value just the time to fix #18331
		public static DependencyProperty DateProperty { get; } =
			DependencyProperty.Register(nameof(Date), typeof(DateTimeOffset), typeof(DatePicker), new FrameworkPropertyMetadata(UnsetDateValue,
				(s, e) => ((DatePicker)s).OnDatePropertyChanged((DateTimeOffset)e.NewValue, (DateTimeOffset)e.OldValue)));

		private void OnDatePropertyChanged(DateTimeOffset newValue, DateTimeOffset oldValue)
		{
			// pass newValue to SelectedDate, except when originated from SelectedDate to avoid ping pong
			if ((SelectedDate != newValue) &&
				!(newValue == UnsetDateValue && !SelectedDate.HasValue))
			{
				SelectedDate = newValue;
			}

			UpdateDisplayedDate();

			OnDateChangedPartial();
			DateChanged?.Invoke(this, new DatePickerValueChangedEventArgs(newValue, oldValue));
		}

		partial void OnDateChangedPartial();
		#endregion

		#region SelectedDate
		public static DependencyProperty SelectedDateProperty { get; } = DependencyProperty.Register(
			nameof(SelectedDate),
			typeof(DateTimeOffset?),
			typeof(DatePicker),
			new FrameworkPropertyMetadata(default(DateTimeOffset?), (s, e) => (s as DatePicker).OnSelectedDateChanged((DateTimeOffset?)e.NewValue, (DateTimeOffset?)e.OldValue)));

		public DateTimeOffset? SelectedDate
		{
			get => (DateTimeOffset?)GetValue(SelectedDateProperty);
			set => SetValue(SelectedDateProperty, value);
		}

		private void OnSelectedDateChanged(DateTimeOffset? newValue, DateTimeOffset? oldValue)
		{
			if (Date != (newValue ?? UnsetDateValue))
			{
				Date = newValue ?? UnsetDateValue;
			}

			OnSelectedDatePartial();
			SelectedDateChanged?.Invoke(this, new DatePickerSelectedValueChangedEventArgs(newValue, oldValue));
		}

		partial void OnSelectedDatePartial();
		#endregion

		#region DayVisibleProperty
		public bool DayVisible
		{
			get => (bool)this.GetValue(DayVisibleProperty);
			set => this.SetValue(DayVisibleProperty, value);
		}

		public static DependencyProperty DayVisibleProperty { get; } =
			DependencyProperty.Register(nameof(DayVisible), typeof(bool), typeof(DatePicker), new FrameworkPropertyMetadata(defaultValue: true,
				propertyChangedCallback: (s, e) => ((DatePicker)s).OnDayVisibleChanged()));

		partial void OnDayVisibleChangedPartial();
		#endregion

		#region MonthVisibleProperty
		public bool MonthVisible
		{
			get => (bool)this.GetValue(MonthVisibleProperty);
			set => this.SetValue(MonthVisibleProperty, value);
		}

		public static DependencyProperty MonthVisibleProperty { get; } =
			DependencyProperty.Register(nameof(MonthVisible), typeof(bool), typeof(DatePicker), new FrameworkPropertyMetadata(defaultValue: true,
				propertyChangedCallback: (s, e) => ((DatePicker)s).OnMonthVisibleChanged()));

		partial void OnMonthVisibleChangedPartial();
		#endregion

		#region YearVisibleProperty
		public bool YearVisible
		{
			get => (bool)this.GetValue(YearVisibleProperty);
			set => this.SetValue(YearVisibleProperty, value);
		}

		public static DependencyProperty YearVisibleProperty { get; } =
			DependencyProperty.Register(nameof(YearVisible), typeof(bool), typeof(DatePicker), new FrameworkPropertyMetadata(defaultValue: true,
				propertyChangedCallback: (s, e) => ((DatePicker)s).OnYearVisibleChanged()));

		partial void OnYearVisibleChangedPartial();
		#endregion

		#region MaxYearProperty
		public DateTimeOffset MaxYear
		{
			get => (DateTimeOffset)this.GetValue(MaxYearProperty);
			set => this.SetValue(MaxYearProperty, value);
		}

		public static DependencyProperty MaxYearProperty { get; } =
			DependencyProperty.Register(nameof(MaxYear), typeof(DateTimeOffset), typeof(DatePicker), new FrameworkPropertyMetadata(defaultValue: DateTimeOffset.MaxValue,
				propertyChangedCallback: (s, e) => ((DatePicker)s).OnMaxYearChangedPartial()));

		partial void OnMaxYearChangedPartial();
		#endregion

		#region MinYearProperty
		public DateTimeOffset MinYear
		{
			get => (DateTimeOffset)this.GetValue(MinYearProperty);
			set => this.SetValue(MinYearProperty, value);
		}

		public static DependencyProperty MinYearProperty { get; } =
			DependencyProperty.Register(nameof(MinYear), typeof(DateTimeOffset), typeof(DatePicker), new FrameworkPropertyMetadata(defaultValue: DateTimeOffset.MinValue,
				propertyChangedCallback: (s, e) => ((DatePicker)s).OnMinYearChangedPartial()));

		partial void OnMinYearChangedPartial();
		#endregion

		#region HeaderProperty

		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(DatePicker),
				new FrameworkPropertyMetadata(null, (s, e) => ((DatePicker)s)?.OnHeaderChanged(e)));

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs e) =>
			UpdateHeaderVisibility();

		#endregion

		#region HeaderTemplateProperty

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(DatePicker),
				new FrameworkPropertyMetadata(null));

		#endregion

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			set => this.SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode), typeof(LightDismissOverlayMode),
			typeof(DatePicker),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get => (Brush)GetValue(LightDismissOverlayBackgroundProperty);
			set => SetValue(LightDismissOverlayBackgroundProperty, value);
		}

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
			DependencyProperty.Register(nameof(LightDismissOverlayBackground), typeof(Brush), typeof(DatePicker), new FrameworkPropertyMetadata(null));

		#region Template parts
		public const string DayTextBlockPartName = "DayTextBlock";
		public const string MonthTextBlockPartName = "MonthTextBlock";
		public const string YearTextBlockPartName = "YearTextBlock";

		public const string FlyoutButtonGridPartName = "FlyoutButtonContentGrid";
		public const string DayColumnPartName = "DayColumn";
		public const string MonthColumnPartName = "MonthColumn";
		public const string YearColumnPartName = "YearColumn";

		public const string FlyoutButtonPartName = "FlyoutButton";

		private const string FirstPickerSpacingPartName = "FirstPickerSpacing";
		private const string SecondPickerSpacingPartName = "SecondPickerSpacing";
		#endregion

		private Button _flyoutButton;
		private ContentPresenter _headerContentPresenter;
		private Grid _flyoutGrid;
		private ColumnDefinition _dayColumn;
		private ColumnDefinition _firstSeparatorColumn;
		private ColumnDefinition _monthColumn;
		private ColumnDefinition _secondSeparatorColumn;
		private ColumnDefinition _yearColumn;
		private TextBlock _dayTextBlock;
		private TextBlock _monthTextBlock;
		private TextBlock _yearTextBlock;
		private Rectangle _firstPickerSpacing;
		private Rectangle _secondPickerSpacing;

		private bool _isLoaded;
		private bool _isViewReady;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_flyoutButton = this.GetTemplateChild(FlyoutButtonPartName) as Button;

			_flyoutGrid = _flyoutButton?.Content as Grid;

			if (_flyoutGrid != null)
			{				
				_dayColumn = _flyoutGrid.ColumnDefinitions[0];
				_firstSeparatorColumn = _flyoutGrid.ColumnDefinitions[1];
				_monthColumn = _flyoutGrid.ColumnDefinitions[2];
				_secondSeparatorColumn = _flyoutGrid.ColumnDefinitions[3];
				_yearColumn = _flyoutGrid.ColumnDefinitions[4];

				_dayTextBlock = _flyoutGrid.GetTemplateChild(DayTextBlockPartName) as TextBlock;
				_monthTextBlock = _flyoutGrid.GetTemplateChild(MonthTextBlockPartName) as TextBlock;
				_yearTextBlock = _flyoutGrid.GetTemplateChild(YearTextBlockPartName) as TextBlock;

				_firstPickerSpacing = _flyoutGrid.GetTemplateChild(FirstPickerSpacingPartName) as Rectangle;
				_secondPickerSpacing = _flyoutGrid.GetTemplateChild(SecondPickerSpacingPartName) as Rectangle;

				if (_dayTextBlock != null && _monthTextBlock != null && _yearTextBlock != null)
				{
					ReassignColumns();
					UpdateDisplayedDate();
				}
			}

			_headerContentPresenter = GetTemplateChild("HeaderContentPresenter") as ContentPresenter;
			if (_headerContentPresenter != null)
			{
				UpdateHeaderVisibility();
			}

			_isViewReady = true;

			SetupFlyoutButton();

			OnApplyTemplatePartial();

			OnDatePartVisibleChanged();
			OnDayVisibleChangedPartial();
			OnMonthVisibleChangedPartial();
			OnYearVisibleChangedPartial();
			OnMinYearChangedPartial();
			OnMaxYearChangedPartial();
		}

		partial void OnApplyTemplatePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_isLoaded = true;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			if (flyoutContent != null)
			{
				SetupFlyoutButton();
			}
		}

		private void UpdateHeaderVisibility()
		{
			if (_headerContentPresenter != null)
			{
				_headerContentPresenter.Visibility =
					Header != null ? Visibility.Visible : Visibility.Collapsed;
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
				var flyout = new DatePickerFlyout()
				{
					Date = Date,
					MinYear = MinYear,
					MaxYear = MaxYear,
				};
				_flyoutButton.Flyout = flyout;

				flyout.Opened += (s, e) => flyout.Date = SelectedDate ?? DateTimeOffset.Now;
				flyout.DatePicked += (s, e) => Date = e.NewDate;

				BindToFlyout(nameof(MinYear));
				BindToFlyout(nameof(MaxYear));
				flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));
#endif
			}
		}

		private void BindToFlyout(string propertyName)
		{
			this.Binding(propertyName, propertyName, _flyoutButton.Flyout, BindingMode.TwoWay);
		}

		private void ReassignColumns()
		{
			if (_flyoutGrid == null)
			{
				return;
			}
			/* DatePicker normally contains 3 textblocks meant for day/month/year with a different Grid.Column each.
			 * We need to shuffle the columns with the order dictated by current culture, eg: yyyy/mm/dd vs mm/dd/yyyy...
			 * Since we can't just "move" the column, we have to move the textblocks' column, and
			 * swap the relevant properties (eg: ColumnDefinition.Width) from the old column to the new one. */
			var orderedColumns = GetOrderedTextBlocksForCulture(CultureInfo.CurrentCulture);

			_flyoutGrid.ColumnDefinitions.Clear();

			_flyoutGrid.ColumnDefinitions.Add(_firstSeparatorColumn);
			_flyoutGrid.ColumnDefinitions.Add(_secondSeparatorColumn);

			_firstPickerSpacing.Visibility = Visibility.Visible;
			_secondPickerSpacing.Visibility = Visibility.Visible;

			// Insert columns into the appropriate spaces in between separators
			int missingColumns = 0;
			for (var columnIndex = 0; columnIndex < orderedColumns.Length; columnIndex++)
			{
				var column = orderedColumns[columnIndex];

				// Check if this column should be displayed based on the TextBlock visibility
				if (column.Item.Visibility == Visibility.Visible)
				{
					var targetPosition = columnIndex * 2 - missingColumns; // Multiply by 2 to adjust for separators
					_flyoutGrid.ColumnDefinitions.Insert(targetPosition, column.ColumnDefinition);
					Grid.SetColumn(column.Item, targetPosition);
				}
				else
				{
					missingColumns++;
				}
			}

			var firstSeparatorColumnIndex = _flyoutGrid.ColumnDefinitions.IndexOf(_firstSeparatorColumn);
			Grid.SetColumn(_firstPickerSpacing, firstSeparatorColumnIndex);
			var secondSeparatorColumnIndex = _flyoutGrid.ColumnDefinitions.IndexOf(_secondSeparatorColumn);
			Grid.SetColumn(_secondPickerSpacing, secondSeparatorColumnIndex);

			// Hide first separator if it is the first column of the grid
			if (firstSeparatorColumnIndex == 0)
			{
				_firstPickerSpacing.Visibility = Visibility.Collapsed;
			}

			// Hide second separator if it is right next to first
			// or if it is the last column of the grid
			if ((firstSeparatorColumnIndex + 1 == secondSeparatorColumnIndex) ||
				(secondSeparatorColumnIndex == _flyoutGrid.ColumnDefinitions.Count - 1))
			{
				_secondPickerSpacing.Visibility = Visibility.Collapsed;
			}
		}

		private void OnDayVisibleChanged()
		{
			OnDatePartVisibleChanged();
			OnDayVisibleChangedPartial();
		}

		private void OnMonthVisibleChanged()
		{
			OnDatePartVisibleChanged();
			OnMonthVisibleChangedPartial();
		}

		private void OnYearVisibleChanged()
		{
			OnDatePartVisibleChanged();
			OnYearVisibleChangedPartial();
		}

		private void OnDatePartVisibleChanged()
		{
			if (_flyoutGrid == null)
			{
				// Template not applied yet
				return;
			}

			_dayTextBlock.Visibility = DayVisible ? Visibility.Visible : Visibility.Collapsed;
			_monthTextBlock.Visibility = MonthVisible ? Visibility.Visible : Visibility.Collapsed;
			_yearTextBlock.Visibility = YearVisible ? Visibility.Visible : Visibility.Collapsed;

			ReassignColumns();
		}

		private (TextBlock Item, int NewIndex, ColumnDefinition ColumnDefinition)[] GetOrderedTextBlocksForCulture(CultureInfo culture)
		{
			var currentDateFormat = culture.DateTimeFormat.ShortDatePattern;

			return new (int Index, string SortKey, TextBlock Item, ColumnDefinition ColumnDefinition)[]
				{
					(0, "d", _dayTextBlock, _dayColumn),
					(1, "m", _monthTextBlock, _monthColumn),
					(2, "y", _yearTextBlock, _yearColumn),
				}
				.OrderBy(x => currentDateFormat.IndexOf(x.SortKey, StringComparison.InvariantCultureIgnoreCase))
				.Select((x, i) => (x.Item, i, x.ColumnDefinition))
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

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new DatePickerAutomationPeer(this);
		}
	}
}

