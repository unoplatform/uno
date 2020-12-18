//// Copyright (c) Microsoft Corporation. All rights reserved.
//// Licensed under the MIT License. See LICENSE in the project root for license information.

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using Windows.Devices.Input;
//using Windows.Globalization.DateTimeFormatting;
//using Windows.System;
//using Windows.UI.Input;
//using Windows.UI.Xaml.Automation.Peers;
//using Windows.UI.Xaml.Controls.Primitives;
//using Windows.UI.Xaml.Input;
//using Windows.UI.Xaml.Media;
//using DateTime = System.DateTimeOffset;

//namespace Windows.UI.Xaml.Controls
//{
//	public partial class CalendarDatePicker : Control
//	{
//		private CalendarView m_tpCalendarView;
//		private ContentPresenter m_tpHeaderContentPresenter;
//		private TextBlock m_tpDateText;
//		private Grid m_tpRoot;
//		private FlyoutBase m_tpFlyout;
//		private event FlyoutBaseOpenedEventCallback m_epFlyoutOpenedHandler;
//		private event FlyoutBaseClosedEventCallback m_epFlyoutClosedHandler;
//		private event CalendarViewCalendarViewDayItemChangingCallback m_epCalendarViewCalendarViewDayItemChangingHandler;
//		private event CalendarViewSelectedDatesChangedCallback m_epCalendarViewSelectedDatesChangedHandler;

//		private int m_colsInYearDecadeView;
//		private int m_rowsInYearDecadeView;

//		private DateTime m_displayDate;

//		private DateTimeFormatter m_tpDateFormatter;


//		// if CalendView template part is not ready, we delay this request.
//		private bool m_isYearDecadeViewDimensionRequested;

//		// if CalendView template part is not ready, we delay this request.
//		private bool m_isSetDisplayDateRequested;

//		// true when pointer is over the control but not on the header.
//		private bool m_isPointerOverMain;

//		// true when pointer is pressed on the control but not on the header.
//		private bool m_isPressedOnMain;

//		// On pointer released we perform some actions depending on control. We decide to whether to perform them
//		// depending on some parameters including but not limited to whether released is followed by a pressed, which
//		// mouse button is pressed, what type of pointer is it etc. This bool keeps our decision.
//		private bool m_shouldPerformActions;

//		private bool m_isSelectedDatesChangingInternally;

//		public CalendarDatePicker()
//		{
//			m_isYearDecadeViewDimensionRequested = false;
//			m_colsInYearDecadeView = 0;
//			m_rowsInYearDecadeView = 0;
//			m_isSetDisplayDateRequested = false;
//			m_displayDate = default;
//			m_isPointerOverMain = false;
//			m_isPressedOnMain = false;
//			m_shouldPerformActions = false;
//			m_isSelectedDatesChangingInternally = false;
//		}

//		private void PrepareState()
//		{
//			CalendarDatePickerGenerated.PrepareState();

//			// Set a default string as the PlaceholderText property value.
//			string strDefaultPlaceholderText;

//			DXamlCore.GetCurrent().GetLocalizedResourceString(TEXT_CALENDARDATEPICKER_DEFAULT_PLACEHOLDER_TEXT, strDefaultPlaceholderText.ReleaseAndGetAddressOf()));

//			PlaceholderText = strDefaultPlaceholderText;

//			return;
//		}

//		private void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
//		{
//			CalendarDatePickerGenerated.OnPropertyChanged2(args);

//			switch (args.Property)
//			{
//				case KnownPropertyIndex.CalendarDatePicker_IsCalendarOpen:
//				{
//					UpdateCalendarVisibility();
//					break;
//				}

//				case KnownPropertyIndex.CalendarDatePicker_Date:
//				{
//					DateTime spNewDateReference;
//					DateTime spOldDateReference;
//					CValueBoxer.UnboxValue<DateTime>(args.OldValue, spOldDateReference);
//					CValueBoxer.UnboxValue<DateTime>(args.NewValue, spNewDateReference);
//					OnDateChanged(spOldDateReference, spNewDateReference);
//					break;
//				}

