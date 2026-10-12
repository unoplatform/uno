// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/tools/XCPTypesAutoGen/Modules/Controls/RichEditBox.cs, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBoxSelectionChangingEventArgs
{
	/// <summary>Gets the position at the beginning of the selection.</summary>
	public int SelectionStart
	{
		get => GetSelectionStart();
		internal set => SetSelectionStart(value);
	}

	/// <summary>Gets the number of characters in the selection.</summary>
	public int SelectionLength
	{
		get => GetSelectionLength();
		internal set => SetSelectionLength(value);
	}

	/// <summary>Gets or sets a value that indicates whether to cancel the selection change.</summary>
	public bool Cancel
	{
		get => GetCancel();
		set => SetCancel(value);
	}
}
