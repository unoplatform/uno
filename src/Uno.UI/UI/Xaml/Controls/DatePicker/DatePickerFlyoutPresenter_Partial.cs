using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerFlyoutPresenter : Control
	{
		const bool PICKER_SHOULD_LOOP = true;

		const int DATEPICKER_RTL_CHARACTER_CODE = 8207;
		//const int DATEPICKER_MIN_MAX_YEAR_DEAFULT_OFFSET = 100;
		const int DATEPICKER_SENTINELTIME_HOUR = 12;
		const int DATEPICKER_SENTINELTIME_MINUTE = 0;
		const int DATEPICKER_SENTINELTIME_SECOND = 0;
		const int DATEPICKER_WRAP_AROUND_MONTHS_FIRST_INDEX = 1;

		//const string _dayLoopingSelectorAutomationId = "DayLoopingSelector";
		//const string _monthLoopingSelectorAutomationId = "MonthLoopingSelector";
		//const string _yearLoopingSelectorAutomationId = "YearLoopingSelector";

		const string _firstPickerHostName = "FirstPickerHost";
		const string _secondPickerHostName = "SecondPickerHost";
		const string _thirdPickerHostName = "ThirdPickerHost";
		const string _backgroundName = "Background";
		const string _contentPanelName = "ContentPanel";
		const string _titlePresenterName = "TitlePresenter";

		public DatePickerFlyoutPresenter()
		{
			//_isInitializing = true;
			_dayVisible = true;
			_monthVisible = true;
			_yearVisible = true;
			_minYear = new DateTimeOffset();
			_maxYear = new DateTimeOffset();
			_acceptDismissButtonsVisible = true;

			DefaultStyleKey = typeof(DatePickerFlyoutPresenter);
		}

		//void InitializeImpl()
		//{
		//	//wrl.ComPtr<xaml_controls.IControlFactory> spInnerFactory;
		//	Control spInnerInstance;
		//	//wrl.ComPtr<DependencyObject> spInnerInspectable;

		//	//DatePickerFlyoutPresenterGenerated.InitializeImpl();
		//	//(wf.GetActivationFactory(
		//	//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Control),
		//	//	&spInnerFactory));

		//	//(spInnerFactory.CreateInstance(
		//	//	(DependencyObject)((IDatePickerFlyoutPresenter)(this)),
		//	//	&spInnerInspectable,
		//	//	&spInnerInstance));
		//	spInnerInstance = new DatePickerFlyoutPresenter();

		//	//(SetComposableBasePointers(
		//	//	spInnerInspectable,
		//	//	spInnerFactory));

		//	//(Private.SetDefaultStyleKey(
		//	//	spInnerInspectable,
		//	//	"Microsoft/* UWP don't rename */.UI.Xaml.Controls.DatePickerFlyoutPresenter"));
		//	spInnerInstance.DefaultStyleKey = typeof(DatePickerFlyoutPresenter);
		//}

		protected override void OnApplyTemplate()
		{
			//Control spControlProtected;
			Border spBackgroundBorder;
			TextBlock spTitlePresenter;
			Border spFirstPickerHost;
			Border spSecondPickerHost;
			Border spThirdPickerHost;
			FrameworkElement spContentPanel;
			ColumnDefinition spDayColumn;
			ColumnDefinition spMonthColumn;
			ColumnDefinition spYearColumn;
			ColumnDefinition spFirstSpacerColumn;
			ColumnDefinition spSecondSpacerColumn;
			Grid spPickerHostGrid;
			UIElement spFirstPickerSpacing;
			UIElement spSecondPickerSpacing;
			//Panel spPickerHostGridAsPanel;
			UIElementCollection spPickerHostGridChildren = default;
			UIElement spAcceptDismissHostGrid;
			UIElement spAcceptButton;
			UIElement spDismissButton;
			//Thickness itemPadding;
			//Thickness monthPadding;

			if (_tpDayPicker != null)
			{
				_tpDayPicker.SelectionChanged -= OnSelectorSelectionChanged;
			}

			if (_tpMonthPicker != null)
			{
				_tpMonthPicker.SelectionChanged -= OnSelectorSelectionChanged;
			}

			if (_tpYearPicker != null)
			{
				_tpYearPicker.SelectionChanged -= OnSelectorSelectionChanged;
			}

			_tpBackgroundBorder = null;
			_tpTitlePresenter = null;
			_tpDayPicker = null;
			_tpMonthPicker = null;
			_tpYearPicker = null;
			_tpFirstPickerHost = null;
			_tpSecondPickerHost = null;
			_tpThirdPickerHost = null;
			_tpContentPanel = null;
			_tpAcceptDismissHostGrid = null;
			_tpAcceptButton = null;
			_tpDismissButton = null;

			//QueryInterface(__uuidof(xaml_controls.IControlProtected), &spControlProtected);
			//DatePickerFlyoutPresenterGenerated.OnApplyTemplateImpl();
			//(Private.AttachTemplatePart<Border>(
			//	spControlProtected,
			//	_backgroundName,
			//	&spBackgroundBorder));
			spBackgroundBorder = GetTemplateChild<Border>(_backgroundName);
			_tpBackgroundBorder = spBackgroundBorder;
			//(Private.AttachTemplatePart<TextBlock>(
			//	spControlProtected,
			//	_titlePresenterName,
			//	&spTitlePresenter));
			spTitlePresenter = GetTemplateChild<TextBlock>(_titlePresenterName);
			_tpTitlePresenter = spTitlePresenter;

			if (spTitlePresenter != null)
			{
				UIElement spPresenterAsUI;
				//_tpTitlePresenter.As(spPresenterAsUI);
				spPresenterAsUI = _tpTitlePresenter;
				spPresenterAsUI.Visibility =
					//WindowsIsStringEmpty(_title ? Visibility.Collapsed : Visibility.Visible);
					string.IsNullOrWhiteSpace(_title) ? Visibility.Collapsed : Visibility.Visible;
				_tpTitlePresenter.Text = _title;
			}

			//(Private.AttachTemplatePart<Border>(
			//	spControlProtected,
			//	_firstPickerHostName,
			//	&spFirstPickerHost));
			spFirstPickerHost = GetTemplateChild<Border>(_firstPickerHostName);
			_tpFirstPickerHost = spFirstPickerHost;
			//(Private.AttachTemplatePart<Border>(
			//	spControlProtected,
			//	_secondPickerHostName,
			//	&spSecondPickerHost));
			spSecondPickerHost = GetTemplateChild<Border>(_secondPickerHostName);
			_tpSecondPickerHost = spSecondPickerHost;
			//(Private.AttachTemplatePart<Border>(
			//	spControlProtected,
			//	_thirdPickerHostName,
			//	&spThirdPickerHost));
			spThirdPickerHost = GetTemplateChild<Border>(_thirdPickerHostName);
			_tpThirdPickerHost = spThirdPickerHost;
			//(Private.AttachTemplatePart<xaml.FrameworkElement>(
			//	spControlProtected,
			//	_contentPanelName,
			//	&spContentPanel));
			spContentPanel = GetTemplateChild<FrameworkElement>(_contentPanelName);
			_tpContentPanel = spContentPanel;
			//(Private.AttachTemplatePart<ColumnDefinition>(
			//	spControlProtected,
			//	"DayColumn",
			//	&spDayColumn));
			spDayColumn = GetTemplateChild<ColumnDefinition>("DayColumn");
			_tpDayColumn = spDayColumn;
			//(Private.AttachTemplatePart<ColumnDefinition>(
			//	spControlProtected,
			//	"MonthColumn",
			//	&spMonthColumn));
			spMonthColumn = GetTemplateChild<ColumnDefinition>("MonthColumn");
			_tpMonthColumn = spMonthColumn;
			//(Private.AttachTemplatePart<ColumnDefinition>(
			//	spControlProtected,
			//	"YearColumn",
			//	&spYearColumn));
			spYearColumn = GetTemplateChild<ColumnDefinition>("YearColumn");
			_tpYearColumn = spYearColumn;
			//(Private.AttachTemplatePart<ColumnDefinition>(
			//	spControlProtected,
			//	"FirstSpacerColumn",
			//	&spFirstSpacerColumn));
			spFirstSpacerColumn = GetTemplateChild<ColumnDefinition>("FirstSpacerColumn");
			_tpFirstSpacerColumn = spFirstSpacerColumn;
			//(Private.AttachTemplatePart<ColumnDefinition>(
			//	spControlProtected,
			//	"SecondSpacerColumn",
			//	&spSecondSpacerColumn));
			spSecondSpacerColumn = GetTemplateChild<ColumnDefinition>("SecondSpacerColumn");
			_tpSecondSpacerColumn = spSecondSpacerColumn;
			//(Private.AttachTemplatePart<UIElement>(
			//	spControlProtected,
			//	"FirstPickerSpacing",
			//	&spFirstPickerSpacing));
			spFirstPickerSpacing = GetTemplateChild<UIElement>("FirstPickerSpacing");
			_tpFirstPickerSpacing = spFirstPickerSpacing;
			//(Private.AttachTemplatePart<UIElement>(
			//	spControlProtected,
			//	"SecondPickerSpacing",
			//	&spSecondPickerSpacing));
			spSecondPickerSpacing = GetTemplateChild<UIElement>("SecondPickerSpacing");
			_tpSecondPickerSpacing = spSecondPickerSpacing;
			//(Private.AttachTemplatePart<Grid>(
			//	spControlProtected,
			//	"PickerHostGrid",
			//	&spPickerHostGrid));
			spPickerHostGrid = GetTemplateChild<Grid>("PickerHostGrid");
			_tpPickerHostGrid = spPickerHostGrid;
			//(Private.AttachTemplatePart<UIElement>(
			//	spControlProtected,
			//	"AcceptDismissHostGrid",
			//	&spAcceptDismissHostGrid));
			spAcceptDismissHostGrid = GetTemplateChild<UIElement>("AcceptDismissHostGrid");
			_tpAcceptDismissHostGrid = spAcceptDismissHostGrid;
			//(Private.AttachTemplatePart<UIElement>(
			//	spControlProtected,
			//	"AcceptButton",
			//	&spAcceptButton));
			spAcceptButton = GetTemplateChild<UIElement>("AcceptButton");
			_tpAcceptButton = spAcceptButton;
			//(Private.AttachTemplatePart<UIElement>(
			//	spControlProtected,
			//	"DismissButton",
			//	&spDismissButton));
			spDismissButton = GetTemplateChild<UIElement>("DismissButton");
			_tpDismissButton = spDismissButton;
			if (_tpPickerHostGrid is Panel spPickerHostGridAsPanel)
			{
				//_tpPickerHostGrid.As(spPickerHostGridAsPanel);
				spPickerHostGridChildren = spPickerHostGridAsPanel.Children;
				global::System.Diagnostics.Debug.Assert(spPickerHostGridChildren != null);
			}

			int itemHeight;
			if (Application.Current.Resources.TryGetValue("DatePickerFlyoutPresenterItemHeight", out var oItemHeightFromMarkup) &&
			   oItemHeightFromMarkup is double itemHeightFromMarkup)
			{
				itemHeight = (int)(itemHeightFromMarkup);
			}
			else
			{
				// Value for RS4. Used if resource values not found
				itemHeight = 44;
			}

			if (!(Application.Current.Resources.TryGetValue("DatePickerFlyoutPresenterItemPadding", out var oItemPadding) &&
				oItemPadding is Thickness itemPadding))
			{
				itemPadding = new Thickness(
					0, 3, 0, 5
				);
			}

			if (!(Application.Current.Resources.TryGetValue("DatePickerFlyoutPresenterMonthPadding", out var oMonthPadding) &&
				  oMonthPadding is Thickness monthPadding))
			{
				monthPadding = new Thickness(
					9, 3, 0, 5
				);
			}

			//The Template uses a single host Grid for the 3 LoopingSelectors.
			if (_tpFirstPickerHost != null || _tpPickerHostGrid != null)
			{
				//wrl.ComPtr<UIElement> spLSAsUI;
				//wrl.ComPtr<xaml.FrameworkElement> spLSAsFE;
				Control spLSAsControl;
				LoopingSelector spMonthPicker;

				//wrl.MakeAndInitialize<xaml_primitives.LoopingSelector>(spMonthPicker);
				spMonthPicker = new LoopingSelector() { ShouldLoop = PICKER_SHOULD_LOOP };
				_tpMonthPicker = spMonthPicker;
				//spMonthPicker.As(spLSAsUI);
				//spMonthPicker.As(spLSAsFE);
				//spMonthPicker.As(spLSAsControl);
				spLSAsControl = spMonthPicker;
				//Don't set ItemWidth. We want the item to size to the width of its parent.
				spMonthPicker.ItemHeight = itemHeight;
				spLSAsControl.HorizontalContentAlignment = HorizontalAlignment.Left;
				spLSAsControl.Padding = monthPadding;
				spMonthPicker.Name = "MonthLoopingSelector";
				if (_tpFirstPickerHost != null)
				{
					//_tpFirstPickerHost.Child = spLSAsUI;
					_tpFirstPickerHost.Child = spMonthPicker;
				}
				else if (spPickerHostGridChildren != null) //_tpPickerHostGrid != null
				{
					//spPickerHostGridChildren.Append(spLSAsUI);
					spPickerHostGridChildren.Add(spMonthPicker);
				}
			}

			if (_tpSecondPickerHost != null || _tpPickerHostGrid != null)
			{
				//wrl.ComPtr<UIElement> spLSAsUI;
				//wrl.ComPtr<xaml.FrameworkElement> spLSAsFE;
				Control spLSAsControl;
				LoopingSelector spDayPicker;

				//wrl.MakeAndInitialize<xaml_primitives.LoopingSelector>(spDayPicker);
				spDayPicker = new LoopingSelector() { ShouldLoop = PICKER_SHOULD_LOOP };
				_tpDayPicker = spDayPicker;
				//spDayPicker.As(spLSAsUI);
				//spDayPicker.As(spLSAsFE);
				//spDayPicker.As(spLSAsControl);
				spLSAsControl = spDayPicker;
				//Don't set ItemWidth. We want the item to size to the width of its parent.
				spDayPicker.ItemHeight = itemHeight;
				spLSAsControl.HorizontalContentAlignment = HorizontalAlignment.Center;
				spLSAsControl.Padding = itemPadding;
				spLSAsControl.Name = "DayLoopingSelector";
				if (_tpSecondPickerHost != null)
				{
					//_tpSecondPickerHost.Child = spLSAsUI;
					_tpSecondPickerHost.Child = spDayPicker;
				}
				else if (spPickerHostGridChildren != null) //_tpPickerHostGrid != null
				{
					//spPickerHostGridChildren.Append(spLSAsUI);
					spPickerHostGridChildren.Add(spDayPicker);
				}
			}

			if (_tpThirdPickerHost != null || _tpPickerHostGrid != null)
			{
				//wrl.ComPtr<UIElement> spLSAsUI;
				//wrl.ComPtr<xaml.FrameworkElement> spLSAsFE;
				Control spLSAsControl;
				LoopingSelector spYearPicker;

				//wrl.MakeAndInitialize<xaml_primitives.LoopingSelector>(spYearPicker);
				spYearPicker = new LoopingSelector() { ShouldLoop = PICKER_SHOULD_LOOP };
				_tpYearPicker = spYearPicker;
				//spYearPicker.As(spLSAsUI);
				//spYearPicker.As(spLSAsFE);
				//spYearPicker.As(spLSAsControl);
				spLSAsControl = spYearPicker;
				/*  spYearPicker.ShouldLoop = false;  NOT SUPPORTED BY LISTVIEW */
				//Don't set ItemWidth. We want the item to size to the width of its parent.
				spYearPicker.ItemHeight = itemHeight;
				spLSAsControl.HorizontalContentAlignment = HorizontalAlignment.Center;
				spLSAsControl.Padding = itemPadding;
				spLSAsControl.Name = "YearLoopingSelector";
				if (_tpSecondPickerHost != null)
				{
					_tpThirdPickerHost.Child = spYearPicker;
				}
				else if (spPickerHostGridChildren != null) //_tpPickerHostGrid != null
				{
					spPickerHostGridChildren.Add(spYearPicker);
				}
			}

			if (_tpDayPicker != null)
			{
				//DependencyObject spDayPickerAsDO;
				//string localizedName;

				//(_tpDayPicker.add_SelectionChanged(
				//	wrl.Callback<xaml_controls.ISelectionChangedEventHandler>
				//		(this, &DatePickerFlyoutPresenter.OnSelectorSelectionChanged),
				//	&_daySelectionChangedToken));
				_tpDayPicker.SelectionChanged += OnSelectorSelectionChanged;

				// TODO - automation peering
				//_tpDayPicker.As(spDayPickerAsDO);
				//(Private.FindStringResource(
				//	UIA_AP_DATEPICKER_DAYNAME,
				//	localizedName));
				//(Private.AutomationHelper.SetElementAutomationName(
				//	spDayPickerAsDO,
				//	localizedName));

				//(Private.AutomationHelper.SetElementAutomationId(
				//	spDayPickerAsDO,
				//	wrl_wrappers.Hstring(_dayLoopingSelectorAutomationId)));
			}

			if (_tpMonthPicker != null)
			{
				//wrl.ComPtr<xaml.IDependencyObject> spMonthPickerAsDO;
				//string localizedName;

				//(_tpMonthPicker.add_SelectionChanged(
				//	wrl.Callback<xaml_controls.ISelectionChangedEventHandler>
				//		(this, &DatePickerFlyoutPresenter.OnSelectorSelectionChanged),
				//	&_monthSelectionChangedToken));
				_tpMonthPicker.SelectionChanged += OnSelectorSelectionChanged;

				// TODO - automation peering
				//_tpMonthPicker.As(spMonthPickerAsDO);
				//(Private.FindStringResource(
				//	UIA_AP_DATEPICKER_MONTHNAME,
				//	localizedName));
				//(Private.AutomationHelper.SetElementAutomationName(
				//	spMonthPickerAsDO,
				//	localizedName));

				//(Private.AutomationHelper.SetElementAutomationId(
				//	spMonthPickerAsDO,
				//	wrl_wrappers.Hstring(_monthLoopingSelectorAutomationId)));
			}

			if (_tpYearPicker != null)
			{
				//wrl.ComPtr<xaml.IDependencyObject> spYearPickerAsDO;
				//string localizedName;

				//(_tpYearPicker.add_SelectionChanged(
				//	wrl.Callback<xaml_controls.ISelectionChangedEventHandler>
				//		(this, &DatePickerFlyoutPresenter.OnSelectorSelectionChanged),
				//	&_yearSelectionChangedToken));
				_tpYearPicker.SelectionChanged += OnSelectorSelectionChanged;

				// TODO - automation peering
				//_tpYearPicker.As(spYearPickerAsDO);
				//(Private.FindStringResource(
				//	UIA_AP_DATEPICKER_YEARNAME,
				//	localizedName));
				//(Private.AutomationHelper.SetElementAutomationName(
				//	spYearPickerAsDO,
				//	localizedName));

				//(Private.AutomationHelper.SetElementAutomationId(
				//	spYearPickerAsDO,
				//	wrl_wrappers.Hstring(_yearLoopingSelectorAutomationId)));
			}

			if (!(_tpYearSource != null && _tpMonthSource != null && _tpDaySource != null))
			{
				IList<object> spCollection;
				//IList<object> spCollectionAsInterface;

				//wfci_.Vector<DependencyObject>.Make(spCollection);
				spCollection = new List<object>();
				//spCollection.As(spCollectionAsInterface);
				//spCollectionAsInterface = spCollection;
				_tpDaySource = spCollection;
				//wfci_.Vector<DependencyObject>.Make(spCollection);
				spCollection = new List<object>();
				//spCollection.As(spCollectionAsInterface);
				_tpMonthSource = spCollection;
				//wfci_.Vector<DependencyObject>.Make(spCollection);
				spCollection = new List<object>();
				//spCollection.As(spCollectionAsInterface);
				_tpYearSource = spCollection;
			}

			if (_calendarIdentifier != null)
			{
				RefreshSetup();
			}

			((IDatePickerFlyoutPresenter)this).SetAcceptDismissButtonsVisibility(_acceptDismissButtonsVisible);
			// Apply a shadow
			bool isDefaultShadowEnabled;
			isDefaultShadowEnabled = IsDefaultShadowEnabled;
			if (isDefaultShadowEnabled)
			{
				// TODO
				//ApplyElevationEffect(_tpBackgroundBorder);
			}

			//_isInitializing = false;
		}

		// TODO - automation peering
		//void OnCreateAutomationPeerImpl(
		//		out xaml.Automation.Peers.IAutomationPeer returnValue)
		//{

		//	wrl.ComPtr<xaml_controls.DatePickerFlyoutPresenter> spThis(this);
		//	wrl.ComPtr<xaml_controls.IDatePickerFlyoutPresenter> spThisAsIDatePickerFlyoutPresenter;
		//	wrl.ComPtr<xaml_automation_peers.DatePickerFlyoutPresenterAutomationPeer>
		//		spDatePickerFlyoutPresenterAutomationPeer;

		//	spThis.As(spThisAsIDatePickerFlyoutPresenter);
		//	(wrl.MakeAndInitialize<xaml_automation_peers.DatePickerFlyoutPresenterAutomationPeer>
		//		(spDatePickerFlyoutPresenterAutomationPeer, spThisAsIDatePickerFlyoutPresenter));

		//	spDatePickerFlyoutPresenterAutomationPeer.CopyTo(returnValue);
		//}

		void IDatePickerFlyoutPresenter.PullPropertiesFromOwner(DatePickerFlyout pOwner)
		{
			//wrl.ComPtr<IDatePickerFlyout>
			//spOwner(pOwner);
			//wrl.ComPtr<IDependencyObject> spOwnerAsDO;
			//wrl.ComPtr<xaml_primitives.IPickerFlyoutBaseStatics> spPickerFlyoutBaseStatics;

			string calendarIdentifier;
			string title = default;
			DateTime date = default;
			bool monthVisible = false;
			bool yearVisible = false;
			bool dayVisible = false;
			DateTime minYear = default;
			DateTime maxYear = default;
			string dayFormat;
			string monthFormat;
			string yearFormat;

			int dayFormatCompareResult = 0;
			int monthFormatCompareResult = 0;
			int yearFormatCompareResult = 0;

			int calendarIDCompareResult = 0;
			string oldCalendarID;
			oldCalendarID = _calendarIdentifier; // copies the string

			//spOwner.As(spOwnerAsDO);
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Primitives_PickerFlyoutBase),
			//	&spPickerFlyoutBaseStatics));

			// Pull properties from owner
			//spOwner.get_CalendarIdentifier(calendarIdentifier);
			calendarIdentifier = pOwner.CalendarIdentifier;
			//spPickerFlyoutBaseStatics.GetTitle(spOwnerAsDO, title);
			//title = spPickerFlyoutBaseStatics
			monthVisible = pOwner.MonthVisible;
			yearVisible = pOwner.YearVisible;
			dayVisible = pOwner.DayVisible;
			minYear = pOwner.MinYear;
			maxYear = pOwner.MaxYear;
			date = pOwner.Date;
			//spOwner.get_DayFormat(dayFormat);
			dayFormat = pOwner.DayFormat;
			//spOwner.get_MonthFormat(monthFormat);
			monthFormat = pOwner.MonthFormat;
			//spOwner.get_YearFormat(yearFormat);
			yearFormat = pOwner.YearFormat;

			// Check which values have changed
			//WindowsCompareStringOrdinal(oldCalendarID, calendarIdentifier, &calendarIDCompareResult);
			//WindowsCompareStringOrdinal(_dayFormat, dayFormat, &dayFormatCompareResult);
			//WindowsCompareStringOrdinal(_monthFormat, monthFormat, &monthFormatCompareResult);
			//WindowsCompareStringOrdinal(_yearFormat, yearFormat, &yearFormatCompareResult);
			calendarIDCompareResult = StringComparer.Ordinal.Compare(oldCalendarID, calendarIdentifier);
			dayFormatCompareResult = StringComparer.Ordinal.Compare(_dayFormat, dayFormat);
			monthFormatCompareResult = StringComparer.Ordinal.Compare(_monthFormat, monthFormat);
			yearFormatCompareResult = StringComparer.Ordinal.Compare(_yearFormat, yearFormat);

			bool dayFormatChanged = dayFormatCompareResult != 0;
			bool monthFormatChanged = monthFormatCompareResult != 0;
			bool yearFormatChanged = yearFormatCompareResult != 0;

			bool haveFieldVisibilitiesChanged = dayVisible != _dayVisible ||
												monthVisible != _monthVisible ||
												yearVisible != _yearVisible;

			bool haveYearLimitsChanged = maxYear != _maxYear ||
										 minYear != _minYear;

			// Store new values

			_calendarIdentifier = calendarIdentifier;
			_title = title;
			if (_tpTitlePresenter != null)
			{
				_tpTitlePresenter.Visibility =
					string.IsNullOrWhiteSpace(_title) ? Visibility.Collapsed : Visibility.Visible;
				_tpTitlePresenter.Text = _title;
			}

			_dayVisible = dayVisible;
			_monthVisible = monthVisible;
			_yearVisible = yearVisible;
			_minYear = minYear;
			_maxYear = maxYear;

			_dayFormat = dayFormat;
			_monthFormat = monthFormat;
			_yearFormat = yearFormat;

			// Perform updates
			if (calendarIDCompareResult != 0)
			{
				OnCalendarIdentifierPropertyChanged(oldCalendarID);
			}

			if (dayFormatChanged)
			{
				// The cached formatters are no longer valid. They will be regenerated via RefreshSetup
				_tpPrimaryDayFormatter = null;
			}

			if (monthFormatChanged)
			{
				// The cached formatters are no longer valid. They will be regenerated via RefreshSetup
				_tpPrimaryMonthFormatter = null;
			}

			if (yearFormatChanged)
			{
				// The cached formatters are no longer valid. They will be regenerated via RefreshSetup
				_tpPrimaryYearFormatter = null;
			}

			if (haveYearLimitsChanged || dayFormatChanged || monthFormatChanged || yearFormatChanged)
			{
				RefreshSetup();
			}

			if (haveFieldVisibilitiesChanged)
			{
				UpdateOrderAndLayout();
			}

			// Date has its own handler since it can be set through multiple codepaths
			SetDate(date);

			return;
		}

		void IDatePickerFlyoutPresenter.SetAcceptDismissButtonsVisibility(bool isVisible)
		{
			// If we have a named host grid for the buttons, we'll hide that.
			// Otherwise, we'll just hide the buttons, since we shouldn't
			// assume anything about the surrounding visual tree.
			if (_tpAcceptDismissHostGrid != null)
			{
				_tpAcceptDismissHostGrid.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			}
			else if (_tpAcceptButton != null && _tpDismissButton != null)
			{
				_tpAcceptButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
				_tpDismissButton.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
			}

			_acceptDismissButtonsVisible = isVisible;

			return;
		}

		DateTime IDatePickerFlyoutPresenter.GetDate()
		{
			return _date;
		}

		void SetDate(DateTime newDate)
		{
			// If we're setting the date to the null sentinel value,
			// we'll instead set it to the current date for the purposes
			// of where to place the user's position in the looping selectors.
			if (newDate.UniversalTime == 0)
			{
				//DateTime dateTime = default;
				Calendar calendar;

				calendar = CreateNewCalendar(_calendarIdentifier);
				calendar.SetToNow();
				newDate = calendar.GetDateTime();
			}

			if (newDate.UniversalTime != _date.UniversalTime)
			{
				DateTime oldDate = _date;
				_date = newDate;
				OnDateChanged(oldDate, _date);
			}

			return;
		}