//				case KnownPropertyIndex.FrameworkElement_Language:
//				case KnownPropertyIndex.CalendarDatePicker_CalendarIdentifier:
//				case KnownPropertyIndex.CalendarDatePicker_DateFormat:
//				{
//					OnDateFormatChanged();
//					break;
//				}

//				case KnownPropertyIndex.CalendarDatePicker_Header:
//				case KnownPropertyIndex.CalendarDatePicker_HeaderTemplate:
//				{
//					UpdateHeaderVisibility();
//					break;
//				}

//				case KnownPropertyIndex.CalendarDatePicker_LightDismissOverlayMode:
//				{
//					if (m_tpFlyout is {})
//					{
//						// Forward the new property onto our flyout.
//						var overlayMode = LightDismissOverlayMode.Off;
//						overlayMode = LightDismissOverlayMode;
//						((FlyoutBase)m_tpFlyout).LightDismissOverlayMode = overlayMode;
//					}

//					break;
//				}

//#if WI_IS_FEATURE_PRESENT //(Feature_HeaderPlacement)
//				case KnownPropertyIndex.CalendarDatePicker_HeaderPlacement:
//				{
//					UpdateVisualState());
//					break;
//				}
//#endif
//			}
//		}

//		protected override void OnApplyTemplate()
//		{
//			CalendarView spCalendarView;
//			TextBlock spDateText;
//			Grid spRoot;

//			DetachHandler(m_epFlyoutOpenedHandler, m_tpFlyout);
//			DetachHandler(m_epFlyoutClosedHandler, m_tpFlyout);
//			DetachHandler(m_epCalendarViewCalendarViewDayItemChangingHandler, m_tpCalendarView);
//			DetachHandler(m_epCalendarViewSelectedDatesChangedHandler, m_tpCalendarView);

//			m_tpCalendarView = null;

//			m_tpHeaderContentPresenter = default;

//			m_tpDateText = default;

//			m_tpFlyout = default;

//			m_tpRoot = default;

//			CalendarDatePickerGenerated.OnApplyTemplate();

//			spCalendarView = GetTemplatePart("CalendarView") as CalendarView;

//			spDateText = GetTemplatePart("DateText") as TextBlock;

//			spRoot = GetTemplatePart("Root") as Grid;

//			if (m_tpRoot is {})
//			{
//				FlyoutBase spFlyout;

//				FlyoutBaseFactory.GetAttachedFlyoutStatic(m_tpRoot.Cast<Grid>(), spFlyout.ReleaseAndGetAddressOf()));
//				SetPtrValue(m_tpFlyout, spFlyout);

//				if (m_tpFlyout is {})
//				{
//					// by default flyoutbase will resize the presenter's content (here is the CalendarView) if there is
//					// not enough space, because the content is inside a ScrollViewer.
//					// however CalendarView has 3 huge scrollviewers, put CalendarView in a scrollviewer will have a very
//					// bad user experience because user can't scroll the outer scrollviewer until the inner scrollviewer
//					// hits the end of content.
//					// we decide to remove the scrollviewer from presenter's template and let flyoutbase not resize us.
//					m_tpFlyout.DisablePresenterResizing();

//					// Forward the value of LightDismissOverlayMode to our flyout.
//					var overlayMode = LightDismissOverlayMode.Off;
//					m_tpFlyout.LightDismissOverlayMode = overlayMode;

//					// TODO UNO
//					//m_epFlyoutOpenedHandler.AttachEventHandler(m_tpFlyout.Cast<FlyoutBase>(), 
//					//	[this](IInspectable * pSender, IInspectable * pArgs)
//					//{
//					//	OpenedEventSourceType* pEventSource = null;

//					//	GetOpenedEventSourceNoRef(&pEventSource));
//					//	return pEventSource.Raise(ctl.as_iinspectable(this), pArgs);
//					//}));


//					//m_epFlyoutClosedHandler.AttachEventHandler(m_tpFlyout.Cast<FlyoutBase>(), 
//					//	[this](IInspectable * pSender, IInspectable * pArgs)
//					//{
//					//	OpenedEventSourceType* pEventSource = null;

//					//	put_IsCalendarOpen(FALSE));

