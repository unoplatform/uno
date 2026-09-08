// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotification.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotification
{
	// Uno: a CLR monitor replaces the native shared/exclusive lock.
	private readonly object _gate = new();
	private readonly string _payload;
	private string _tag = string.Empty;
	private string _group = string.Empty;
	private uint _id;
	private AppNotificationProgressData? _progress;
	private DateTimeOffset _expiration = DateTimeOffset.FromFileTime(0).ToLocalTime();
	private bool _expiresOnReboot;
	private AppNotificationPriority _priority;
	private bool _suppressDisplay;

	// TODO Uno: ConferencingConfig belongs to the later calling-preview contract and is not projected here.
}