#if false
		void OnKeyDownImpl(KeyRoutedEventArgs pEventArgs)
		{
			DateTimePickerFlyoutHelper.OnKeyDownImpl(pEventArgs, _tpFirstPickerAsControl,
				_tpSecondPickerAsControl, _tpThirdPickerAsControl, _tpContentPanel);
		}
#endif

		//DependencyObject GetDefaultIsDefaultShadowEnabled()
		//{
		//	Private.ValueBoxer.CreateBoolean(true, ppIsDefaultShadowEnabledValue);
		//}

		#region Logical Functionality

		// Clears the ItemsSource and SelectedItem properties of the selectors.
		void ClearSelectors(
				bool clearDay,
				bool clearMonth,
				bool clearYear)
		{
			if (_tpDayPicker != null && clearDay)
			{
				_tpDayPicker.Items = null;
			}

			if (_tpMonthPicker != null && clearMonth)
			{
				_tpMonthPicker.Items = null;
			}

			if (_tpYearPicker != null && clearYear)
			{
				_tpYearPicker.Items = null;
			}
		}

		// Get indices of related fields of current Date for generated itemsources.
		void GetIndices(
				out int yearIndex,
				out int monthIndex,
				out int dayIndex)
		{
			int currentIndex = 0;
			int firstIndex = 0;
			int monthsInThisYear = 0;

			// We will need the second calendar for calculating the year difference
			_tpBaselineCalendar.SetDateTime(ClampDate(_date, _startDate, _endDate));
			_tpCalendar.SetDateTime(_startDate);
			yearIndex = GetYearDifference(_tpCalendar, _tpBaselineCalendar);
			firstIndex = _tpBaselineCalendar.FirstMonthInThisYear;
			currentIndex = _tpBaselineCalendar.Month;
			monthsInThisYear = _tpBaselineCalendar.NumberOfMonthsInThisYear;
			if (currentIndex - firstIndex >= 0)
			{
				monthIndex = currentIndex - firstIndex;
			}
			else
			{
				// A special case is in some ThaiCalendar years first month
				// of the year is April, last month is March and month flow is wrap-around
				// style; April, March .... November, December, January, February, March. So the first index
				// will be 4 and last index will be 3. We are handling the case to convert this wraparound behavior
				// into selected index.
				monthIndex = currentIndex - firstIndex + monthsInThisYear;
			}

			firstIndex = _tpBaselineCalendar.FirstDayInThisMonth;
			currentIndex = _tpBaselineCalendar.Day;
			dayIndex = currentIndex - firstIndex;
		}

		// Clears everything and refreshes the helper objects. After that, generates and
		// sets the itemssources to selectors.
		void RefreshSetup()
		{


			// Since we will be clearing itemssources / selecteditems and putting new ones, selection changes will be fired from the
			// selectors. However, we do not want to process them as if the end user has purposefully changed the selection on a selector.
			PreventReactionToSelectionChange();

			// This will recalculate the startyear/endyear etc and will tell us if we have a valid range to generate sources.
			UpdateState();
			if (_hasValidYearRange)
			{
				int yearIndex = 0;
				int monthIndex = 0;
				int dayIndex = 0;
				DateTime date = default;

				// When we are refreshing all our setup, year selector should
				// also be refreshed.
				RefreshSourcesAndSetSelectedIndices(true /*Refresh day */, true /* Refresh month*/, true /* Refresh year */ );
				// If we refreshed our set up due to a property change, this may have caused us to coerce and change the current displayed date. For example
				// min/max year changes may have force us to coerce the current datetime to the nearest value inside the valid range.
				// So, we should update our DateTime property. If there is a change, we will end up firing the event as desired, if there isn't a change
				// we will just no_op.
				GetIndices(out yearIndex, out monthIndex, out dayIndex);
				date = GetDateFromIndices(yearIndex, monthIndex, dayIndex);
				SetDate(date);
			}

			AllowReactionToSelectionChange();
		}

		// Regenerate the itemssource for the day/month/yearpickers and select the appropriate indices that represent the current DateTime.
		// Depending on which field changes we might not need to refresh some of the sources.
		void RefreshSourcesAndSetSelectedIndices(
				bool refreshDay,
				bool refreshMonth,
				bool refreshYear)
		{

			int yearIndex = 0;
			int monthIndex = 0;
			int dayIndex = 0;

			PreventReactionToSelectionChange();

			GetIndices(out yearIndex, out monthIndex, out dayIndex);
			//ClearSelectors(refreshDay, refreshMonth, refreshYear);
			if (_tpYearPicker != null)
			{
				if (refreshYear)
				{
					GenerateYears();
					_tpYearPicker.Items = _tpYearSource;
				}

				_tpYearPicker.SelectedIndex = yearIndex;
			}

			if (_tpMonthPicker != null)
			{
				if (refreshMonth)
				{
					GenerateMonths(yearIndex);
					_tpMonthPicker.Items = _tpMonthSource;
				}

				_tpMonthPicker.SelectedIndex = monthIndex;
			}

			if (_tpDayPicker != null)
			{
				if (refreshDay)
				{
					GenerateDays(yearIndex, monthIndex);
					_tpDayPicker.Items = _tpDaySource;
				}

				_tpDayPicker.SelectedIndex = dayIndex;
			}

			AllowReactionToSelectionChange();
		}

		// Generate the collection that we will populate our year picker with.
		void GenerateYears()
		{
			string strYear;
			DateTimeFormatter spPrimaryFormatter;
			DateTimeOffset dateTime;

			spPrimaryFormatter = GetYearFormatter(_calendarIdentifier);

			var oldList = _tpYearSource;
			var newList = new object[_numberOfYears];

			for (int yearOffset = 0; yearOffset < _numberOfYears; yearOffset++)
			{
				DatePickerFlyoutItem spItem;

				_tpCalendar.SetDateTime(_startDate);
				_tpCalendar.AddYears(yearOffset);
				_tpCalendar.Hour = DATEPICKER_SENTINELTIME_HOUR;
				_tpCalendar.Minute = DATEPICKER_SENTINELTIME_MINUTE;
				_tpCalendar.Second = DATEPICKER_SENTINELTIME_SECOND;
				dateTime = _tpCalendar.GetDateTime();
				//wrl.MakeAndInitialize<DatePickerFlyoutItem>(spItem);
				spItem = (oldList.Count > yearOffset ? oldList[yearOffset] as DatePickerFlyoutItem : null)
						 ?? new DatePickerFlyoutItem();
				strYear = spPrimaryFormatter.Format(dateTime);
				spItem.PrimaryText = strYear;
				spItem.SecondaryText = "";
				//spItem.As(spInspectable);

				newList[yearOffset] = spItem;
			}

			_tpYearSource = new List<object>(newList);
		}

		// Generate the collection that we will populate our month picker with.
		void GenerateMonths(int yearOffset)
		{
			string strMonth;
			DateTimeFormatter spPrimaryFormatter;
			DateTimeOffset dateTime;
			int monthOffset = 0;
			int numberOfMonths = 0;
			int firstMonthInThisYear = 0;

			spPrimaryFormatter = GetMonthFormatter(_calendarIdentifier);
			_tpCalendar.SetDateTime(_startDate);
			_tpCalendar.AddYears(yearOffset);
			_tpCalendar.Hour = DATEPICKER_SENTINELTIME_HOUR;
			_tpCalendar.Minute = DATEPICKER_SENTINELTIME_MINUTE;
			_tpCalendar.Second = DATEPICKER_SENTINELTIME_SECOND;
			numberOfMonths = _tpCalendar.NumberOfMonthsInThisYear;
			firstMonthInThisYear = _tpCalendar.FirstMonthInThisYear;
			//_tpMonthSource.Clear();

			var oldList = _tpMonthSource;
			var newList = new object[numberOfMonths];

			for (monthOffset = 0; monthOffset < numberOfMonths; monthOffset++)
			{
				DatePickerFlyoutItem spItem;
				//wrl.ComPtr<DependencyObject> spInspectable;

				_tpCalendar.Month = firstMonthInThisYear;
				_tpCalendar.AddMonths(monthOffset);
				dateTime = _tpCalendar.GetDateTime();
				//wrl.MakeAndInitialize<DatePickerFlyoutItem>(spItem);
				spItem = (oldList.Count > monthOffset ? oldList[monthOffset] as DatePickerFlyoutItem : null)
						 ?? new DatePickerFlyoutItem();
				strMonth = spPrimaryFormatter.Format(dateTime);
				spItem.PrimaryText = strMonth;
				//spItem.As(spInspectable);

				newList[monthOffset] = spItem;
			}

			_tpMonthSource = new List<object>(newList);
		}


		// Generate the collection that we will populate our day picker with.
		void GenerateDays(
				int yearOffset,
				int monthOffset)
		{

			string strDay;
			DateTimeFormatter spPrimaryFormatter;
			DateTimeOffset dateTime;
			int dayOffset = 0;
			int numberOfDays = 0;
			int firstDayInThisMonth = 0;
			int firstMonthInThisYear = 0;

			spPrimaryFormatter = GetDayFormatter(_calendarIdentifier);
			_tpCalendar.SetDateTime(_startDate);
			_tpCalendar.AddYears(yearOffset);
			firstMonthInThisYear = _tpCalendar.FirstMonthInThisYear;
			_tpCalendar.Month = firstMonthInThisYear;
			_tpCalendar.AddMonths(monthOffset);
			numberOfDays = _tpCalendar.NumberOfDaysInThisMonth;
			firstDayInThisMonth = _tpCalendar.FirstDayInThisMonth;
			_tpCalendar.Hour = DATEPICKER_SENTINELTIME_HOUR;
			_tpCalendar.Minute = DATEPICKER_SENTINELTIME_MINUTE;
			_tpCalendar.Second = DATEPICKER_SENTINELTIME_SECOND;
			//_tpDaySource.Clear();

			var oldList = _tpDaySource;
			var newList = new object[numberOfDays];

			for (dayOffset = 0; dayOffset < numberOfDays; dayOffset++)
			{
				DatePickerFlyoutItem spItem;
				//wrl.ComPtr<DependencyObject> spInspectable;

				_tpCalendar.Day = firstDayInThisMonth + dayOffset;
				dateTime = _tpCalendar.GetDateTime();
				//wrl.MakeAndInitialize<DatePickerFlyoutItem>(spItem);
				spItem = (oldList.Count > dayOffset ? oldList[dayOffset] as DatePickerFlyoutItem : null)
						 ?? new DatePickerFlyoutItem();
				strDay = spPrimaryFormatter.Format(dateTime);
				spItem.PrimaryText = strDay;
				//spItem.As(spInspectable);

				newList[dayOffset] = spItem;
			}

			_tpDaySource = new List<object>(newList);
		}

		// Reacts to change in selection of our selectors. Calculates the new date represented by the selected indices and updates the
		// Date property.
		void OnSelectorSelectionChanged(
				object sender,
				SelectionChangedEventArgs pArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pArgs);
			if (IsReactionToSelectionChangeAllowed())
			{
				int yearIndex = 0;
				int monthIndex = 0;
				int dayIndex = 0;
				DateTime date = default;

				if (_tpYearPicker != null)
				{
					yearIndex = _tpYearPicker.SelectedIndex;
				}

				if (_tpMonthPicker != null)
				{
					monthIndex = _tpMonthPicker.SelectedIndex;
				}

				if (_tpDayPicker != null)
				{
					dayIndex = _tpDayPicker.SelectedIndex;
				}

				date = GetDateFromIndices(yearIndex, monthIndex, dayIndex);
				SetDate(date);
			}
		}

		// Interprets the selected indices of the selectors and creates and returns a DateTime corresponding to the date represented by these
		// indices.
		DateTime GetDateFromIndices(
				int yearIndex,
				int monthIndex,
				int dayIndex)
		{

			DateTime current = default;
			int safeIndex = 0;
			int firstIndex = 0;
			int totalNumber = 0;
			int period = 0;
			int hour = 0;
			int minute = 0;
			int second = 0;
			int nanosecond = 0;
			int newYear = 0;
			int newMonth = 0;
			int previousYear = 0;
			int previousMonth = 0;
			int previousDay = 0;
			int lastIndex = 0;

			current = ClampDate(_date, _startDate, _endDate);
			_tpCalendar.SetDateTime(current);
			// We want to preserve the time information. So we keep them around in order to prevent them overwritten by our sentinel time.
			period = _tpCalendar.Period;
			hour = _tpCalendar.Hour;
			minute = _tpCalendar.Minute;
			second = _tpCalendar.Second;
			nanosecond = _tpCalendar.Nanosecond;
			previousYear = _tpCalendar.Year;
			previousMonth = _tpCalendar.Month;
			previousDay = _tpCalendar.Day;
			_tpCalendar.SetDateTime(_startDate);
			_tpCalendar.Period = period;
			_tpCalendar.Hour = hour;
			_tpCalendar.Minute = minute;
			_tpCalendar.Second = second;
			_tpCalendar.Nanosecond = nanosecond;
			_tpCalendar.AddYears(yearIndex);
			newYear = _tpCalendar.Year;
			firstIndex = _tpCalendar.FirstMonthInThisYear;
			totalNumber = _tpCalendar.NumberOfMonthsInThisYear;
			lastIndex = _tpCalendar.LastMonthInThisYear;
			if (firstIndex > lastIndex)
			{
				if (monthIndex + firstIndex > totalNumber)
				{
					safeIndex = monthIndex + firstIndex - totalNumber;
				}
				else
				{
					safeIndex = monthIndex + firstIndex;
				}

				if (previousYear != newYear)
				{
					// Year has changed in some transitions in Thai Calendar, this will change the first month, and last month indices of the year.
					safeIndex = Math.Max(Math.Min(previousMonth, totalNumber), DATEPICKER_WRAP_AROUND_MONTHS_FIRST_INDEX);
				}
			}
			else
			{
				if (previousYear == newYear)
				{
					safeIndex = Math.Max(Math.Min(monthIndex + firstIndex, firstIndex + totalNumber - 1), firstIndex);
				}
				else
				{
					// Year has changed in some transitions in Thai Calendar, this will change the first month, and last month indices of the year.
					safeIndex = Math.Max(Math.Min(previousMonth, firstIndex + totalNumber - 1), firstIndex);
				}
			}

			_tpCalendar.Month = safeIndex;
			newMonth = _tpCalendar.Month;
			firstIndex = _tpCalendar.FirstDayInThisMonth;
			totalNumber = _tpCalendar.NumberOfDaysInThisMonth;
			// We also need to coerce the day index into the safe range because a change in month or year may have changed the number of days
			// rendering our previous index invalid.
			safeIndex = Math.Max(Math.Min(dayIndex + firstIndex, firstIndex + totalNumber - 1), firstIndex);
			if (previousYear != newYear || previousMonth != newMonth)
			{
				safeIndex = Math.Max(Math.Min(previousDay, firstIndex + totalNumber - 1), firstIndex);
			}

			_tpCalendar.Day = safeIndex;
			var date = _tpCalendar.GetDateTime();

			return date;
		}

		// Reacts to the changes in string typed properties. Reverts the property value to the last valid value,
		// if property change causes an exception.

		void OnCalendarIdentifierPropertyChanged(
				string oldValue)
		{
			try
			{
				RefreshSetup();
			}
			catch
			{
				_calendarIdentifier = null;
				RefreshSetup();
			}
			//if (/FAILED / (hr))
			//{
			//	// revert the change
			//	_calendarIdentifier.Release();
			//	_calendarIdentifier.Attach(oldValue);
			//	IGNOREHR(SUCCEEDED(RefreshSetup()));
			//}
		}

		// Reacts to changes in Date property. Day may have changed programmatically or end user may have changed the
		// selection of one of our selectors causing a change in Date.

		void OnDateChanged(
				DateTime oldValue,
				DateTime newValue)
		{

			DateTime clampedNewDate = default;
			DateTime clampedOldDate = default;

			if (_hasValidYearRange)
			{
				int newYear = 0;
				int oldYear = 0;
				int newMonth = 0;
				int oldMonth = 0;
				bool refreshMonth = false;
				bool refreshDay = false;

				// The DateTime value set may be out of our acceptable range.
				clampedNewDate = ClampDate(newValue, _startDate, _endDate);
				clampedOldDate = ClampDate(oldValue, _startDate, _endDate);
				if (clampedNewDate.UniversalTime != newValue.UniversalTime)
				{
					// We need to coerce the date into the acceptable range. This will trigger another OnDateChanged which
					// will take care of executing the logic needed.
					SetDate(clampedNewDate);
					return;
				}

				if (clampedNewDate.UniversalTime == clampedOldDate.UniversalTime)
				{
					// It looks like we clamped an invalid date into an acceptable one, we need to refresh the sources.
					refreshMonth = true;
					refreshDay = true;
				}
				else
				{
					_tpCalendar.SetDateTime(clampedOldDate);
					oldYear = _tpCalendar.Year;
					oldMonth = _tpCalendar.Month;
					_tpCalendar.SetDateTime(clampedNewDate);
					newYear = _tpCalendar.Year;
					newMonth = _tpCalendar.Month;
					// Change in year will invalidate month and days.
					if (oldYear != newYear)
					{
						refreshMonth = true;
						refreshDay = true;
					}
					// Change in month will invalidate days.
					else if (oldMonth != newMonth)
					{
						refreshDay = true;
					}
				}

				RefreshSourcesAndSetSelectedIndices(refreshDay, refreshMonth, false);
			}
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the years in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.
		DateTimeFormatter GetYearFormatter(
				string strCalendarIdentifier)
		{


			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(_tpPrimaryYearFormatter != null &&
				  strCalendarIdentifier == _strYearCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				_tpPrimaryYearFormatter = null;

				spFormatter = CreateNewFormatter(_yearFormat, strCalendarIdentifier);
				_tpPrimaryYearFormatter = spFormatter;
				_strYearCalendarIdentifier = strCalendarIdentifier;
			}

			return _tpPrimaryYearFormatter;
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the months in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.
		DateTimeFormatter GetMonthFormatter(
				string strCalendarIdentifier)
		{


			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(_tpPrimaryMonthFormatter != null && strCalendarIdentifier == _strMonthCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				_tpPrimaryMonthFormatter = null;

				spFormatter = CreateNewFormatter(_monthFormat, strCalendarIdentifier);
				_tpPrimaryMonthFormatter = spFormatter;
				_strMonthCalendarIdentifier = strCalendarIdentifier;
			}

			return _tpPrimaryMonthFormatter;
		}

		// Returns the cached DateTimeFormatter for the given Calendar - Format pair for generating the strings
		// representing the days in our date range. If there isn't a cached DateTimeFormatter instance,
		// creates one and caches it to be returned for the following calls with the same pair.
		DateTimeFormatter GetDayFormatter(
				string strCalendarIdentifier)
		{


			// We can only use the cached formatter if there is a cached formatter, cached formatter's format is the same as the new one's
			// and cached formatter's calendar identifier is the same as the new one's.
			if (!(_tpPrimaryDayFormatter != null && strCalendarIdentifier == _strDayCalendarIdentifier))
			{
				// We either do not have a cached formatter or it is stale. We need a create a new one and cache it along
				// with its identifying info.
				DateTimeFormatter spFormatter;

				_tpPrimaryDayFormatter = null;

				spFormatter = CreateNewFormatter(_dayFormat, strCalendarIdentifier);
				_tpPrimaryDayFormatter = spFormatter;
				_strDayCalendarIdentifier = strCalendarIdentifier;
			}

			return _tpPrimaryDayFormatter;
		}

		// Creates a new DateTimeFormatter with the given parameters.
		DateTimeFormatter CreateNewFormatter(
				string strFormat,
				string strCalendarIdentifier)
		{

			//DateTimeFormatterFactory spFormatterFactory;
			DateTimeFormatter spFormatter;
			IReadOnlyList<string> spLanguages;
			//wrl.ComPtr<wfc.IIterable<string>> spLanguagesAsIterable;
			string strGeographicRegion;
			string strClock;

			//if (ppDateTimeFormatter == null) throw new ArgumentNullException(nameof(ppDateTimeFormatter));

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter),
			//	&spFormatterFactory));
			//if (spFormatterFactory == null) throw new ArgumentNullException();

			//spFormatterFactory.CreateDateTimeFormatter(strFormat, spFormatter);
			spFormatter = new DateTimeFormatter(strFormat);

			//spFormatter.get_GeographicRegion(strGeographicRegion);
			//spFormatter.get_Languages(spLanguages);
			//spFormatter.get_Clock(strClock);
			strGeographicRegion = spFormatter.GeographicRegion;
			spLanguages = spFormatter.Languages;
			strClock = spFormatter.Clock;

			//spLanguages.As(spLanguagesAsIterable);
			//(spFormatterFactory.CreateDateTimeFormatterContext(
			//	strFormat, /* Format string */
			//	spLanguagesAsIterable, /* Languages/
			//strGeographicRegion, /* Geographic region */
			//	strCalendarIdentifier, /* Calendar */
			//	strClock, /* Clock */
			//	spFormatter));

			spFormatter = new DateTimeFormatter(
				strFormat, /* Format string */
				spLanguages, /* Languages */
				strGeographicRegion, /* Geographic region */
				strCalendarIdentifier, /* Calendar */
				strClock); /* Clock */


			//ppDateTimeFormatter = spFormatter.Detach();
			return spFormatter;
		}

		// Creates a new wg.Calendar, taking into account the Calendar Identifier
		// represented by our public "Calendar" property.
		Calendar CreateNewCalendar(
				string strCalendarIdentifier)
		{
			//wrl.ComPtr<wg.ICalendarFactory> spCalendarFactory;
			Calendar spTemporaryCalendar;
			IReadOnlyList<string> spLanguages;
			//wrl.ComPtr<wfc.IIterable<string>> spLanguagesAsIterable;
			string strClock;
			Calendar pspCalendar;

			//(wf.ActivateInstance(
			//	wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_Calendar),
			//	spTemporaryCalendar));
			spTemporaryCalendar = new Calendar();

			spLanguages = spTemporaryCalendar.Languages;
			strClock = spTemporaryCalendar.GetClock();
			//spLanguages.As(spLanguagesAsIterable);
			//Create the calendar
			//	(wf.GetActivationFactory(
			//		wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_Calendar),
			//		&spCalendarFactory));

			//	(spCalendarFactory.CreateCalendar(
			//		spLanguagesAsIterable, /* Languages/
			//strCalendarIdentifier, /* Calendar */
			//		strClock, /* Clock */
			//		pspCalendar.ReleaseAndGetAddressOf()));
			pspCalendar = new Calendar(spLanguages, strCalendarIdentifier, strClock);

			return pspCalendar;
		}

		// Given two calendars, finds the difference of years between them. Note that we are counting on the two
		// calendars will have the same system.
		int GetYearDifference(
				Calendar pStartCalendar,
				Calendar pEndCalendar)
		{

			int startEra = 0;
			int endEra = 0;
			int startYear = 0;
			int endYear = 0;
			string strStartCalendarSystem;
			string strEndCalendarSystem;

			strStartCalendarSystem = pStartCalendar.GetCalendarSystem();
			strEndCalendarSystem = pEndCalendar.GetCalendarSystem();
			if (strStartCalendarSystem != strEndCalendarSystem)
			{
				throw new InvalidOperationException("Different calendar system");
			}

			int difference = 0;

			// Get the eras and years of the calendars.
			startEra = pStartCalendar.Era;
			endEra = pEndCalendar.Era;
			startYear = pStartCalendar.Year;
			endYear = pEndCalendar.Year;
			while (startEra != endEra || startYear != endYear)
			{
				// Add years to start calendar until their eras and years both match.
				pStartCalendar.AddYears(1);
				difference++;
				startEra = pStartCalendar.Era;
				startYear = pStartCalendar.Year;
			}

			return difference;
		}

		// Clamps the given date within the range defined by the min and max dates. Note that it is caller's responsibility
		// to feed appropriate min/max values that defines a valid date range.
		DateTime ClampDate(
				DateTime date,
				DateTime minDate,
				DateTime maxDate)
		{
			if (date.UniversalTime < minDate.UniversalTime)
			{
				return minDate;
			}
			else if (date.UniversalTime > maxDate.UniversalTime)
			{
				return maxDate;
			}

			return date;
		}

		// The order of date fields vary depending on geographic region, calendar type etc. This function determines the M/D/Y order using
		// globalization APIs. It also determines whether the fields should be laid RTL.
		void GetOrder(
				out int yearOrder,
				out int monthOrder,
				out int dayOrder,
				out bool isRTL)
		{

			DateTimeFormatter spFormatter;
			IReadOnlyList<string> spPatterns;
			string strDate;

			// Default orderings.
			yearOrder = 2;
			monthOrder = 0;
			dayOrder = 1;
			isRTL = false;

			spFormatter = CreateNewFormatter(
				"day month.full year",
				_calendarIdentifier);
			spPatterns = spFormatter.Patterns;
			strDate = spPatterns[0];
			if (strDate != null)
			{
				string szDate;
				//uint length = 0;
				uint dayOccurence;
				uint monthOccurence;
				uint yearOccurence;

				szDate = strDate;

				//The calendar is right-to-left if the first character of the pattern string is the rtl character
				isRTL = szDate[0] == DATEPICKER_RTL_CHARACTER_CODE;

				unchecked
				{
					// We do string search to determine the order of the fields.
					dayOccurence = (uint)szDate.IndexOf("{day", StringComparison.Ordinal);
					monthOccurence = (uint)szDate.IndexOf("{month", StringComparison.Ordinal);
					yearOccurence = (uint)szDate.IndexOf("{year", StringComparison.Ordinal);
				}

				if (dayOccurence < monthOccurence)
				{
					if (dayOccurence < yearOccurence)
					{
						dayOrder = 0;
						if (monthOccurence < yearOccurence)
						{
							monthOrder = 1;
							yearOrder = 2;
						}
						else
						{
							monthOrder = 2;
							yearOrder = 1;
						}
					}
					else
					{
						dayOrder = 1;
						monthOrder = 2;
						yearOrder = 0;
					}
				}
				else
				{
					if (dayOccurence < yearOccurence)
					{
						dayOrder = 1;
						monthOrder = 0;
						yearOrder = 2;
					}
					else
					{
						dayOrder = 2;
						if (monthOccurence < yearOccurence)
						{
							monthOrder = 0;
							yearOrder = 1;
						}
						else
						{

							monthOrder = 1;
							yearOrder = 0;
						}
					}
				}

			}
		}

		// Updates the order of selectors in our layout. Also takes care of hiding/showing the comboboxes and related spacing depending our
		// public properties set by the user.
		void UpdateOrderAndLayout()
		{

			int yearOrder = 0;
			int monthOrder = 0;
			int dayOrder = 0;
			bool isRTL = false;
			bool firstHostPopulated = false;
			bool secondHostPopulated = false;
			bool thirdHostPopulated = false;
			bool columnIsFound = false;
			int columnIndex = 0;
			ColumnDefinitionCollection spColumns = null;
			ColumnDefinition firstPickerHostColumn = null;
			ColumnDefinition secondPickerHostColumn = null;
			ColumnDefinition thirdPickerHostColumn = null;
			//wrl.ComPtr<GridStatics> spGridStatics;
			FrameworkElement spFrameworkElement;
			UIElement spUIElement;
			Control firstPickerAsControl = default;
			Control secondPickerAsControl = default;
			Control thirdPickerAsControl = default;

			_tpFirstPickerAsControl = null;
			_tpSecondPickerAsControl = null;
			_tpThirdPickerAsControl = null;

			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Grid),
			//	&spGridStatics));

			GetOrder(out yearOrder, out monthOrder, out dayOrder, out isRTL);
			// Some of the Calendars are RTL (Hebrew, Um Al Qura) we need to change the flow direction of DatePicker to accomodate these
			// calendars.
			if (_tpContentPanel != null)
			{
				_tpContentPanel.FlowDirection = isRTL
					? FlowDirection.RightToLeft
					: FlowDirection.LeftToRight;
			}

			// Clear the children of hosts first, so we never risk putting one picker in two hosts and failing.
			if (_tpFirstPickerHost != null)
			{
				_tpFirstPickerHost.Child = null;
			}

			if (_tpSecondPickerHost != null)
			{
				_tpSecondPickerHost.Child = null;
			}

			if (_tpThirdPickerHost != null)
			{
				_tpThirdPickerHost.Child = null;
			}


			// Clear the columns of the grid first. We will re-add the columns that we need further down.
			if (_tpPickerHostGrid != null)
			{
				spColumns = _tpPickerHostGrid.ColumnDefinitions;
				spColumns.Clear();
			}

			// Assign the selectors to the hosts, if the selector is not shown, we will not put the selector inside the related hosts. Note that we
			// could have just collapsed selector or its host to accomplish hiding, however, we decided not to put the hidden fields to already
			// crowded visual tree.
			switch (yearOrder)
			{
				case 0:
					if (_tpFirstPickerHost != null && _tpYearPicker != null && _yearVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpYearPicker;
						_tpFirstPickerHost.Child = spPickerAsUI;
						firstHostPopulated = true;
					}
					else if (_tpYearColumn != null && _tpYearPicker != null && _yearVisible)
					{
						firstHostPopulated = true;
						firstPickerHostColumn = _tpYearColumn;
					}

					if (firstHostPopulated)
					{
						firstPickerAsControl = _tpYearPicker;
					}

					break;
				case 1:
					if (_tpSecondPickerHost != null && _tpYearPicker != null && _yearVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpYearPicker;
						_tpSecondPickerHost.Child = spPickerAsUI;
						secondHostPopulated = true;
					}
					else if (_tpYearColumn != null && _tpYearPicker != null && _yearVisible)
					{
						secondHostPopulated = true;
						secondPickerHostColumn = _tpYearColumn;
					}

					if (secondHostPopulated)
					{
						secondPickerAsControl = _tpYearPicker;
					}

					break;
				case 2:
					if (_tpThirdPickerHost != null && _tpYearPicker != null && _yearVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpYearPicker;
						_tpThirdPickerHost.Child = spPickerAsUI;
						thirdHostPopulated = true;
					}
					else if (_tpYearColumn != null && _tpYearPicker != null && _yearVisible)
					{
						thirdHostPopulated = true;
						thirdPickerHostColumn = _tpYearColumn;
					}

					if (thirdHostPopulated)
					{
						thirdPickerAsControl = _tpYearPicker;
					}

					break;
			}

			switch (monthOrder)
			{
				case 0:
					if (_tpFirstPickerHost != null && _tpMonthPicker != null && _monthVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpMonthPicker;
						_tpFirstPickerHost.Child = spPickerAsUI;
						firstHostPopulated = true;
					}
					else if (_tpMonthColumn != null && _tpMonthPicker != null && _monthVisible)
					{
						firstHostPopulated = true;
						firstPickerHostColumn = _tpMonthColumn;
					}

					if (firstHostPopulated)
					{
						firstPickerAsControl = _tpMonthPicker;
					}

					break;
				case 1:
					if (_tpSecondPickerHost != null && _tpMonthPicker != null && _monthVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpMonthPicker;
						_tpSecondPickerHost.Child = spPickerAsUI;
						secondHostPopulated = true;
					}
					else if (_tpMonthColumn != null && _tpMonthPicker != null && _monthVisible)
					{
						secondHostPopulated = true;
						secondPickerHostColumn = _tpMonthColumn;
					}

					if (secondHostPopulated)
					{
						secondPickerAsControl = _tpMonthPicker;
					}

					break;
				case 2:
					if (_tpThirdPickerHost != null && _tpMonthPicker != null && _monthVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpMonthPicker;
						_tpThirdPickerHost.Child = spPickerAsUI;
						thirdHostPopulated = true;
					}
					else if (_tpMonthColumn != null && _tpMonthPicker != null && _monthVisible)
					{
						thirdHostPopulated = true;
						thirdPickerHostColumn = _tpMonthColumn;
					}

					if (thirdHostPopulated)
					{
						thirdPickerAsControl = _tpMonthPicker;
					}

					break;
			}

			switch (dayOrder)
			{
				case 0:
					if (_tpFirstPickerHost != null && _tpDayPicker != null && _dayVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpDayPicker;
						_tpFirstPickerHost.Child = spPickerAsUI;
						firstHostPopulated = true;
					}
					else if (_tpDayColumn != null && _tpDayPicker != null && _dayVisible)
					{
						firstHostPopulated = true;
						firstPickerHostColumn = _tpDayColumn;
					}

					if (firstHostPopulated)
					{
						firstPickerAsControl = _tpDayPicker;
					}

					break;
				case 1:
					if (_tpSecondPickerHost != null && _tpDayPicker != null && _dayVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpDayPicker;
						_tpSecondPickerHost.Child = spPickerAsUI;
						secondHostPopulated = true;
					}
					else if (_tpDayColumn != null && _tpDayPicker != null && _dayVisible)
					{
						secondHostPopulated = true;
						secondPickerHostColumn = _tpDayColumn;
					}

					if (secondHostPopulated)
					{
						secondPickerAsControl = _tpDayPicker;
					}

					break;
				case 2:
					if (_tpThirdPickerHost != null && _tpDayPicker != null && _dayVisible)
					{
						UIElement spPickerAsUI;
						spPickerAsUI = _tpDayPicker;
						_tpThirdPickerHost.Child = spPickerAsUI;
						thirdHostPopulated = true;
					}
					else if (_tpDayColumn != null && _tpDayPicker != null && _dayVisible)
					{
						thirdHostPopulated = true;
						thirdPickerHostColumn = _tpDayColumn;
					}

					if (thirdHostPopulated)
					{
						thirdPickerAsControl = _tpDayPicker;
					}

					break;
			}

			_tpFirstPickerAsControl = firstPickerAsControl;
			_tpSecondPickerAsControl = secondPickerAsControl;
			_tpThirdPickerAsControl = thirdPickerAsControl;
			// Add the columns to the grid in the correct order (as computed in the switch statement above).
			if (spColumns != null)
			{
				if (firstPickerHostColumn != null)
				{
					spColumns.Add(firstPickerHostColumn);
				}

				if (_tpFirstSpacerColumn != null)
				{
					spColumns.Add(_tpFirstSpacerColumn);
				}

				if (secondPickerHostColumn != null)
				{
					spColumns.Add(secondPickerHostColumn);
				}

				if (_tpSecondSpacerColumn != null)
				{
					spColumns.Add(_tpSecondSpacerColumn);
				}

				if (thirdPickerHostColumn != null)
				{
					spColumns.Add(thirdPickerHostColumn);
				}
			}

			// Set the Grid.Column property on the Day/Month/Year TextBlocks to the index of the matching ColumnDefinition
			// e.g. YearTextBlock Grid.Column = columns.IndexOf(YearColumn)
			if (_tpYearPicker != null && _tpYearColumn != null && _yearVisible && spColumns != null)
			{
				columnIsFound = (columnIndex = spColumns.IndexOf(_tpYearColumn)) >= 0;
				global::System.Diagnostics.Debug.Assert(columnIsFound);
				spFrameworkElement = _tpYearPicker;
				Grid.SetColumn(spFrameworkElement, columnIndex);
			}

			if (_tpMonthPicker != null && _tpMonthColumn != null && _monthVisible && spColumns != null)
			{
				columnIsFound = (columnIndex = spColumns.IndexOf(_tpMonthColumn)) >= 0;
				global::System.Diagnostics.Debug.Assert(columnIsFound);
				spFrameworkElement = _tpMonthPicker;
				Grid.SetColumn(spFrameworkElement, columnIndex);
			}

			if (_tpDayPicker != null && _tpDayColumn != null && _dayVisible && spColumns != null)
			{
				columnIsFound = (columnIndex = spColumns.IndexOf(_tpDayColumn)) >= 0;
				global::System.Diagnostics.Debug.Assert(columnIsFound);
				spFrameworkElement = _tpDayPicker;
				Grid.SetColumn(spFrameworkElement, columnIndex);
			}

			// Collapse the Day/Month/Year LoopingSelectors if DayVisible/MonthVisible/YearVisible are false.
			// Set the TabIndex property on the LoopingSelectors to match the day/month/year order.
			if (_tpDayPicker != null)
			{
				spUIElement = _tpDayPicker;
				spUIElement.Visibility = _dayVisible ? Visibility.Visible : Visibility.Collapsed;
				(spUIElement as Control).TabIndex = dayOrder;
			}

			if (_tpMonthPicker != null)
			{
				spUIElement = _tpMonthPicker;
				spUIElement.Visibility = _monthVisible ? Visibility.Visible : Visibility.Collapsed;
				(spUIElement as Control).TabIndex = monthOrder;
			}

			if (_tpYearPicker != null)
			{
				spUIElement = _tpYearPicker;
				spUIElement.Visibility = _yearVisible ? Visibility.Visible : Visibility.Collapsed;
				(spUIElement as Control).TabIndex = yearOrder;
			}

			// Determine if we will show the spacings and assign visibilities to spacing holders. We will determine if the spacings
			// are shown by looking at which borders/columns are populated.
			// Also move the spacers to the correct column.
			spFrameworkElement = _tpFirstPickerSpacing as FrameworkElement;
			if (spFrameworkElement != null)
			{
				//spFrameworkElement = _tpFirstPickerSpacing;
				spUIElement = _tpFirstPickerSpacing;
				spUIElement.Visibility =
					firstHostPopulated && (secondHostPopulated || thirdHostPopulated)
						? Visibility.Visible
						: Visibility.Collapsed;
				if (_tpFirstSpacerColumn != null && spColumns != null)
				{
					columnIsFound = (columnIndex = spColumns.IndexOf(_tpFirstSpacerColumn)) >= 0;
					global::System.Diagnostics.Debug.Assert(columnIsFound);
					Grid.SetColumn(spFrameworkElement, columnIndex);
				}
			}

			spFrameworkElement = _tpSecondPickerSpacing as FrameworkElement;
			if (spFrameworkElement != null)
			{
				//spFrameworkElement = _tpSecondPickerSpacing;
				spUIElement = _tpSecondPickerSpacing;
				spUIElement.Visibility =
					secondHostPopulated && thirdHostPopulated ? Visibility.Visible : Visibility.Collapsed;
				if (_tpSecondSpacerColumn != null && spColumns != null)
				{
					columnIsFound = (columnIndex = spColumns.IndexOf(_tpSecondSpacerColumn)) >= 0;
					global::System.Diagnostics.Debug.Assert(columnIsFound);
					Grid.SetColumn(spFrameworkElement, columnIndex);
				}
			}
		}

		// We execute our logic depending on some state information such as start date, end date, number of years etc. These state
		// variables need to be updated whenever a public property change occurs which affects them.
		void UpdateState()
		{

			int month = 0;
			int day = 0;
			DateTime minYearDate = default;
			DateTime maxYearDate = default;
			DateTime maxCalendarDate = default;
			DateTime minCalendarDate = default;

			Calendar spCalendar;
			Calendar spBaselineCalendar;

			// Create a calendar with the the current CalendarIdentifier
			spCalendar = CreateNewCalendar(_calendarIdentifier);
			spBaselineCalendar = CreateNewCalendar(_calendarIdentifier);
			_tpCalendar = spCalendar;
			_tpBaselineCalendar = spBaselineCalendar;
			// We do not have a valid range if our MinYear is later than our MaxYear
			_hasValidYearRange = _minYear.UniversalTime <= _maxYear.UniversalTime;

			if (_hasValidYearRange)
			{
				// Find the earliest and latest dates available for this calendar.
				_tpCalendar.SetToMin();
				minCalendarDate = _tpCalendar.GetDateTime();
				//Find the latest date available for this calendar.
				_tpCalendar.SetToMax();
				maxCalendarDate = _tpCalendar.GetDateTime();
				minYearDate = ClampDate(_minYear, minCalendarDate, maxCalendarDate);
				maxYearDate = ClampDate(_maxYear, minCalendarDate, maxCalendarDate);

				// Since we only care about the year field of minYearDate and maxYearDate we will change other fields into first day and last day
				// of the year respectively.
				_tpCalendar.SetDateTime(minYearDate);
				month = _tpCalendar.FirstMonthInThisYear;
				_tpCalendar.Month = month;
				day = _tpCalendar.FirstDayInThisMonth;
				_tpCalendar.Day = day;
				minYearDate = _tpCalendar.GetDateTime();
				_tpCalendar.SetDateTime(maxYearDate);
				month = _tpCalendar.LastMonthInThisYear;
				_tpCalendar.Month = month;
				day = _tpCalendar.LastDayInThisMonth;
				_tpCalendar.Day = day;
				maxYearDate = _tpCalendar.GetDateTime();
				_tpCalendar.SetDateTime(minYearDate);
				//Set our sentinel time to the start date as we will be using it while generating item sources, we do not need to do this for end date
				_tpCalendar.Hour = DATEPICKER_SENTINELTIME_HOUR;
				_tpCalendar.Minute = DATEPICKER_SENTINELTIME_MINUTE;
				_tpCalendar.Second = DATEPICKER_SENTINELTIME_SECOND;
				_startDate = _tpCalendar.GetDateTime();
				_endDate = maxYearDate;

				// Find the number of years in our range
				_tpCalendar.SetDateTime(_startDate);
				_tpBaselineCalendar.SetDateTime(_endDate);
				_numberOfYears = GetYearDifference(_tpCalendar, _tpBaselineCalendar);
				_numberOfYears++; //since we should include both start and end years
			}
			else
			{
				// We do not want to display anything if we do not have a valid year range
				ClearSelectors(true /*Clear day*/, true /*Clear month*/, true /*Clear year*/);
			}

			UpdateOrderAndLayout();
		}

		#endregion
	}
}