//					//	GetClosedEventSourceNoRef(&pEventSource));
//					//	return pEventSource.Raise(ctl.as_iinspectable(this), pArgs);
//					//}));
//				}
//			}

//			if (m_tpCalendarView is {})
//			{
//				// TODO UNO
//				//// Forward CalendarViewDayItemChanging event from CalendarView to CalendarDatePicker
//				//m_epCalendarViewCalendarViewDayItemChangingHandler.AttachEventHandler(m_tpCalendarView.Cast<CalendarView>(), 
//				//	[this](ICalendarView * pSender, ICalendarViewDayItemChangingEventArgs * pArgs)
//				//{
//				//	CalendarViewDayItemChangingEventSourceType* pEventSource = null;

//				//	GetCalendarViewDayItemChangingEventSourceNoRef(&pEventSource));
//				//	return pEventSource.Raise(m_tpCalendarView, pArgs);
//				//}));

//				//// handle SelectedDatesChanged event
//				//m_epCalendarViewSelectedDatesChangedHandler.AttachEventHandler(m_tpCalendarView.Cast<CalendarView>(), 
//				//	[this](ICalendarView * pSender, ICalendarViewSelectedDatesChangedEventArgs * pArgs)
//				//{
//				//	return OnSelectedDatesChanged(pSender, pArgs);

//				//}));

//				// check if we requested any operations that require CalendarView template part
//				if (m_isYearDecadeViewDimensionRequested)
//				{
//					m_isYearDecadeViewDimensionRequested = false;
//					m_tpCalendarView.SetYearDecadeDisplayDimensions(m_colsInYearDecadeView, m_rowsInYearDecadeView));
//				}

//				if (m_isSetDisplayDateRequested)
//				{
//					m_isSetDisplayDateRequested = false;
//					m_tpCalendarView.SetDisplayDate(m_displayDate));
//				}
//			}

//			// we might set IsCalendarOpen to true before template is applied.
//			UpdateCalendarVisibility();

//			// Initialize header visibility
//			UpdateHeaderVisibility();

//			FormatDate();

//			UpdateVisualState();
//		}

//		private void OnSelectedDatesChanged(
//			CalendarView pSender,
//			CalendarViewSelectedDatesChangedEventArgs pArgs)
//		{
//			Debug.Debug.Assert(m_tpCalendarView is {} && pSender == m_tpCalendarView);

//			if (!m_isSelectedDatesChangingInternally)
//			{
//				CalendarViewSelectionMode mode = CalendarViewSelectionMode.None;
//				m_tpCalendarView.SelectionMode = mode;

//				// We only care about single selection mode.
//				// In case the calendarview's selection mode is set to multiple by developer,
//				// we just silently ignore this event.
//				if (mode == CalendarViewSelectionMode.Single)
//				{
//					wfc.IVectorView<DateTime>> spAddedDates;
//					unsigned addedDatesSize = 0;

//					spAddedDates = pArgs.AddedDates;
//					addedDatesSize = spAddedDates.Size;

//					Debug.Assert(addedDatesSize <= 1);

//					if (addedDatesSize == 1)
//					{
//						DateTime newDate;
//						object spNewDate;
//						DateTime spNewDateReference;

//						newDate = spAddedDates.GetAt(0);
//						PropertyValue.CreateFromDateTime(newDate, spNewDate);
//						spNewDate.As(&spNewDateReference));

//						IsCalendarOpen = false;

//						Date = spNewDateReference;
//					}
//					else // date is deselected
//					{
//						Date = null;
//					}
//				}
//			}		}

//		private void OnDateChanged(
//			DateTime? pOldDateReference,
//			DateTime? pNewDateReference)
//		{
//			if (pNewDateReference.HasValue) // new Date property is set.
//			{
//				DateTime date;
//				DateTime coercedDate;
//				DateTime minDate;
//				DateTime maxDate;

//				date = pNewDateReference.Value;

//				// coerce dates
//				minDate = MinDate;
//				maxDate = MaxDate;
//				coercedDate.UniversalTime = Math.Min(maxDate.UniversalTime, Math.Max(minDate.UniversalTime, date.UniversalTime));

