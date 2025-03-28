// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	// UNO ONLY
	// Note: This class is public as sub-class CalendarViewDayItem is public
	//		 All members of this class MUST be internal (or private protected).

	public partial class CalendarViewBaseItem : Control
	{
		internal CalendarViewBaseItem() // Make sure the ctor is not publically visible
		{
			m_pParentCalendarView = null;
#if DEBUG
			m_eraForDebug = 0;
			m_yearForDebug = 0;
			m_monthForDebug = 0;
			m_dayForDebug = 0;
#endif
			// Uno only
			Initialize_CalendarViewBaseItemChrome();
#if !__NETSTD_REFERENCE__
			this.Loaded += (_, _) =>
			{
#if !UNO_HAS_BORDER_VISUAL
				_borderRenderer ??= new(this);
#endif
#if !UNO_HAS_ENHANCED_LIFECYCLE
				EnterImpl();
#endif
			};
#endif
		}



		//protected:

		//// this base panel implementation is hidden from IDL
		//private void QueryInterfaceImpl( REFIID iid, out  void* ppObject)
		//{
		//    if (InlineIsEqualGUID(iid, __uuidof(ICalendarViewBaseItem)))
		//    {
		//        ppObject = (ICalendarViewBaseItem)(this);
		//    }
		//    else
		//    {
		//        return CalendarViewBaseItemGenerated.QueryInterfaceImpl(iid, ppObject);
		//    }

		//    AddRefOuter();
		//    return;
		//}

		//// Called when the user presses a pointer down over the
		//// CalendarViewBaseItem.
		//IFACEMETHOD(OnPointerPressed)(
		//     xaml_input.IPointerRoutedEventArgs pArgs)
		//    override;

		//// Called when the user releases a pointer over the
		//// CalendarViewBaseItem.
		//IFACEMETHOD(OnPointerReleased)(
		//     xaml_input.IPointerRoutedEventArgs pArgs)
		//    override;

		//// Called when a pointer enters a CalendarViewBaseItem.
		//IFACEMETHOD(OnPointerEntered)(
		//     xaml_input.IPointerRoutedEventArgs pArgs)
		//    override;

		//// Called when a pointer leaves a CalendarViewBaseItem.
		//IFACEMETHOD(OnPointerExited)(
		//     xaml_input.IPointerRoutedEventArgs pArgs)
		//    override;

		//// Called when the CalendarViewBaseItem or its children lose pointer capture.
		//IFACEMETHOD(OnPointerCaptureLost)(
		//     xaml_input.IPointerRoutedEventArgs pArgs)
		//    override;

		//// Called when the CalendarViewBaseItem receives focus.
		//IFACEMETHOD(OnGotFocus)(
		//     xaml.IRoutedEventArgs pArgs)
		//    override;

		//// Called when the CalendarViewBaseItem loses focus.
		//IFACEMETHOD(OnLostFocus)(
		//     xaml.IRoutedEventArgs pArgs)
		//    override;

		//IFACEMETHOD(OnRightTapped)( xaml_input.IRightTappedRoutedEventArgs pArgs) override;

		//// Called when the IsEnabled property changes.
		//private void OnIsEnabledChanged( DirectUI.IsEnabledChangedEventArgs pArgs) override;

		//// Called when the element enters the tree. Refreshes visual state.
		//private void EnterImpl(
		//     XBOOL bLive,
		//     XBOOL bSkipNameRegistration,
		//     XBOOL bCoercedIsEnabled,
		//     XBOOL bUseLayoutRounding) override sealed;

		//public:

		//void SetParentCalendarView( CalendarView pCalendarView);

		//CalendarView GetParentCalendarView();

		//private void SetIsToday( bool state);
		//private void SetIsKeyboardFocused( bool state);
		//private void SetIsSelected( bool state);
		//private void SetIsBlackout( bool state);
		//private void SetIsHovered( bool state);
		//private void SetIsPressed( bool state);
		//private void SetIsOutOfScope( bool state);

		//private void UpdateText( string mainText,  string labelText,  bool showLabel);
		//private void UpdateMainText( string mainText);
		//private void UpdateLabelText( string labelText);
		//private void ShowLabelText( bool showLabel);
		//private void GetMainText(out HSTRING pMainText);

		// CalendarViewItem and CalendarViewDayItem will override this method.
		internal virtual DateTime DateBase
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException(); // For debug purpose only
		}

		//private void FocusSelfOrChild(
		//     xaml.FocusState focusState,
		//    out bool pFocused,
		//     xaml_input.FocusNavigationDirection focusNavigationDirection = xaml_input.FocusNavigationDirection.FocusNavigationDirection_None);

		//// invalidate render to make sure chrome properties (background, border, ...) get updated
		//private void InvalidateRender();

		//private void UpdateTextBlockForeground();

		//private void UpdateTextBlockFontProperties();

		//private void UpdateTextBlockAlignments();

#if DEBUG && false
		// DateTime has an int64 member which is not intutive enough. This method will convert it
		// into numbers that we can easily read.
		private void SetDateForDebug(DateTime value);
#endif

		//protected:
		//    private void ChangeVisualState(bool useTransitions = true) override;

		//private:
		//    private void UpdateVisualStateInternal();


		//private:
		private CalendarView m_pParentCalendarView;
#if DEBUG
		private int m_eraForDebug;
		private int m_yearForDebug;
		private int m_monthForDebug;
		private int m_dayForDebug;
#endif
	}
}
