using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Globalization;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

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

			_asyncOperationManager = new FlyoutAsyncOperationManager<DateTime>(this, () => default);

			// As UWP, initial value is the current date
			Date = DateTime.Now.Date;
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

			oldDateTime = Date.Date;
			newDateTime = ((DatePickerFlyoutPresenter)_tpPresenter).GetDate();
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

			DatePicked?.Invoke(this, new DatePickedEventArgs(newDateTime, oldDateTime) );

			Close();
		}

		//// -----
		//// IFlyoutBaseOverrides Impl
		//// -----
		protected override Control CreatePresenter()
		{
			DatePickerFlyoutPresenter spFlyoutPresenter;
			spFlyoutPresenter = new DatePickerFlyoutPresenter();
			_tpPresenter = spFlyoutPresenter;
			return _tpPresenter;
		}

		private protected override void ShowAtCore(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
		{
			_tpTarget = placementTarget;
			base.ShowAtCore(placementTarget, showOptions);
			//_asyncOperationManager.Start(placementTarget);
		} 

		public IAsyncOperation<DateTime> ShowAtAsync(FrameworkElement pTarget)
		{
			_tpTarget = pTarget;
			base.ShowAtCore(pTarget, null);
			return _asyncOperationManager.Start(pTarget);
		}

		private protected override void OnOpening()
		{
			//wrl.ComPtr<xaml_input.IInputManagerStatics> inputManagerStatics;
			//xaml_input.LastInputDeviceType lastInputDeviceType;

			if (_tpPresenter == null)
			{
				throw new InvalidOperationException("Expected non-null presenter");
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

		private protected override void OnOpened()
		{
			//wrl.ComPtr<UIElement> spFlyoutPresenterAsUIE;
			Control spFlyoutPresenterAsControl = _tpPresenter;
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

			if (_tpTarget != null)
			{
				Point point;
				FlyoutBase spFlyoutBase;

				point = DateTimePickerFlyoutHelper.CalculatePlacementPosition(_tpTarget, spFlyoutPresenterAsControl);
				//GetComposableBase().As(spFlyoutBase);
				//spFlyoutBase = GetComposableBase();
				spFlyoutBase = this;
				spFlyoutBase.PlaceFlyoutForDateTimePicker(point);
			}

			//Hook up OnAcceptClick and OnDismissClick event handlers:
			var spDismissButtonAsDO = _tpPresenter?.GetTemplateChild("DismissButton");
			if (spDismissButtonAsDO is ButtonBase spDismissButtonAsButtonBase)
			{
				//spDismissButtonAsDO.As(spDismissButtonAsUIE);
				//IGNOREHR(spDismissButtonAsDO.As(spDismissButtonAsButtonBase));
				_tpDismissButton = spDismissButtonAsButtonBase;
			}

			var spAcceptButtonAsDO = _tpPresenter?.GetTemplateChild("AcceptButton");
			if (spAcceptButtonAsDO is ButtonBase spAcceptButtonAsButtonBase)
			{
				//spAcceptButtonAsDO.As(spAcceptButtonAsUIE);
				//IGNOREHR(spAcceptButtonAsDO.As(spAcceptButtonAsButtonBase));
				_tpAcceptButton = spAcceptButtonAsButtonBase;
			}

			if (_tpAcceptButton != null)
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

			if (_tpDismissButton != null)
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

			_tpPresenter.KeyDown += OnKeyDown;
		}


		private protected override void OnClosed()
		{
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

			if (_tpPresenter != null)
			{
				_tpPresenter.KeyDown -= OnKeyDown;
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

		DateTime GetDefaultDate()
		{
			DateTime currentDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			currentDate = s_spCalendar.GetDateTime().Date;
			return currentDate;
		}

		DateTime GetDefaultMinYear()
		{
			DateTime minDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			s_spCalendar.AddYears(-_deltaYears);
			minDate = s_spCalendar.GetDateTime().Date;
			return minDate;
		}

		DateTime GetDefaultMaxYear()
		{

			DateTime maxDate = default;

			EnsureCalendar();
			s_spCalendar.SetToNow();
			s_spCalendar.AddYears(_deltaYears);
			maxDate = s_spCalendar.GetDateTime().Date;
			return maxDate;
		}

		string GetDefaultDayFormat()
		{
			return "day";
		}

		string GetDefaultMonthFormat()
		{
			return "{month.full}";
		}

		string GetDefaultYearFormat()
		{
			return "year.full";
		}

		void EnsureCalendar()
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
