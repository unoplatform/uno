// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Globalization.DateTimeFormatting;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using Microsoft.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class CalendarDatePicker : Control
	{
		private const string TEXT_CALENDARDATEPICKER_DEFAULT_PLACEHOLDER_TEXT = nameof(TEXT_CALENDARDATEPICKER_DEFAULT_PLACEHOLDER_TEXT);

		private CalendarView m_tpCalendarView;
		private ContentPresenter m_tpHeaderContentPresenter;
		private TextBlock m_tpDateText;
		private Grid m_tpRoot;
		private FlyoutBase m_tpFlyout;
		//private event FlyoutBaseOpenedEventCallback m_epFlyoutOpenedHandler;
		//private event FlyoutBaseClosedEventCallback m_epFlyoutClosedHandler;
		//private event CalendarViewCalendarViewDayItemChangingCallback m_epCalendarViewCalendarViewDayItemChangingHandler;
		//private event CalendarViewSelectedDatesChangedCallback m_epCalendarViewSelectedDatesChangedHandler;

		private int m_colsInYearDecadeView;
		private int m_rowsInYearDecadeView;

		private DateTime m_displayDate;

		private DateTimeFormatter m_tpDateFormatter;


		// if CalendView template part is not ready, we delay this request.
		private bool m_isYearDecadeViewDimensionRequested;

		// if CalendView template part is not ready, we delay this request.
		private bool m_isSetDisplayDateRequested;

		// true when pointer is over the control but not on the header.
		private bool m_isPointerOverMain;

		// true when pointer is pressed on the control but not on the header.
		private bool m_isPressedOnMain;

		// On pointer released we perform some actions depending on control. We decide to whether to perform them
		// depending on some parameters including but not limited to whether released is followed by a pressed, which
		// mouse button is pressed, what type of pointer is it etc. This bool keeps our decision.
		private bool m_shouldPerformActions;

		private bool m_isSelectedDatesChangingInternally;

		public CalendarDatePicker()
		{
			DefaultStyleKey = typeof(CalendarDatePicker);

			m_isYearDecadeViewDimensionRequested = false;
			m_colsInYearDecadeView = 0;
			m_rowsInYearDecadeView = 0;
			m_isSetDisplayDateRequested = false;
			m_displayDate = default;
			m_isPointerOverMain = false;
			m_isPressedOnMain = false;
			m_shouldPerformActions = false;
			m_isSelectedDatesChangingInternally = false;

			PrepareState();
		}

		private void PrepareState()
		{
			//CalendarDatePickerGenerated.PrepareState();

			// Set a default string as the PlaceholderText property value.
			string strDefaultPlaceholderText;

			strDefaultPlaceholderText = DXamlCore.Current.GetLocalizedResourceString(TEXT_CALENDARDATEPICKER_DEFAULT_PLACEHOLDER_TEXT);

			PlaceholderText = strDefaultPlaceholderText;

			return;
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			//CalendarDatePickerGenerated.OnPropertyChanged2(args);

			if (args.Property == CalendarDatePicker.IsCalendarOpenProperty)
			{
				UpdateCalendarVisibility();
			}
			else if (args.Property == CalendarDatePicker.DateProperty)
			{
				DateTime? spNewDateReference;
				DateTime? spOldDateReference;
				//CValueBoxer.UnboxValue<DateTime>(args.OldValue, spOldDateReference);
				//CValueBoxer.UnboxValue<DateTime>(args.NewValue, spNewDateReference);
				spOldDateReference = (DateTimeOffset?)args.OldValue;
				spNewDateReference = (DateTimeOffset?)args.NewValue;
				OnDateChanged(spOldDateReference, spNewDateReference);
			}
			else if (args.Property == FrameworkElement.LanguageProperty ||
					 args.Property == CalendarDatePicker.CalendarIdentifierProperty ||
					 args.Property == CalendarDatePicker.DateFormatProperty)
			{
				OnDateFormatChanged();
			}
			else if (args.Property == CalendarDatePicker.HeaderProperty || args.Property == CalendarDatePicker.HeaderTemplateProperty)
			{
				UpdateHeaderVisibility();
			}
			else if (args.Property == CalendarDatePicker.LightDismissOverlayModeProperty)
			{
				if (m_tpFlyout is { })
				{
					// Forward the new property onto our flyout.
					var overlayMode = LightDismissOverlayMode.Off;
					overlayMode = LightDismissOverlayMode;
					((FlyoutBase)m_tpFlyout).LightDismissOverlayMode = overlayMode;
				}
			}
		}

		protected override void OnApplyTemplate()
		{
			CalendarView spCalendarView;
			TextBlock spDateText;
			Grid spRoot;

			//DetachHandler(m_epFlyoutOpenedHandler, m_tpFlyout);
			//DetachHandler(m_epFlyoutClosedHandler, m_tpFlyout);
			//DetachHandler(m_epCalendarViewCalendarViewDayItemChangingHandler, m_tpCalendarView);
			//DetachHandler(m_epCalendarViewSelectedDatesChangedHandler, m_tpCalendarView);
			if (m_tpFlyout is { })
			{
				m_tpFlyout.Opened -= OnFlyoutOpened;
				m_tpFlyout.Closed -= OnFlyoutClosed;
			}

			if (m_tpCalendarView is { })
			{
				m_tpCalendarView.CalendarViewDayItemChanging -= OnCalendarViewDayChanging;
				m_tpCalendarView.SelectedDatesChanged -= OnCalendarViewDatesChanged;
			}

			m_tpCalendarView = null;

			m_tpHeaderContentPresenter = default;

			m_tpDateText = default;

			m_tpFlyout = default;

			m_tpRoot = default;

			//CalendarDatePickerGenerated.OnApplyTemplate();

			object GetTemplatePart(string name) => GetTemplateChild(name);

			spCalendarView = GetTemplatePart("CalendarView") as CalendarView;

			spDateText = GetTemplatePart("DateText") as TextBlock;

			spRoot = GetTemplatePart("Root") as Grid;

			m_tpCalendarView = spCalendarView;
			m_tpDateText = spDateText;
			m_tpRoot = spRoot;

			if (m_tpRoot is { })
			{
				FlyoutBase spFlyout;

				//FlyoutBaseFactory.GetAttachedFlyoutStatic(m_tpRoot.Cast<Grid>(), spFlyout.ReleaseAndGetAddressOf()));
				spFlyout = FlyoutBase.GetAttachedFlyout(m_tpRoot);
				//SetPtrValue(m_tpFlyout, spFlyout);
				m_tpFlyout = spFlyout;

				if (m_tpFlyout is { })
				{
					// by default flyoutbase will resize the presenter's content (here is the CalendarView) if there is
					// not enough space, because the content is inside a ScrollViewer.
					// however CalendarView has 3 huge scrollviewers, put CalendarView in a scrollviewer will have a very
					// bad user experience because user can't scroll the outer scrollviewer until the inner scrollviewer
					// hits the end of content.
					// we decide to remove the scrollviewer from presenter's template and let flyoutbase not resize us.
					m_tpFlyout.DisablePresenterResizing();

					// Forward the value of LightDismissOverlayMode to our flyout.
					var overlayMode = LightDismissOverlayMode.Off;
					m_tpFlyout.LightDismissOverlayMode = overlayMode;

					// TODO UNO
					//m_epFlyoutOpenedHandler.AttachEventHandler(m_tpFlyout.Cast<FlyoutBase>(), 
					//	[this](IInspectable * pSender, IInspectable * pArgs)
					//{
					//	OpenedEventSourceType* pEventSource = null;

					//	GetOpenedEventSourceNoRef(&pEventSource));
					//	return pEventSource.Raise(ctl.as_iinspectable(this), pArgs);
					//}));
					m_tpFlyout.Opened += OnFlyoutOpened;

					//m_epFlyoutClosedHandler.AttachEventHandler(m_tpFlyout.Cast<FlyoutBase>(), 
					//	[this](IInspectable * pSender, IInspectable * pArgs)
					//{
					//	OpenedEventSourceType* pEventSource = null;

					//	put_IsCalendarOpen(false));

					//	GetClosedEventSourceNoRef(&pEventSource));
					//	return pEventSource.Raise(ctl.as_iinspectable(this), pArgs);
					//}));
					m_tpFlyout.Closed += OnFlyoutClosed;
				}
			}

			if (m_tpCalendarView is { })
			{
				// TODO UNO
				//// Forward CalendarViewDayItemChanging event from CalendarView to CalendarDatePicker
				//m_epCalendarViewCalendarViewDayItemChangingHandler.AttachEventHandler(m_tpCalendarView.Cast<CalendarView>(), 
				//	[this](ICalendarView * pSender, ICalendarViewDayItemChangingEventArgs * pArgs)
				//{
				//	CalendarViewDayItemChangingEventSourceType* pEventSource = null;

				//	GetCalendarViewDayItemChangingEventSourceNoRef(&pEventSource));
				//	return pEventSource.Raise(m_tpCalendarView, pArgs);
				//}));
				m_tpCalendarView.CalendarViewDayItemChanging += OnCalendarViewDayChanging;

				//// handle SelectedDatesChanged event
				//m_epCalendarViewSelectedDatesChangedHandler.AttachEventHandler(m_tpCalendarView.Cast<CalendarView>(), 
				//	[this](ICalendarView * pSender, ICalendarViewSelectedDatesChangedEventArgs * pArgs)
				//{
				//	return OnSelectedDatesChanged(pSender, pArgs);

				//}));

				m_tpCalendarView.SelectedDatesChanged += OnCalendarViewDatesChanged;

				// check if we requested any operations that require CalendarView template part
				if (m_isYearDecadeViewDimensionRequested)
				{
					m_isYearDecadeViewDimensionRequested = false;
					m_tpCalendarView.SetYearDecadeDisplayDimensions(m_colsInYearDecadeView, m_rowsInYearDecadeView);
				}

				if (m_isSetDisplayDateRequested)
				{
					m_isSetDisplayDateRequested = false;
					m_tpCalendarView.SetDisplayDate(m_displayDate);
				}
			}

			// we might set IsCalendarOpen to true before template is applied.
			UpdateCalendarVisibility();

			// Initialize header visibility
			UpdateHeaderVisibility();

			FormatDate();

			UpdateVisualState();

			// TODO: Uno specific: This logic should later move to Control to match WinUI
			UpdateDescriptionVisibility(true);

			void OnFlyoutOpened(object sender, object eventArgs)
			{
				IsCalendarOpen = true;
				Opened?.Invoke(this, new object());
			}

			void OnFlyoutClosed(object sender, object eventArgs)
			{
				IsCalendarOpen = false;
				Closed?.Invoke(this, new object());
			}

			void OnCalendarViewDayChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
			{
				// UNO-TODO
			}

			void OnCalendarViewDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
			{
				OnSelectedDatesChanged(sender, args);
			}
		}

		private void OnSelectedDatesChanged(
			CalendarView pSender,
			CalendarViewSelectedDatesChangedEventArgs pArgs)
		{
			global::System.Diagnostics.Debug.Assert(m_tpCalendarView is { } && pSender == m_tpCalendarView);

			if (!m_isSelectedDatesChangingInternally)
			{
				CalendarViewSelectionMode mode = CalendarViewSelectionMode.None;
				mode = m_tpCalendarView.SelectionMode;

				// We only care about single selection mode.
				// In case the calendarview's selection mode is set to multiple by developer,
				// we just silently ignore this event.
				if (mode == CalendarViewSelectionMode.Single)
				{
					IReadOnlyList<DateTimeOffset> spAddedDates;
					int addedDatesSize = 0;

					spAddedDates = pArgs.AddedDates;
					addedDatesSize = spAddedDates.Count;

					global::System.Diagnostics.Debug.Assert(addedDatesSize <= 1);

					if (addedDatesSize == 1)
					{
						DateTime newDate;
						object spNewDate;
						DateTime spNewDateReference;

						newDate = spAddedDates[0];
						PropertyValue.CreateFromDateTime(newDate, out spNewDate);
						//spNewDate.As(&spNewDateReference));
						spNewDateReference = (DateTime)spNewDate;

						IsCalendarOpen = false;

						Date = spNewDateReference;
					}
					else // date is deselected
					{
						Date = null;
					}
				}
			}
		}

		private void OnDateChanged(
			DateTime? pOldDateReference,
			DateTime? pNewDateReference)
		{
			if (pNewDateReference.HasValue) // new Date property is set.
			{
				DateTime date;
				DateTime coercedDate;
				DateTime minDate;
				DateTime maxDate;

				date = pNewDateReference.Value;

				// coerce dates
				minDate = MinDate;
				maxDate = MaxDate;
				coercedDate.UniversalTime = Math.Min(maxDate.UniversalTime, Math.Max(minDate.UniversalTime, date.UniversalTime));

				// if Date is not in the range of min/max date, we'll coerce it and trigger DateChanged again.
				if (coercedDate.UniversalTime != date.UniversalTime)
				{
					DateTime spCoercedDateReference;
					//PropertyValue.CreateFromDateTime(coercedDate, &spCoercedDateReference));
					spCoercedDateReference = coercedDate;
					Date = spCoercedDateReference;
					return;
				}
			}

			SyncDate();

			// Raise DateChanged event.
			RaiseDateChanged(pOldDateReference, pNewDateReference);

			// Update the Date text.
			FormatDate();

			UpdateVisualState();
		}

		private void RaiseDateChanged(
			DateTime? pOldDateReference,
			DateTime? pNewDateReference)
		{
			CalendarDatePickerDateChangedEventArgs spArgs;
			//DateChangedEventSourceType pEventSource = null;

			spArgs = new CalendarDatePickerDateChangedEventArgs(pNewDateReference, pOldDateReference);

			//GetDateChangedEventSourceNoRef(&pEventSource));

			//pEventSource.Raise(this, spArgs);
			DateChanged?.Invoke(this, spArgs);
		}

		public void SetYearDecadeDisplayDimensions(int columns, int rows)
		{
			if (m_tpCalendarView is { })
			{
				m_tpCalendarView.SetYearDecadeDisplayDimensions(columns, rows);
			}
			else
			{
				m_isYearDecadeViewDimensionRequested = true;
				m_colsInYearDecadeView = columns;
				m_rowsInYearDecadeView = rows;
			}
		}

		public void SetDisplayDate(global::System.DateTimeOffset date)
		{
			if (m_tpCalendarView is { })
			{
				m_tpCalendarView.SetDisplayDate(date);
			}
			else
			{
				m_isSetDisplayDateRequested = true;
				m_displayDate = date;
			}
		}

		private void UpdateCalendarVisibility()
		{
			if (m_tpFlyout is { } && m_tpRoot is { })
			{
				bool isCalendarOpen = false;

				isCalendarOpen = IsCalendarOpen;
				if (isCalendarOpen)
				{
					m_tpFlyout.ShowAt((Grid)m_tpRoot);
					SyncDate();
				}
				else
				{
					m_tpFlyout.Hide();
				}
			}
		}

		private void OnDateFormatChanged()
		{
			object spDateFormat;
			bool isUnsetValue = false;

			spDateFormat = ReadLocalValue(CalendarDatePicker.DateFormatProperty);

			DependencyPropertyFactory.IsUnsetValue(spDateFormat, out isUnsetValue);

			m_tpDateFormatter = null;

			if (!isUnsetValue) // format is set, use this format.
			{
				string dateFormat;

				//get_DateFormat(dateFormat.GetAddressOf()));
				dateFormat = DateFormat;

				if (!string.IsNullOrEmpty(dateFormat))
				{
					//wg.DateTimeFormatting.IDateTimeFormatterFactory > spFormatterFactory;
					DateTimeFormatter spFormatter;
					string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
					string strGeographicRegion = "ZZ"; // geographicRegion doesn't really matter as we have no decimal separator or grouping
					string strCalendarIdentifier;
					IEnumerable<string> spLanguages;
					string strLanguage;

					//get_CalendarIdentifier(strCalendarIdentifier.GetAddressOf()));
					strCalendarIdentifier = CalendarIdentifier;
					//get_Language(strLanguage.GetAddressOf()));
					strLanguage = Language;
					//CalendarView.CreateCalendarLanguagesStatic(std.move(strLanguage), &spLanguages));
					spLanguages = CalendarView.CreateCalendarLanguagesStatic(strLanguage);

					//ctl.GetActivationFactory(
					//	string(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter),
					//	&spFormatterFactory));

					// IFCPTR_RETURN(spFormatterFactory);

					//spFormatterFactory.CreateDateTimeFormatterContext(
					//	dateFormat,
					//	spLanguages,
					//	strGeographicRegion,
					//	strCalendarIdentifier,
					//	strClock,
					//	spFormatter.ReleaseAndGetAddressOf()));
					spFormatter = new DateTimeFormatter(
						dateFormat,
						spLanguages,
						strGeographicRegion,
						strCalendarIdentifier,
						strClock);

					//SetPtrValue(m_tpDateFormatter, spFormatter);
					m_tpDateFormatter = spFormatter;
				}
			}

			FormatDate();

			return;
		}

		private void FormatDate()
		{
			if (m_tpDateText is { })
			{
				DateTime? spDateReference;
				string dateAsString;

				spDateReference = Date;

				if (spDateReference is { })
				{
					DateTime date;

					date = spDateReference.Value;

					if (m_tpDateFormatter is { }) // when there is a formatter, use it
					{
						//m_tpDateFormatter.Format(date, dateAsString.GetAddressOf()));
						dateAsString = m_tpDateFormatter.Format(date);
					}
					else // else use system build-in shortdate formatter.
					{
						//wg.DateTimeFormatting.IDateTimeFormatterStatics > spDateTimeFormatterStatics;
						DateTimeFormatter spDateFormatter;

						//ctl.GetActivationFactory(string(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter), &spDateTimeFormatterStatics));
						//spDateTimeFormatterStatics.spDateFormatter = ShortDate;
						spDateFormatter = DateTimeFormatter.ShortDate;
						//spDateFormatter.Format(date, dateAsString.GetAddressOf()));
						dateAsString = spDateFormatter.Format(date);
					}
				}
				else
				{
					// else use placeholder text.
					//get_PlaceholderText(dateAsString.GetAddressOf()));
					dateAsString = PlaceholderText;
				}

				//m_tpDateText.put_Text(dateAsString));
				m_tpDateText.Text = dateAsString;
			}

			return;
		}


		private void UpdateHeaderVisibility()
		{
			DataTemplate spHeaderTemplate;
			object spHeader;

			spHeaderTemplate = HeaderTemplate;

			spHeader = Header;

			//ConditionallyGetTemplatePartAndUpdateVisibility(
			//	XSTRING_PTR_EPHEMERAL("HeaderContentPresenter"),
			//	(spHeader || spHeaderTemplate),

			//	m_tpHeaderContentPresenter));

			ConditionallyGetTemplatePartAndUpdateVisibility(
				"HeaderContentPresenter",
				(spHeader is { } || spHeaderTemplate is { }),
				ref m_tpHeaderContentPresenter);
		}

		//private void GetPlainText(out string strPlainText)
		//{
		//	object spHeader;
		//	strPlainText = null;

		//	spHeader = Header;
		//	if (spHeader != null)
		//	{
		//		FrameworkElement.GetStringFromObject(spHeader, out strPlainText);
		//	}
		//}


		private protected override void ChangeVisualState(bool useTransitions)
		{
			bool isEnabled = false;
			bool ignored = false;
			FocusState focusState = FocusState.Unfocused;
			DateTime? spDateReference;

			isEnabled = IsEnabled;

			focusState = FocusState;

			spDateReference = Date;

			// CommonStates VisualStateGroup.
			if (!isEnabled)
			{
				GoToState(useTransitions, "Disabled", out ignored);
			}
			else if (m_isPressedOnMain)
			{
				GoToState(useTransitions, "Pressed", out ignored);
			}

			else if (m_isPointerOverMain)
			{
				GoToState(useTransitions, "PointerOver", out ignored);
			}

			else
			{
				GoToState(useTransitions, "Normal", out ignored);
			}

			// FocusStates VisualStateGroup.
			if (FocusState.Unfocused != focusState && isEnabled)
			{
				if (FocusState.Pointer == focusState)
				{
					GoToState(useTransitions, "PointerFocused", out ignored);
				}
				else
				{
					GoToState(useTransitions, "Focused", out ignored);
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused", out ignored);
			}

			// SelectionStates VisualStateGroup.
			if (spDateReference is { } && isEnabled)
			{
				GoToState(useTransitions, "Selected", out ignored);
			}
			else
			{
				GoToState(useTransitions, "Unselected", out ignored);
			}
		}

		// Responds to the KeyDown event.
		protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
		{
			//CalendarDatePickerGenerated.OnKeyDown(pArgs);

			bool isHandled = false;

			isHandled = pArgs.Handled;
			if (isHandled)
			{
				return;
			}

			VirtualKey key = VirtualKey.None;
			VirtualKeyModifiers modifiers = GetKeyboardModifiers();

			key = pArgs.Key;

			if (modifiers == VirtualKeyModifiers.None)
			{
				switch (key)
				{
					case VirtualKey.Enter:
					case VirtualKey.Space:
						IsCalendarOpen = true;
						break;
					default:
						break;
				}
			}

			return;
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
		{
			//CalendarDatePickerGenerated.OnPointerPressed(pArgs);

			bool isHandled = false;

			isHandled = pArgs.Handled;
			if (isHandled)
			{
				return;
			}

			bool isEnabled = false;

			isEnabled = IsEnabled;

			if (!isEnabled)
			{
				return;
			}

			bool isEventSourceTarget = false;

			IsEventSourceTarget((PointerRoutedEventArgs)(pArgs), out isEventSourceTarget);

			if (isEventSourceTarget)
			{
				bool isLeftButtonPressed = false;

				PointerPoint spPointerPoint;
				PointerPointProperties spPointerProperties;

				spPointerPoint = pArgs.GetCurrentPoint(this);
				//// IFCPTR_RETURN(spPointerPoint);

				spPointerProperties = spPointerPoint.Properties;
				//// IFCPTR_RETURN(spPointerProperties);
				isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;

				if (isLeftButtonPressed)
				{
					pArgs.Handled = true;
					m_isPressedOnMain = true;
					// for "Pressed" visual state to render
					UpdateVisualState();
				}
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
		{
			//CalendarDatePickerGenerated.OnPointerReleased(pArgs);

			bool isHandled = false;

			isHandled = pArgs.Handled;
			if (isHandled)
			{
				return;
			}

			bool isEnabled = false;

			isEnabled = IsEnabled;

			if (!isEnabled)
			{
				return;
			}

			bool isEventSourceTarget = false;

			IsEventSourceTarget((PointerRoutedEventArgs)(pArgs), out isEventSourceTarget);

			if (isEventSourceTarget)
			{
				bool isLeftButtonPressed = false;

				PointerPoint spPointerPoint;
				PointerPointProperties spPointerProperties;

				spPointerPoint = pArgs.GetCurrentPoint(this);
				// IFCPTR_RETURN(spPointerPoint);

				spPointerProperties = spPointerPoint.Properties;
				// IFCPTR_RETURN(spPointerProperties);
				isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;

				VirtualKeyModifiers modifiers = GetKeyboardModifiers();

				m_shouldPerformActions = (m_isPressedOnMain && !isLeftButtonPressed
					&& modifiers == VirtualKeyModifiers.None);

				if (m_shouldPerformActions)
				{
					m_isPressedOnMain = false;

					pArgs.Handled = true;
				}

				GestureModes gestureFollowing = GestureModes.None;
				gestureFollowing = ((PointerRoutedEventArgs)pArgs).GestureFollowing;
				if (gestureFollowing == GestureModes.RightTapped)
				{
					// This will be released OnRightTappedUnhandled or destructor.
					return;
				}

				PerformPointerUpAction();

				UpdateVisualState();
			}
		}

		private protected override void OnRightTappedUnhandled(
			RightTappedRoutedEventArgs pArgs)
		{
			//CalendarDatePickerGenerated.OnRightTappedUnhandled(pArgs);

			bool isHandled = false;

			isHandled = pArgs.Handled;
			if (isHandled)
			{
				return;
			}

			bool isEventSourceTarget = false;

			IsEventSourceTarget(((RightTappedRoutedEventArgs)pArgs), out isEventSourceTarget);

			if (isEventSourceTarget)
			{
				PerformPointerUpAction();
			}
		}


		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
		{
			bool isEventSourceTarget = false;

			//CalendarDatePickerGenerated.OnPointerEntered(pArgs);

			IsEventSourceTarget(pArgs, out isEventSourceTarget);

			if (isEventSourceTarget)
			{
				m_isPointerOverMain = true;
				m_isPressedOnMain = false;
				UpdateVisualState();
			}
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
		{
			bool isEventSourceTarget = false;

			//CalendarDatePickerGenerated.OnPointerMoved(pArgs);

			IsEventSourceTarget(pArgs, out isEventSourceTarget);

			if (isEventSourceTarget)
			{
				if (!m_isPointerOverMain)
				{
					m_isPointerOverMain = true;
					UpdateVisualState();
				}
			}

			else if (m_isPointerOverMain)
			{
				// treat as PointerExited.
				m_isPointerOverMain = false;
				m_isPressedOnMain = false;
				UpdateVisualState();
			}
		}

		protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
		{
			//CalendarDatePickerGenerated.OnPointerExited(pArgs);

			m_isPointerOverMain = false;

			m_isPressedOnMain = false;

			UpdateVisualState();
		}

		protected override void OnGotFocus(RoutedEventArgs pArgs)
		{
			UpdateVisualState();
		}

		protected override void OnLostFocus(RoutedEventArgs pArgs)
		{
			UpdateVisualState();
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerPoint spPointerPoint;
			global::Windows.Devices.Input.PointerDevice spPointerDevice;
			PointerDeviceType nPointerDeviceType = PointerDeviceType.Touch;

			//CalendarDatePickerGenerated.OnPointerCaptureLost(pArgs);

			spPointer = pArgs.Pointer;

			// For touch, we can clear PointerOver when receiving PointerCaptureLost, which we get when the finger is lifted
			// or from cancellation, e.g. pinch-zoom gesture in ScrollViewer.
			// For mouse, we need to wait for PointerExited because the mouse may still be above the control when
			// PointerCaptureLost is received from clicking.
			spPointerPoint = pArgs.GetCurrentPoint(null);

			// IFCPTR_RETURN(spPointerPoint);

			spPointerDevice = spPointerPoint.PointerDevice;

			// IFCPTR_RETURN(spPointerDevice);

			nPointerDeviceType = (PointerDeviceType)spPointerDevice.PointerDeviceType;
			if (nPointerDeviceType == PointerDeviceType.Touch)
			{
				m_isPointerOverMain = false;
			}

			m_isPressedOnMain = false;

			UpdateVisualState();
		}

		// Called when the IsEnabled property changes.
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
		{
			UpdateVisualState();
		}

		// Perform the primary action related to pointer up.
		private void PerformPointerUpAction()
		{
			if (m_shouldPerformActions)
			{
				m_shouldPerformActions = false;
				IsCalendarOpen = true;

				var soundPlayerService = DXamlCore.Current.GetElementSoundPlayerServiceNoRef();
				soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);
			}
		}


		private void IsEventSourceTarget(
			RoutedEventArgs pArgs,
			out bool pIsEventSourceChildOfTarget)
		{
			object spOriginalSourceAsI;
			DependencyObject spOriginalSourceAsDO;

			spOriginalSourceAsI = pArgs.OriginalSource;

			spOriginalSourceAsDO = spOriginalSourceAsI as DependencyObject;

			IsChildOfTarget(
				spOriginalSourceAsDO,
				true, /* doCacheResult */

				out pIsEventSourceChildOfTarget);
		}

		// Used in hit-testing for the CalendarDatePicker target area, which must exclude the header
		private void IsChildOfTarget(
			DependencyObject pChild,
			bool doCacheResult,
			out bool pIsChildOfTarget)
		{
			// Simple perf optimization: most pointer events have the same source as the previous
			// event, so we'll cache the most recent result and reuse it whenever possible.
			DependencyObject pMostRecentSearchChildNoRef = null;

			bool mostRecentResult = false;

			pIsChildOfTarget = false;

			if (pChild is { })
			{
				bool result = mostRecentResult;
				DependencyObject spHeaderPresenterAsDO;
				DependencyObject spCurrentDO = pChild;
				DependencyObject spParentDO;
				DependencyObject pThisAsDONoRef = (DependencyObject)this;
				bool isFound = false;

				spHeaderPresenterAsDO = m_tpHeaderContentPresenter as DependencyObject;

				while (spCurrentDO is { } && !isFound)
				{
					if (spCurrentDO == pMostRecentSearchChildNoRef)
					{
						// use the cached result
						isFound = true;
					}
					else if (spCurrentDO == pThisAsDONoRef)
					{
						// meet the CalendarDatePicker itself, break;
						result = true;
						isFound = true;
					}
					else if (spHeaderPresenterAsDO is { } && spCurrentDO == spHeaderPresenterAsDO)
					{
						// meet the Header, break;
						result = false;
						isFound = true;
					}
					else
					{
						spParentDO = VisualTreeHelper.GetParent(spCurrentDO);

						// refcounting note: Attach releases the previously stored ptr, and does not
						// addthe new one.
						spCurrentDO = spParentDO;
					}
				}

				if (!isFound)
				{
					result = false;
				}

				if (doCacheResult is { })
				{
					pMostRecentSearchChildNoRef = pChild;
					mostRecentResult = result;
				}

				pIsChildOfTarget = result;
			}
		}

		private void SyncDate()
		{
			if (m_tpCalendarView is { })
			{
				bool isCalendarOpen = false;

				isCalendarOpen = IsCalendarOpen;
				if (isCalendarOpen)
				{
					DateTime? spDateReference;
					IList<DateTimeOffset> spSelectedDates;

					m_isSelectedDatesChangingInternally = true;
					//var selectedDatesChangingGuard = wil.scope_exit([&] {
					//	m_isSelectedDatesChangingInternally = false;
					//});
					try
					{

						spDateReference = Date;

						spSelectedDates = m_tpCalendarView.SelectedDates;
						spSelectedDates.Clear();

						if (spDateReference is { })
						{
							DateTime date;

							date = (DateTime)spDateReference;
							// if Date property is being set, we should always display the Date when Calendar is open.
							SetDisplayDate(date);
							spSelectedDates.Add(date);
						}
					}
					finally
					{
						m_isSelectedDatesChangingInternally = false;
					}
				}
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			// IFCPTR_RETURN(ppAutomationPeer);
			//ppAutomationPeer = null;

			//CalendarDatePickerAutomationPeer spAutomationPeer;

			//ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarDatePickerAutomationPeer, GetHandle(), spAutomationPeer.GetAddressOf()));

			//spAutomationPeer.Owner = this;
			//ppAutomationPeer = spAutomationPeer.Detach();

			return new CalendarDatePickerAutomationPeer(this);
		}

		internal void GetCurrentFormattedDate(out string value)
		{
			value = null;

			if (m_tpDateText is { })
			{
				value = m_tpDateText.Text;
			}
		}

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description == null)
			{
				// Avoid loading DescriptionPresenter element in template if not needed.
				return;
			}

			var descriptionPresenter = this.FindName("DescriptionPresenter") as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
