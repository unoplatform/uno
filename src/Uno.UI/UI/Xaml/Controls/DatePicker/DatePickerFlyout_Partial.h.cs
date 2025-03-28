using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	//extern __declspec(selectany)  char DatePickerFlyoutShowAtAsyncOperationName[] = "Windows.Foundation.IAsyncOperation`1<Windows.Foundation.IReference`1<Windows.Foundation.DateTime>> Microsoft/* UWP don't rename */.UI.Xaml.Controls.DatePickerFlyout.ShowAtAsync";

	partial class DatePickerFlyout //: DatePickerFlyoutGenerated
	{

		// public

		// void OnPropertyChanged(
		//     xaml.IDependencyPropertyChangedEventArgs pArgs);

		//static  void GetDefaultCalendarIdentifier(out  DependencyObject ppDefaultCalendarIdentifierValue);
		//static  void GetDefaultDate(out  DependencyObject ppDefaultDateValue);
		//static  void GetDefaultDayVisible(out  DependencyObject ppDayVisibleValue);
		//static  void GetDefaultMonthVisible(out  DependencyObject ppMonthVisibleValue);
		//static  void GetDefaultYearVisible(out  DependencyObject ppYearVisibleValue);
		//static  void GetDefaultMinYear(out  DependencyObject ppDefaultMinYearValue);
		//static  void GetDefaultMaxYear(out  DependencyObject ppDefaultMaxYearValue);
		//static  void GetDefaultDayFormat(out  DependencyObject ppDefaultDayFormat);
		//static  void GetDefaultMonthFormat(out  DependencyObject ppDefaultMonthFormat);
		//static  void GetDefaultYearFormat(out  DependencyObject ppDefaultYearFormat);


		//// private
		//    ~DatePickerFlyout();

		//     void InitializeImpl() override;

		//    static  void EnsureCalendar();

		//     void OnOpening( DependencyObject pSender,  DependencyObject pArgs);

		//     void OnOpened( DependencyObject pSender,  DependencyObject pArgs);

		//     void OnClosed( DependencyObject pSender,  DependencyObject pArgs);

		//     void OnAcceptClick( DependencyObject pSender,  xaml.IRoutedEventArgs pArgs);

		//     void OnDismissClick( DependencyObject pSender,  xaml.IRoutedEventArgs pArgs);

		//     void OnKeyDown( DependencyObject pSender,  xaml_input.IKeyRoutedEventArgs pEventArgs);

		//// public
		//     void ShowAtAsyncImpl(
		//         xaml.FrameworkElement pTarget,
		//        out  wf.IAsyncOperation<DateTime>* ppAction);

		//// private
		//     void OnConfirmedImpl() override;

		//     void ShouldShowConfirmationButtonsImpl(
		//        out bool returnValue) override;

		//     void CreatePresenterImpl(
		//        out  xaml_controls.IControl returnValue) override;

		// The number of years the default Max and Min year field will
		// be set from the current year.
		private const int _deltaYears = 50;
		static Calendar s_spCalendar;

		private protected IDatePickerFlyoutPresenter _tpPresenter;
		FrameworkElement _tpTarget;
		FlyoutAsyncOperationManager<DateTimeOffset?> _asyncOperationManager;

		ButtonBase _tpAcceptButton;
		ButtonBase _tpDismissButton;
	};
}
