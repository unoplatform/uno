// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePickerFlyout
{
	private const string UIA_DIALOG_ACCEPT = nameof(UIA_DIALOG_ACCEPT);
	private const string UIA_DIALOG_DISMISS = nameof(UIA_DIALOG_DISMISS);

	private const string s_strHourFormat = "{hour.integer(1)}";
	private const long s_timeSpanTicksPerMinute = TimeSpan.TicksPerMinute;
	private const long s_timeSpanTicksPerHour = TimeSpan.TicksPerHour;
	//private const long s_timeSpanTicksPerDay = TimeSpan.TicksPerDay;

	/// <summary>
	/// Represents a control that allows a user to pick a time value.
	/// </summary>
	public TimePickerFlyout()
	{
		_asyncOperationManager = new FlyoutAsyncOperationManager<TimeSpan?>(this, () => default); // TODO:MZ: Which params should be here?
		InitializeImpl();
	}

	//~TimePickerFlyout()
	//{
	//    ASYNCTRACE("TimePickerFlyout deructor called.");
	//}

	private void InitializeImpl()
	{
		FlyoutBase spFlyoutBase = this;

		// base.InitializeImpl();

		//(GetActivationFactory(
		//		 stringReference(RuntimeClass_Microsoft_UI_Xaml_Controls_Primitives_PickerFlyoutBase),
		//		 &spInnerFactory));
		//(spInnerFactory.CreateInstance(
		//		 (ITimePickerFlyout*)(this),
		//		 &spInnerInspectable,
		//		 &spInnerInstance));

		//(SetComposableBasePointers(
		//		 spInnerInspectable,
		//		 spInnerFactory));

		//(QueryInterface(
		//	__uuidof(FlyoutBase),
		//	&spFlyoutBase));

		spFlyoutBase.Opening += OnOpening;

		spFlyoutBase.Opened += OnOpened;

		spFlyoutBase.Closed += OnClosed;

		{
			spFlyoutBase.UsePickerFlyoutTheme = true;

			spFlyoutBase.Placement = FlyoutPlacementMode.Right;
		}
	}

	protected override bool ShouldShowConfirmationButtons() => false;

	protected override void OnConfirmed()
	{
		var oldTimeSpan = Time;
		var newTimeSpan = _tpPresenter.GetTime();

		Time = newTimeSpan;

		TimeSpan? spBoxedTsAsReference = newTimeSpan;
		_asyncOperationManager.Complete(spBoxedTsAsReference);

		var spArgs = new TimePickedEventArgs(oldTimeSpan, newTimeSpan);
		OnTimePicked(spArgs);

		base.OnConfirmed();
	}

	private protected void OnTimePicked(TimePickedEventArgs args) => TimePicked?.Invoke(this, args);

	protected override Control CreatePresenter()
	{
		_tpPresenter = new TimePickerFlyoutPresenter();
		if (TimePickerFlyoutPresenterStyle is not null)
		{
			_tpPresenter.Style = TimePickerFlyoutPresenterStyle;
		}
		// TODO: Uno specific: This is a workaround to avoid the popup to be shown at the wrong position briefly #15031
		_tpPresenter.Opacity = 0.0;

		return _tpPresenter;
	}

	public IAsyncOperation<TimeSpan?> ShowAtAsync(FrameworkElement target)
	{
		_tpTarget = target;
		return _asyncOperationManager.Start(_tpTarget);
	}

	private void OnOpening(object pSender, object pArgs)
	{
#if __IOS__ || __ANDROID__ // Avoid OnOpening initialization for native flyouts.
		if (this is NativeTimePickerFlyout)
		{
			return;
		}
#endif

		MUX_ASSERT(_tpPresenter is not null, "Expected non-null presenter");

		var lastInputDeviceType = XamlRoot.VisualTree.ContentRoot.InputManager.LastInputDeviceType;

		_tpPresenter.PullPropertiesFromOwner(this);
		_tpPresenter.SetAcceptDismissButtonsVisibility(lastInputDeviceType != InputDeviceType.GamepadOrRemote);

		if (_tpTarget is not null)
		{
			//The width of the flyout should equal that of the target element.
			double actualWidth = _tpTarget.ActualWidth;
			_tpPresenter.Width = actualWidth;
			//Also set MinWidth as FlyoutBase can change Width.
			_tpPresenter.MinWidth = actualWidth;
		}
	}

	private void OnOpened(object pSender, object pArgs)
	{
		string strAutomationName;

		if (_tpTarget is not null)
		{
			var point = DateTimePickerFlyoutHelper.CalculatePlacementPosition(_tpTarget, _tpPresenter);
			PlaceFlyoutForDateTimePicker(point);
			// TODO: Uno specific: This is a workaround to avoid the popup to be shown at the wrong position briefly #15031
			_tpPresenter.Opacity = 1.0;
		}

		//Hook up OnAcceptClick and OnDismissClick event handlers:
		_tpDismissButton = _tpPresenter.GetTemplateChild("DismissButton") as ButtonBase;

		_tpAcceptButton = _tpPresenter.GetTemplateChild("AcceptButton") as ButtonBase;

		if (_tpAcceptButton is not null)
		{
			_tpAcceptButton.Click += OnAcceptClick;
			_acceptClickToken.Disposable = Disposable.Create(() => _tpAcceptButton.Click -= OnAcceptClick);

			strAutomationName = ResourceAccessor.GetLocalizedStringResource(UIA_DIALOG_ACCEPT);
			AutomationProperties.SetName(_tpAcceptButton, strAutomationName);
		}

		if (_tpDismissButton is not null)
		{
			_tpDismissButton.Click += OnDismissClick;
			_dismissClickToken.Disposable = Disposable.Create(() => _tpDismissButton.Click -= OnDismissClick);

			strAutomationName = ResourceAccessor.GetLocalizedStringResource(UIA_DIALOG_DISMISS);
			AutomationProperties.SetName(_tpDismissButton, strAutomationName);
		}

		_tpPresenter.KeyDown += OnKeyDown;
		_keyDownToken.Disposable = Disposable.Create(() => _tpPresenter.KeyDown -= OnKeyDown);
	}

	private void OnClosed(object pSender, object pArgs)
	{
		if (_tpAcceptButton is not null)
		{
			_acceptClickToken.Disposable = null;
			_tpAcceptButton = null;
		}

		if (_tpDismissButton is not null)
		{
			_dismissClickToken.Disposable = null;
			_tpDismissButton = null;
		}

		if (_tpPresenter is not null)
		{
			_keyDownToken.Disposable = null;
		}
	}

	private void OnAcceptClick(object pSender, RoutedEventArgs pArgs)
	{
		OnConfirmed();
	}

	private void OnDismissClick(object pSender, RoutedEventArgs pArgs)
	{
		Hide();
	}

	private void OnKeyDown(object pSender, KeyRoutedEventArgs pEventArgs)
	{

		bool bHandled = false;
		bool bShouldConfirm = false;
		VirtualKey key = VirtualKey.None;
		VirtualKeyModifiers nModifierKeys;

		bHandled = pEventArgs.Handled;
		if (bHandled)
		{
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

	internal static TimeSpan GetDefaultTime()
	{
		TimeSpan retTimeSpan = default;

		var spCalendar = new Calendar();
		var spLanguages = spCalendar.Languages;

		var strClock = spCalendar.GetClock();

		spCalendar = new Calendar(
			spLanguages,
			"GregorianCalendar",
			strClock);

		spCalendar.SetToNow();
		var minute = spCalendar.Minute;
		var hour = spCalendar.Hour;

		bool isTwelveHourClock = strClock == TimePickerFlyoutPresenter._strTwelveHourClock;
		if (isTwelveHourClock)
		{
			var period = spCalendar.Period;
			var firstPeriodInThisDay = spCalendar.FirstPeriodInThisDay;

			if (period == firstPeriodInThisDay)
			{
				if (hour == TimePickerFlyoutPresenter._periodCoercionOffset)
				{
					hour = 0;
				}
			}
			else
			{
				if (hour != TimePickerFlyoutPresenter._periodCoercionOffset)
				{
					hour += TimePickerFlyoutPresenter._periodCoercionOffset;
				}
			}
		}

		retTimeSpan += new TimeSpan(minute * s_timeSpanTicksPerMinute);
		retTimeSpan += new TimeSpan(hour * s_timeSpanTicksPerHour);

		return retTimeSpan;
	}

	internal static string GetDefaultClockIdentifier()
	{
		var spFormatter = new DateTimeFormatter(s_strHourFormat);

		return spFormatter.Clock;
	}

	internal static int GetDefaultMinuteIncrement() => 1;
}
