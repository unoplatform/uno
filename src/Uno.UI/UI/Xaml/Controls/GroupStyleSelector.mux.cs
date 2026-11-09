// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference GroupStyleSelector_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// GroupStyleSelector allows the app writer to provide custom group style selection logic.
public partial class GroupStyleSelector
{
	public GroupStyleSelector()
	{
	}

	public GroupStyle? SelectGroupStyle(object? group, uint level) => SelectGroupStyleCore(group, level);

	// Override this method to return an app specific GroupStyle.
	// Returns an app-specific style to apply, or null.
	protected virtual GroupStyle? SelectGroupStyleCore(object? group, uint level) => null;
}