//				// if Date is not in the range of min/max date, we'll coerce it and trigger DateChanged again.
//				if (coercedDate.UniversalTime != date.UniversalTime)
//				{
//					DateTime spCoercedDateReference;
//					PropertyValue.CreateFromDateTime(coercedDate, &spCoercedDateReference));
//					Date = spCoercedDateReference;
//					return;
//				}
//			}

//			SyncDate();

//			// Raise DateChanged event.
//			RaiseDateChanged(pOldDateReference, pNewDateReference);

//			// Update the Date text.
//			FormatDate();

//			UpdateVisualState();
//		}

//		private void RaiseDateChanged(
//			DateTime? pOldDateReference,
//			DateTime? pNewDateReference)
//		{
//			CalendarDatePickerDateChangedEventArgs spArgs;
//			DateChangedEventSourceType pEventSource = null;

//			spArgs = new CalendarDatePickerDateChangedEventArgs(pNewDateReference, pOldDateReference);

//			GetDateChangedEventSourceNoRef(&pEventSource));

//			pEventSource.Raise(this, spArgs);
//		}

//		private void SetYearDecadeDisplayDimensionsImpl(int columns, int rows)
//		{
//			if (m_tpCalendarView is {})
//			{
//				m_tpCalendarView.SetYearDecadeDisplayDimensions(columns, rows);
//			}
//			else
//			{
//				m_isYearDecadeViewDimensionRequested = true;
//				m_colsInYearDecadeView = columns;
//				m_rowsInYearDecadeView = rows;
//			}
//		}

//		private void SetDisplayDateImpl( DateTime date)
//		{
//			if (m_tpCalendarView is {})
//			{
//				m_tpCalendarView.SetDisplayDate(date);
//			}
//			else
//			{
//				m_isSetDisplayDateRequested = true;
//				m_displayDate = date;
//			}
//		}

//		private void UpdateCalendarVisibility()
//		{
//			if (m_tpFlyout is {} && m_tpRoot is { })
//			{
//				bool isCalendarOpen = false;

//				isCalendarOpen = IsCalendarOpen;
//				if (isCalendarOpen)
//				{
//					m_tpFlyout.ShowAt((Grid)m_tpRoot);
//					SyncDate();
//				}
//				else
//				{
//					m_tpFlyout.Hide());
//				}
//			}
//		}

//		private void OnDateFormatChanged()
//		{
//			object spDateFormat;
//			bool isUnsetValue = FALSE;

//			ReadLocalValue(
//				MetadataAPI.GetDependencyPropertyByIndex(KnownPropertyIndex.CalendarDatePicker_DateFormat),
//				&spDateFormat));

//			DependencyPropertyFactory.IsUnsetValue(spDateFormat, isUnsetValue));

//			m_tpDateFormatter.Clear();

//			if (!isUnsetValue) // format is set, use this format.
//			{
//				wrl_wrappers.HString dateFormat;

//				get_DateFormat(dateFormat.GetAddressOf()));

//				if (!dateFormat.IsEmpty())
//				{
//					wg.DateTimeFormatting.IDateTimeFormatterFactory> spFormatterFactory;
//					wg.DateTimeFormatting.IDateTimeFormatter> spFormatter;
//					wrl_wrappers.Hconst string strClock = "24HourClock"; // it doesn't matter if it is 24 or 12 hour clock
//					wrl_wrappers.Hconst string strGeographicRegion = "ZZ"; // geographicRegion doesn't really matter as we have no decimal separator or grouping
//					wrl_wrappers.HString strCalendarIdentifier;
//					wfc.IEnumerable<HSTRING>> spLanguages;
//					wrl_wrappers.HString strLanguage;

//					get_CalendarIdentifier(strCalendarIdentifier.GetAddressOf()));
//					get_Language(strLanguage.GetAddressOf()));
//					CalendarView.CreateCalendarLanguagesStatic(std.move(strLanguage), &spLanguages));

//					ctl.GetActivationFactory(
//						wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter),
//						&spFormatterFactory));

//					// IFCPTR_RETURN(spFormatterFactory);

