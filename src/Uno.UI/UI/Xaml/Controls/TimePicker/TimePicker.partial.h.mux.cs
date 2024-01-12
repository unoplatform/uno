#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      Represents TimePicker control. TimePicker is a XAML UI control that allows
//      the selection of times.

using System;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Globalization;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePicker
{
	// The selection of the selectors in our template can be changed by two sources. First source is
	// the end user changing a field to select the desired time. Second source is us updating
	// the itemssources and selected indices. We only want to react to the first source as the
	// second one will cause an unintentional recurrence in our logic. So we use this locking mechanism to
	// anticipate selection changes caused by us and making sure we do not react to them. It is okay
	// that these locks are not atomic since they will be only accessed by a single thread so no race
	// condition can occur.
	private void AllowReactionToSelectionChange()
	{
		m_reactionToSelectionChangeAllowed = true;
	}

	private void PreventReactionToSelectionChange()
	{
		m_reactionToSelectionChangeAllowed = false;
	}

	private bool IsReactionToSelectionChangeAllowed()
	{
		return m_reactionToSelectionChangeAllowed;
	}

	// Specifies if we are in 12 hour clock mode currently.
	private bool m_is12HourClock;

	private bool m_reactionToSelectionChangeAllowed;

	// Reference to a Button for invoking the TimePickerFlyout in the form factor APISet
	private ButtonBase? m_tpFlyoutButton;

	// References to the TextBlocks that are used to display the Hour/Minute/Period.
	private TextBlock? m_tpHourTextBlock;
	private TextBlock? m_tpMinuteTextBlock;
	private TextBlock? m_tpPeriodTextBlock;

	private TrackerCollection<object>? m_tpHourSource;
	private TrackerCollection<object>? m_tpMinuteSource;
	private TrackerCollection<object>? m_tpPeriodSource;

	// References to the hosting borders.
	private Border? m_tpFirstPickerHost;
	private Border? m_tpSecondPickerHost;
	private Border? m_tpThirdPickerHost;

	// References to the columns that will hold the hour/minute/period textblocks.
	private ColumnDefinition? m_tpFirstTextBlockColumn;
	private ColumnDefinition? m_tpSecondTextBlockColumn;
	private ColumnDefinition? m_tpThirdTextBlockColumn;

	// References to the column dividers between the hour/minute/period textblocks.
	private UIElement? m_tpFirstColumnDivider;
	private UIElement? m_tpSecondColumnDivider;

	// Reference to the Header content presenter. We need this to collapse the visibility
	// when the Header and HeaderTemplate are null.
	private UIElement? m_tpHeaderPresenter;

	// Reference to our lay out root. We will be setting the flowdirection property on our root to achieve
	// RTL where necessary.
	private FrameworkElement? m_tpLayoutRoot;

	// Reference to HourPicker Selector. We need this as we will change its item
	// source as our properties change.
	private Selector? m_tpHourPicker;

	// Reference to MinutePicler Selector. We need this as we will change its item
	// source as our properties change.
	private Selector? m_tpMinutePicker;

	// Reference to PeriodPicker Selector. We need this as we will change its item
	// source as our properties change.
	private Selector? m_tpPeriodPicker;

	// This calendar will be used over and over while we are generating the ItemsSources instead
	// of creating new calendars.
	private Calendar? m_tpCalendar;

	private readonly SerialDisposable m_epHourSelectionChangedHandler = new();
	private readonly SerialDisposable m_epMinuteSelectionChangedHandler = new();
	private readonly SerialDisposable m_epPeriodSelectionChangedHandler = new();

	private readonly SerialDisposable m_epFlyoutButtonClickHandler = new();

	// Events and references used to respond to Window.Activated
	private readonly SerialDisposable m_windowActivatedHandler = new();
	// private readonly SerialDisposable m_loadedEventHandler = new();

	// The default Time value if no Time is set.
	private TimeSpan m_defaultTime;

	// The current time.
	private TimeSpan m_currentTime;

	// Keeps track of the pending async operation and allows
	// for cancellation.
	private IAsyncInfo? m_tpAsyncSelectionInfo;

	private bool m_isPropagatingTime;
}
