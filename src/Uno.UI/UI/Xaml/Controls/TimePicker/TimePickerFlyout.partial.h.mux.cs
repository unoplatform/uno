#nullable enable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Uno.Disposables;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

partial class TimePickerFlyout
{
	private TimePickerFlyoutPresenter? _tpPresenter;
	private FrameworkElement _tpTarget;
	private ButtonBase? _tpAcceptButton;
	private ButtonBase? _tpDismissButton;

	private FlyoutAsyncOperationManager<TimeSpan?>? _asyncOperationManager;
	private readonly SerialDisposable _acceptClickToken = new();
	private readonly SerialDisposable _dismissClickToken = new();
	private readonly SerialDisposable _keyDownToken = new();
}