//					spFormatterFactory.CreateDateTimeFormatterContext(
//						dateFormat,
//						spLanguages,
//						strGeographicRegion,
//						strCalendarIdentifier,
//						strClock,
//						spFormatter.ReleaseAndGetAddressOf()));

//					SetPtrValue(m_tpDateFormatter, spFormatter);
//				}
//			}

//			FormatDate());

//			return;
//		}

//		private void FormatDate()
//		{
//			if (m_tpDateText)
//			{
//				DateTime>> spDateReference;
//				wrl_wrappers.HString dateAsString;

//				spDateReference = Date;

//				if (spDateReference)
//				{
//					DateTime date;

//					spDateReference.date = Value;

//					if (m_tpDateFormatter) // when there is a formatter, use it
//					{
//						m_tpDateFormatter.Format(date, dateAsString.GetAddressOf()));
//					}
//					else // else use system build-in shortdate formatter.
//					{
//						wg.DateTimeFormatting.IDateTimeFormatterStatics> spDateTimeFormatterStatics;
//						wg.DateTimeFormatting.IDateTimeFormatter> spDateFormatter;

//						ctl.GetActivationFactory(wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_DateTimeFormatting_DateTimeFormatter), &spDateTimeFormatterStatics));
//						spDateTimeFormatterStatics.spDateFormatter = ShortDate;
//						spDateFormatter.Format(date, dateAsString.GetAddressOf()));
//					}
//				}
//				else
//				{
//					// else use placeholder text.
//					get_PlaceholderText(dateAsString.GetAddressOf()));
//				}

//				m_tpDateText.put_Text(dateAsString));
//			}

//			return;
//		}


//		private void UpdateHeaderVisibility()
//		{
//			xaml.IDataTemplate> spHeaderTemplate;
//			object spHeader;

//			spHeaderTemplate = HeaderTemplate;

//			spHeader = Header;

//			ConditionallyGetTemplatePartAndUpdateVisibility(
//				XSTRING_PTR_EPHEMERAL("HeaderContentPresenter"),
//				(spHeader || spHeaderTemplate),

//				m_tpHeaderContentPresenter));
//		}

//		private void GetPlainText(out string strPlainText)
//		{
//			object spHeader;
//			strPlainText = null;

//			pHeader = Header;
//			if (spHeader != null)
//			{
//				FrameworkElement.GetStringFromObject(spHeader, strPlainText));
//			}
//		}

//		private void ChangeVisualState(bool useTransitions)
//		{
//			bool isEnabled = false;
//			bool ignored = false;
//			FocusState focusState = FocusState.Unfocused;
//			DateTime spDateReference;

//			isEnabled = IsEnabled;

//			focusState = FocusState;

//			spDateReference = Date;

//			// CommonStates VisualStateGroup.
//			if (!isEnabled)
//			{
//				GoToState(useTransitions, "Disabled", ignored);
//			}
//			else if (m_isPressedOnMain)
//			{
//				GoToState(useTransitions, "Pressed", ignored);
//			}

//			else if (m_isPointerOverMain)
//			{
//				GoToState(useTransitions, "PointerOver", ignored);
//			}

//			else
//			{
//				GoToState(useTransitions, "Normal", ignored);
//			}

//			// FocusStates VisualStateGroup.
//			if (FocusState_Unfocused != focusState && isEnabled)
//			{
//				if (FocusState_Pointer == focusState)
//				{
//					GoToState(useTransitions, "PointerFocused", ignored);
//				}
//				else
//				{
//					GoToState(useTransitions, "Focused", ignored);
//				}
//			}
//			else
//			{
//				GoToState(useTransitions, "Unfocused", ignored);
//			}

//			// SelectionStates VisualStateGroup.
//			if (spDateReference && isEnabled)
//			{
//				GoToState(useTransitions, "Selected", ignored);
//			}
//			else
//			{
//				GoToState(useTransitions, "Unselected", ignored);
//			}

//#if WI_IS_FEATURE_PRESENT(Feature_HeaderPlacement)
//    // HeaderStates VisualStateGroup.
//    ControlHeaderPlacement headerPlacement = ControlHeaderPlacement_Top;
//    headerPlacement = HeaderPlacement;

