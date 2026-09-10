// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference controls/dev/ComboBox/ComboBoxHelper.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ComboBoxHelper
{
	// Uno: static DP initialization replaces native EnsureProperties/ClearProperties lifetime management.
	private static DependencyProperty DropDownEventRevokersProperty { get; } =
		DependencyProperty.RegisterAttached(
			"DropDownEventRevokers",
			typeof(object),
			typeof(ComboBoxHelper),
			new FrameworkPropertyMetadata(default(object)));
}
