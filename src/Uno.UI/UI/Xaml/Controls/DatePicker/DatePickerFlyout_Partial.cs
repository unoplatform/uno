using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Globalization;
using Windows.System;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{

	public partial class DatePickerFlyout : PickerFlyoutBase
	{
		public DatePickerFlyout()
		{
			InitializeImpl();
		}

		private void InitializeImpl()
		{
			base.Placement = FlyoutPlacementMode.Right;
			base.UsePickerFlyoutTheme = true;

			Opening += OnOpening;
			Opened += OnOpened;

			_asyncOperationManager = new FlyoutAsyncOperationManager<DateTimeOffset?>(this, () => default);
		}

		protected override bool ShouldShowConfirmationButtons()
		{
			bool result;
			result = false;
			return result;
		}

		protected override void OnConfirmed()
		{

			DateTime oldDateTime = default;
			DateTime newDateTime = default;
			//DatePickedEventArgs spArgs;
			//DependencyObject spBoxedDateTime;
			//DateTime spBoxedDtAsReference;

			oldDateTime = Date;
			newDateTime = _tpPresenter.GetDate();
			Date = newDateTime;
			//Private.ValueBoxer.CreateDateTime(newDateTime, &spBoxedDateTime);
			//spBoxedDateTime.As(spBoxedDtAsReference);
			_asyncOperationManager.Complete(newDateTime);
			//wrl.MakeAndInitialize<xaml_controls.DatePickedEventArgs>(spArgs);
			//spArgs.OldDate = oldDateTime;
			//spArgs.NewDate = newDateTime;
			//m_DatePickedEventSource.InvokeAll(this, spArgs);
			//DatePickerFlyoutGenerated.OnConfirmedImpl();
			// Cleanup
			// return hr;

			_datePicked?.Invoke(this, new DatePickedEventArgs(newDateTime, oldDateTime));

			Close();
		}

		//// -----
		//// IFlyoutBaseOverrides Impl
		//// -----
		protected override Control CreatePresenter()
		{
			DatePickerFlyoutPresenter spFlyoutPresenter;
			spFlyoutPresenter = new DatePickerFlyoutPresenter();
			if (DatePickerFlyoutPresenterStyle is not null)
			{
				spFlyoutPresenter.Style = DatePickerFlyoutPresenterStyle;
			}
			_tpPresenter = spFlyoutPresenter;

			// TODO: Uno specific: This is a workaround to avoid the popup to be shown at the wrong position briefly #15031
			if (_tpPresenter is FrameworkElement presenter && _tpTarget is not null)
			{
				presenter.Opacity = 0.0;
			}

			return _tpPresenter as Control;
		}

		public IAsyncOperation<DateTimeOffset?> ShowAtAsync(FrameworkElement target)
		{
			_tpTarget = target;
			return _asyncOperationManager.Start(target);
		}

		private void OnOpening(object sender, object args)
		{
			//wrl.ComPtr<xaml_input.IInputManagerStatics> inputManagerStatics;
			//xaml_input.LastInputDeviceType lastInputDeviceType;

			if (_tpPresenter == null)
			{
				return;
				//throw new InvalidOperationException("Expected non-null presenter");
			}

			//(wf.GetActivationFactory(
			//    wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Input_InputManager),
			//    &inputManagerStatics));

			//inputManagerStatics.GetLastInputDeviceType(lastInputDeviceType);

			_tpPresenter.PullPropertiesFromOwner(this);
			//_tpPresenter.SetAcceptDismissButtonsVisibility(lastInputDeviceType != LastInputDeviceType.GamepadOrRemote);
			_tpPresenter.SetAcceptDismissButtonsVisibility(true);

			if (_tpTarget is FrameworkElement spTargetAsFE &&
				_tpPresenter is FrameworkElement spPresenterAsFE)
			{
				//wrl.ComPtr<xaml.FrameworkElement> spPresenterAsFE;
				//wrl.ComPtr<xaml.FrameworkElement> spTargetAsFE;
				double actualWidth;

				//_tpPresenter.As(spPresenterAsFE);
				//_tpTarget.As(spTargetAsFE);

				//The width of the flyout should equal that of the target element.
				actualWidth = spTargetAsFE.ActualWidth;
				//spPresenterAsFE.Width = actualWidth;
				//Also set MinWidth as FlyoutBase can change Width.
				spPresenterAsFE.MinWidth = actualWidth;
			}
		}

		private void OnOpened(object sender, object args)
		{
			//wrl.ComPtr<UIElement> spFlyoutPresenterAsUIE;
			Control spFlyoutPresenterAsControl = _tpPresenter as Control;
			//wrl.ComPtr<xaml_controls.IControlProtected> spFlyoutPresenterAsControlProtected;
			//wrl.ComPtr<xaml.IDependencyObject> spDismissButtonAsDO;
			//wrl.ComPtr<UIElement> spDismissButtonAsUIE;
			//wrl.ComPtr<ButtonBase> spDismissButtonAsButtonBase;
			//wrl.ComPtr<xaml.IDependencyObject> spAcceptButtonAsDO;
			//wrl.ComPtr<UIElement> spAcceptButtonAsUIE;
			//wrl.ComPtr<ButtonBase> spAcceptButtonAsButtonBase;
			//wrl.ComPtr<xaml_automation.IAutomationPropertiesStatics> spAutomationPropertiesStatics;
			//wrl.ComPtr<IDependencyObject> spButtonAsDO;
			//string strAutomationName;

			//wf.GetActivationFactory(wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_AutomationProperties), &spAutomationPropertiesStatics);
			//_tpPresenter.As(spFlyoutPresenterAsUIE);
			//_tpPresenter.As(spFlyoutPresenterAsControl);
			//_tpPresenter.As(spFlyoutPresenterAsControlProtected);

			if (_tpTarget is { })
			{
				Point point;
				FlyoutBase spFlyoutBase;

				point = DateTimePickerFlyoutHelper.CalculatePlacementPosition(_tpTarget, spFlyoutPresenterAsControl);
				//GetComposableBase().As(spFlyoutBase);
				//spFlyoutBase = GetComposableBase();
				spFlyoutBase = this;
				spFlyoutBase.PlaceFlyoutForDateTimePicker(point);

				// TODO: Uno specific: This is a workaround to avoid the popup to be shown at the wrong position briefly #15031
				if (_tpPresenter is FrameworkElement presenter)
				{
					presenter.Opacity = 1.0;
				}
			}

			//Hook up OnAcceptClick and OnDismissClick event handlers:
			var spDismissButtonAsDO = spFlyoutPresenterAsControl?.GetTemplateChild("DismissButton");
			if (spDismissButtonAsDO is ButtonBase spDismissButtonAsButtonBase)
			{
				//spDismissButtonAsDO.As(spDismissButtonAsUIE);
				//IGNOREHR(spDismissButtonAsDO.As(spDismissButtonAsButtonBase));
				_tpDismissButton = spDismissButtonAsButtonBase;
			}

			var spAcceptButtonAsDO = spFlyoutPresenterAsControl?.GetTemplateChild("AcceptButton");
			if (spAcceptButtonAsDO is ButtonBase spAcceptButtonAsButtonBase)
			{
				//spAcceptButtonAsDO.As(spAcceptButtonAsUIE);
				//IGNOREHR(spAcceptButtonAsDO.As(spAcceptButtonAsButtonBase));
				_tpAcceptButton = spAcceptButtonAsButtonBase;
			}

			if (_tpAcceptButton is { })
			{
				//global.System.Diagnostics.Debug.Assert(spAcceptButtonAsUIE);
				//(_tpAcceptButton.add_Click(
				//	wrl.Callback<xaml.IRoutedEventHandler>(this, &global::DatePickerFlyout.OnAcceptClick),
				//	&_acceptClickToken));
				_tpAcceptButton.Click += OnAcceptClick;

				// ----AUTOMATION PEER STUFF----
				//Private.FindStringResource(UIA_DIALOG_ACCEPT, strAutomationName);
				//_tpAcceptButton.As(spButtonAsDO);
				//spAutomationPropertiesStatics.SetName(spButtonAsDO, strAutomationName);
			}

			if (_tpDismissButton is { })
			{
				//global.System.Diagnostics.Debug.Assert(spDismissButtonAsUIE);
				//(_tpDismissButton.add_Click(
				//	wrl.Callback<xaml.IRoutedEventHandler>(this, &global::DatePickerFlyout.OnDismissClick),
				//	&_dismissClickToken));
				_tpDismissButton.Click += OnDismissClick;

				// ----AUTOMATION PEER STUFF----
				//Private.FindStringResource(UIA_DIALOG_DISMISS, strAutomationName);
				//_tpDismissButton.As(spButtonAsDO);
				//spAutomationPropertiesStatics.SetName(spButtonAsDO, strAutomationName);
			}

			Debug.Assert(_tpPresenter != null);

			//(spFlyoutPresenterAsUIE.add_KeyDown(
			//	wrl.Callback<xaml_input.IKeyEventHandler>(this, &global::DatePickerFlyout.OnKeyDown),
			//	&_keyDownToken));

			spFlyoutPresenterAsControl.KeyDown += OnKeyDown;
		}


		private protected override void OnClosed()
		{
			base.OnClosed();

			if (_tpAcceptButton != null)
			{
				_tpAcceptButton.Click -= OnAcceptClick;
				_tpAcceptButton = null;
			}

			if (_tpDismissButton != null)
			{
				_tpDismissButton.Click -= OnDismissClick;
				_tpDismissButton = null;
			}

			if (_tpPresenter is Control ctl)
			{
				ctl.KeyDown -= OnKeyDown;
			}
		}

		void OnAcceptClick(object sender, RoutedEventArgs pArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pArgs);

			OnConfirmed();
		}

		void OnDismissClick(object sender, RoutedEventArgs pArgs)
		{
			//UNREFERENCED_PARAMETER(pSender);
			//UNREFERENCED_PARAMETER(pArgs);
			FlyoutBase spFlyoutBase;

			//GetComposableBase().As(spFlyoutBase);
			//spFlyoutBase = GetComposableBase();
			spFlyoutBase = this;
			//spFlyoutBase.Hide();
			spFlyoutBase.Hide();
		}

		void OnKeyDown(object sender, KeyRoutedEventArgs pEventArgs)
		{
			bool bHandled = false;
			bool bShouldConfirm = false;
			VirtualKey key = VirtualKey.None;
			VirtualKeyModifiers nModifierKeys;

			//UNREFERENCED_PARAMETER(pSender);
			if (pEventArgs == null) throw new ArgumentNullException(nameof(sender));

			bHandled = pEventArgs.Handled;
			if (bHandled)
			{
				//goto Cleanup;
				return;
			}

			key = pEventArgs.Key;
			if ((key == VirtualKey.Up || key == VirtualKey.Down))
			{
				nModifierKeys = PlatformHelpers.GetKeyboardModifiers();
				if (nModifierKeys.HasFlag(VirtualKeyModifiers.Menu))
				{
					bShouldConfirm = true;
				}
			}
			else if (key == VirtualKey.Space || key == VirtualKey.Enter)
			{
				bShouldConfirm = true;
			}

			if (bShouldConfirm)
			{
				pEventArgs.Handled = true;
				OnConfirmed();
			}
		}

		static string GetDefaultCalendarIdentifier()
		{
			return "GregorianCalendar";
		}

		static DateTimeOffset GetDefaultDate()
		{
			DateTime currentDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			currentDate = s_spCalendar.GetDateTime();
			return currentDate;
		}

		static DateTimeOffset GetDefaultMinYear()
		{
			DateTime minDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			s_spCalendar.AddYears(-_deltaYears);
			minDate = s_spCalendar.GetDateTime();
			return minDate;
		}

		static DateTimeOffset GetDefaultMaxYear()
		{

			DateTime maxDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			s_spCalendar.AddYears(_deltaYears);
			maxDate = s_spCalendar.GetDateTime();
			return maxDate;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetDefaultDayFormat()
		{
			return "day";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetDefaultMonthFormat()
		{
			return "{month.full}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static string GetDefaultYearFormat()
		{
			return "year.full";
		}

		static void EnsureCalendar()
		{
			if (s_spCalendar == null)
			{
				//CalendarFactory spCalendarFactory;
				Calendar spCalendar;
				IReadOnlyList<string> spLanguages;
				//wrl.ComPtr<wfc.IIterable<string>> spLanguagesAsIterable;
				string strClock;

				//(wf.ActivateInstance(
				//	wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_Calendar),
				//	&spCalendar));
				spCalendar = new Calendar();

				spLanguages = spCalendar.Languages;
				//spLanguages.As(spLanguagesAsIterable);
				strClock = spCalendar.GetClock();
				//(wf.GetActivationFactory(
				//	wrl_wrappers.Hstring(RuntimeClass_Windows_Globalization_Calendar),
				//	&spCalendarFactory));

				//(spCalendarFactory.CreateCalendar(
				//	spLanguagesAsIterable,
				//	wrl_wrappers.Hstring("GregorianCalendar"),
				//	strClock,
				//	spCalendar));
				spCalendar = new Calendar(spLanguages, "GregorianCalendar", strClock);

				//spCalendar.CopyTo(s_spCalendar);
				s_spCalendar = spCalendar;
			}
		}
	}
}