//    switch (headerPlacement)
//    {
//        case DirectUI.ControlHeaderPlacement.Top:
//            GoToState(useTransitions, "TopHeader", ignored));
//            break;

//        case DirectUI.ControlHeaderPlacement.Left:
//            GoToState(useTransitions, "LeftHeader", ignored));
//            break;
//    }
//#endif
//		}

//		// Responds to the KeyDown event.
//		private void OnKeyDown(KeyRoutedEventArgs pArgs)
//		{
//			CalendarDatePickerGenerated.OnKeyDown(pArgs);

//			bool isHandled = false;

//			isHandled = pArgs.Handled;
//			if (isHandled)
//			{
//				return;
//			}

//			VirtualKey key = VirtualKey.None;
//			VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;

//			GetKeyboardModifiers(modifiers);

//			key = pArgs.Key;

//			if (modifiers == VirtualKeyModifiers.None)
//			{
//				switch (key)
//				{
//					case VirtualKey.Enter:
//					case VirtualKey.Space:
//						IsCalendarOpen = true;
//						break;
//					default:
//						break;
//				}
//			}

//			return;
//		}

//		private void OnPointerPressed(PointerRoutedEventArgs pArgs)
//		{
//			CalendarDatePickerGenerated.OnPointerPressed(pArgs);

//			bool isHandled = false;

//			isHandled = pArgs.Handled;
//			if (isHandled)
//			{
//				return;
//			}

//			bool isEnabled = false;

//			isEnabled = IsEnabled;

//			if (!isEnabled)
//			{
//				return;
//			}

//			bool isEventSourceTarget = false;

//			IsEventSourceTarget((PointerRoutedEventArgs)(pArgs), &isEventSourceTarget));

//			if (isEventSourceTarget)
//			{
//				bool isLeftButtonPressed = false;

//				PointerPoint spPointerPoint;
//				PointerPointProperties spPointerProperties;

//				spPointerPoint = pArgs.GetCurrentPoint(this);
//				//// IFCPTR_RETURN(spPointerPoint);

//				spPointerProperties = spPointerPoint.Properties;
//				//// IFCPTR_RETURN(spPointerProperties);
//				isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;

//				if (isLeftButtonPressed)
//				{
//					pArgs.Handled = true;
//					m_isPressedOnMain = true;
//					// for "Pressed" visual state to render
//					UpdateVisualState();
//				}
//			}
//		}

//		private void OnPointerReleased(PointerRoutedEventArgs pArgs)
//		{
//			CalendarDatePickerGenerated.OnPointerReleased(pArgs));

//			bool isHandled = false;

//			pArgs.isHandled = Handled;
//			if (isHandled)
//			{
//				return;
//			}

//			bool isEnabled = false;

//			isEnabled = IsEnabled;

//			if (!isEnabled)
//			{
//				return;
//			}

//			bool isEventSourceTarget = false;

//			IsEventSourceTarget((PointerRoutedEventArgs)(pArgs), &isEventSourceTarget));

//			if (isEventSourceTarget)
//			{
//				bool isLeftButtonPressed = false;

//				PointerPoint spPointerPoint;
//				PointerPointProperties spPointerProperties;

//				spPointerPoint = pArgs.GetCurrentPoint(this);
//				// IFCPTR_RETURN(spPointerPoint);

//				spPointerProperties = spPointerPoint.Properties;
//				// IFCPTR_RETURN(spPointerProperties);
//				isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;

//				VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;

//				GetKeyboardModifiers(&modifiers);

//				m_shouldPerformActions = (m_isPressedOnMain && !isLeftButtonPressed
//					&& modifiers == VirtualKeyModifiers.None);

//				if (m_shouldPerformActions)
//				{
//					m_isPressedOnMain = false;

//					pArgs.Handled = true;
//				}

//				GestureModes gestureFollowing = GestureModes.None;
//				gestureFollowing = ((PointerRoutedEventArgs)pArgs).GestureFollowing;
//				if (gestureFollowing == GestureModes.RightTapped)
//				{
//					// This will be released OnRightTappedUnhandled or destructor.
//					return;
//				}

