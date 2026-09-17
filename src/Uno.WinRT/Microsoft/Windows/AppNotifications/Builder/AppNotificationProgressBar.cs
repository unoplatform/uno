// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationBuilder.idl, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

/// <summary>
/// Represents a progress bar in an app notification.
/// </summary>
[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public sealed partial class AppNotificationProgressBar : IStringable
{
}
