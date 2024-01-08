#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Uno.Disposables;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

partial class TimePickerFlyoutPresenter
{
	// The selection of the selectors in our template can be changed by two sources. First source is
	// the end user changing a field to select the desired time. Second source is us updating
	// the itemssources and selected indices. We only want to react to the first source as the
	// second one will cause an unintentional recurrence in our logic. So we use this locking mechanism to
	// anticipate selection changes caused by us and making sure we do not react to them. It is okay
	// that these locks are not atomic since they will be only accessed by a single thread so no race
	// condition can occur.
	void AllowReactionToSelectionChange()
	{
		_reactionToSelectionChangeAllowed = true;
	}

	void PreventReactionToSelectionChange()
	{
		_reactionToSelectionChangeAllowed = false;
	}

	bool IsReactionToSelectionChangeAllowed()
	{
		return _reactionToSelectionChangeAllowed;
	}

	// Specifies if we are in 12 hour clock mode currently.
	private bool _is12HourClock;

	private bool _reactionToSelectionChangeAllowed;

	//// Reference to a Button for invoking the TimePickerFlyout in the form factor APISet
	//private ButtonBase? _tpFlyoutButton;

	private IList<object>? _tpHourSource;
	private IList<object>? _tpMinuteSource;
	private IList<object>? _tpPeriodSource;

	// Reference the picker selectors by order of appearance.
	// Used for arrow navigation, so stored as IControls for easy access
	// to the Focus() method.
	private Control? _tpFirstPickerAsControl;
	private Control? _tpSecondPickerAsControl;
	private Control? _tpThirdPickerAsControl;

	// References to the hosting borders.
	private Border? _tpFirstPickerHost;
	private Border? _tpSecondPickerHost;
	private Border? _tpThirdPickerHost;

	// References to the columns of the Grid that will hold the day/month/year LoopingSelectors and the spacers.
	private ColumnDefinition? _tpFirstPickerHostColumn;
	private ColumnDefinition? _tpSecondPickerHostColumn;
	private ColumnDefinition? _tpThirdPickerHostColumn;

	// References to elements which will act as the dividers between the LoopingSelectors.
	private UIElement? _tpFirstPickerSpacing;
	private UIElement? _tpSecondPickerSpacing;

	// Reference to the title presenter
	private TextBlock? _tpTitlePresenter;

	// Reference to background border
	private Border? _tpBackgroundBorder;

	// Reference to our content panel. We will be setting the flowdirection property on our root to achieve
	// RTL where necessary.
	private FrameworkElement? _tpContentPanel;

	// References to the elements for the accept and dismiss buttons.
	private UIElement? _tpAcceptDismissHostGrid;
	private UIElement? _tpAcceptButton;
	private UIElement? _tpDismissButton;

	private bool _acceptDismissButtonsVisible;

	// Reference to HourPicker Selector. We need this as we will change its item
	// source as our properties change.
	private LoopingSelector? _tpHourPicker;

	// Reference to MinutePicler Selector. We need this as we will change its item
	// source as our properties change.
	private LoopingSelector? _tpMinutePicker;

	// Reference to PeriodPicker Selector. We need this as we will change its item
	// source as our properties change.
	private LoopingSelector? _tpPeriodPicker;

	// This calendar will be used over and over while we are generating the ItemsSources instead
	// of creating new calendars.
	private Calendar? _tpCalendar;

	// This DateTimeFormatter will be used over and over when updating the
	// FlyoutButton content property
	private DateTimeFormatter? _tpTimeFormatter;
	private string? _strTimeFormatterClockIdentifier;

	// Properties pulled from owner TimePickerFlyout
	private string _clockIdentifier = "";
	private string? _title;
	private int _minuteIncrement;
	private TimeSpan _time;

	private readonly SerialDisposable _hourSelectionChangedToken = new();
	private readonly SerialDisposable _minuteSelectionChangedToken = new();
	private readonly SerialDisposable _periodSelectionChangedToken = new();
}