//				PerformPointerUpAction();

//				UpdateVisualState();
//			}
//		}

//		private void OnRightTappedUnhandled(
//			RightTappedRoutedEventArgs pArgs)
//		{
//			CalendarDatePickerGenerated.OnRightTappedUnhandled(pArgs);

//			bool isHandled = false;

//			isHandled = pArgs.Handled;
//			if (isHandled)
//			{
//				return;
//			}

//			bool isEventSourceTarget = false;

//			IsEventSourceTarget(((RightTappedRoutedEventArgs)pArgs), isEventSourceTarget);

//			if (isEventSourceTarget)
//			{
//				PerformPointerUpAction();
//			}
//		}


//		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
//		{
//			bool isEventSourceTarget = false;

//			CalendarDatePickerGenerated.OnPointerEntered(pArgs);

//			isEventSourceTarget = IsEventSourceTarget(pArgs);

//			if (isEventSourceTarget)
//			{
//				m_isPointerOverMain = true;
//				m_isPressedOnMain = false;
//				UpdateVisualState();
//			}
//		}

//		protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
//		{
//			bool isEventSourceTarget = false;

//			CalendarDatePickerGenerated.OnPointerMoved(pArgs);

//			isEventSourceTarget = IsEventSourceTarget(pArgs);

//			if (isEventSourceTarget)
//			{
//				if (!m_isPointerOverMain)
//				{
//					m_isPointerOverMain = true;
//					UpdateVisualState();
//				}
//			}

//			else if (m_isPointerOverMain)
//			{
//				// treat as PointerExited.
//				m_isPointerOverMain = false;
//				m_isPressedOnMain = false;
//				UpdateVisualState();
//			}
//		}

//		protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
//		{
//			CalendarDatePickerGenerated.OnPointerExited(pArgs);

//			m_isPointerOverMain = false;

//			m_isPressedOnMain = false;

//			UpdateVisualState();
//		}

//		protected override void OnGotFocus(RoutedEventArgs pArgs)
//		{
//			return UpdateVisualState();
//		}

//		protected override void OnLostFocus(RoutedEventArgs pArgs)
//		{
//			return UpdateVisualState();
//		}

//		protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
//		{
//			Pointer spPointer;
//			PointerPoint spPointerPoint;
//			PointerDevice spPointerDevice;
//			PointerDeviceType nPointerDeviceType = PointerDeviceType.Touch;

//			CalendarDatePickerGenerated.OnPointerCaptureLost(pArgs);

//			spPointer = Pointer;

//			// For touch, we can clear PointerOver when receiving PointerCaptureLost, which we get when the finger is lifted
//			// or from cancellation, e.g. pinch-zoom gesture in ScrollViewer.
//			// For mouse, we need to wait for PointerExited because the mouse may still be above the control when
//			// PointerCaptureLost is received from clicking.
//			spPointerPoint = pArgs.GetCurrentPoint(null);

//			// IFCPTR_RETURN(spPointerPoint);

//			spPointerDevice = spPointerPoint.PointerDevice;

//			// IFCPTR_RETURN(spPointerDevice);

//			nPointerDeviceType = spPointerDevice.PointerDeviceType;
//			if (nPointerDeviceType == PointerDeviceType.Touch)
//			{
//				m_isPointerOverMain = false;
//			}

//			m_isPressedOnMain = false;

//			UpdateVisualState();
//		}

//		// Called when the IsEnabled property changes.
//		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
//		{
//			UpdateVisualState();
//		}

//		// Perform the primary action related to pointer up.
//		private void PerformPointerUpAction()
//		{
//			if (m_shouldPerformActions)
//			{
//				m_shouldPerformActions = false;
//				IsCalendarOpen = true;

//				ElementSoundPlayerService soundPlayerService = DXamlCore.GetCurrent().GetElementSoundPlayerServiceNoRef();
//				soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);
//			}
//		}


//		private void IsEventSourceTarget(
//			RoutedEventArgs pArgs,
//			out bool pIsEventSourceChildOfTarget)
//		{
//			object spOriginalSourceAsI;
//			DependencyObject spOriginalSourceAsDO;

