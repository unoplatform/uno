// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationProgressData.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

namespace Microsoft.Windows.AppNotifications;

partial class AppNotificationProgressData
{
	// Uno: a CLR monitor replaces the native shared/exclusive lock.
	private readonly object _gate = new();
	private uint _sequenceNumber = 1;
	private string _title = string.Empty;
	private double _value;
	private string _valueStringOverride = string.Empty;
	private string _status = string.Empty;
}
