// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PagerControlSelectedIndexChangedEventArgs.cpp, tag winui3/release/1.7.3, commit 65718e2813a9

namespace Microsoft.UI.Xaml.Controls;

public partial class PagerControlSelectedIndexChangedEventArgs
{
	public PagerControlSelectedIndexChangedEventArgs(int previousIndex, int newIndex)
	{
		PreviousPageIndex = previousIndex;
		NewPageIndex = newIndex;
	}

	public int PreviousPageIndex { get; }

	public int NewPageIndex { get; }
}