//			spOriginalSourceAsI = pArgs.OriginalSource;

//			spOriginalSourceAsDO = spOriginalSourceAsI;

//			IsChildOfTarget(
//				spOriginalSourceAsDO,
//				true, /* doCacheResult */

//				pIsEventSourceChildOfTarget);
//		}

//		// Used in hit-testing for the CalendarDatePicker target area, which must exclude the header
//		private void IsChildOfTarget(
//			DependencyObject pChild,
//			bool doCacheResult,
//			out bool pIsChildOfTarget)
//		{
//			// Simple perf optimization: most pointer events have the same source as the previous
//			// event, so we'll cache the most recent result and reuse it whenever possible.
//			DependencyObject pMostRecentSearchChildNoRef = null;

//			bool mostRecentResult = false;

//			pIsChildOfTarget = false;

//			if (pChild is {})
//			{
//				bool result = mostRecentResult;
//				DependencyObject spHeaderPresenterAsDO;
//				DependencyObject spCurrentDO = pChild;
//				DependencyObject spParentDO;
//				DependencyObject pThisAsDONoRef = (DependencyObject)this;
//				bool isFound = false;

//				spHeaderPresenterAsDO = m_tpHeaderContentPresenter as DependencyObject;

//				while (spCurrentDO is {} && !isFound)
//				{
//					if (spCurrentDO == pMostRecentSearchChildNoRef)
//					{
//						// use the cached result
//						isFound = true;
//					}
//					else if (spCurrentDO == pThisAsDONoRef)
//					{
//						// meet the CalendarDatePicker itself, break;
//						result = true;
//						isFound = true;
//					}
//					else if (spHeaderPresenterAsDO is {} && spCurrentDO == spHeaderPresenterAsDO)
//					{
//						// meet the Header, break;
//						result = false;
//						isFound = true;
//					}
//					else
//					{
//						spParentDO = VisualTreeHelper.GetParent(spCurrentDO);

//						// refcounting note: Attach releases the previously stored ptr, and does not
//						// addthe new one.
//						spCurrentDO = spParentDO;
//					}
//				}

//				if (!isFound)
//				{
//					result = false;
//				}

//				if (doCacheResult is {})
//				{
//					pMostRecentSearchChildNoRef = pChild;
//					mostRecentResult = result;
//				}

//				pIsChildOfTarget = result;
//			}
//		}

//		private void SyncDate()
//		{
//			if (m_tpCalendarView is {})
//			{
//				bool isCalendarOpen = false;

//				isCalendarOpen = IsCalendarOpen;
//				if (isCalendarOpen)
//				{
//					DateTime spDateReference;
//					IList<DateTime> spSelectedDates;

//					m_isSelectedDatesChangingInternally = true;
//					var selectedDatesChangingGuard = wil.scope_exit([&] {
//						m_isSelectedDatesChangingInternally = false;
//					});

//					spDateReference = Date;

//					spSelectedDates = m_tpCalendarView.SelectedDates;
//					spSelectedDates.Clear();

//					if (spDateReference.HasValue)
//					{
//						DateTime date;

//						date = spDateReference.Value;
//						// if Date property is being set, we should always display the Date when Calendar is open.
//						SetDisplayDate(date);
//						spSelectedDates.Append(date);
//					}
//				}
//			}
//		}

//		private void OnCreateAutomationPeer(out AutomationPeer ppAutomationPeer)
//		{
//			// IFCPTR_RETURN(ppAutomationPeer);
//			pAutomationPeer = null;

//			CalendarDatePickerAutomationPeer spAutomationPeer;

//			ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.CalendarDatePickerAutomationPeer, GetHandle(), spAutomationPeer.GetAddressOf()));

//			spAutomationPeer.Owner = this;
//			ppAutomationPeer = spAutomationPeer.Detach();
//		}

//		private void GetCurrentFormattedDate(out string value)
//		{
//			value = null;

//			if (m_tpDateText)
//			{
//				value = m_tpDateText.Text;
//			}
//		}
//	}
//}
