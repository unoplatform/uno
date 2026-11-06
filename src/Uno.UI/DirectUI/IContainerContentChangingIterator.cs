// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IContainerContentChangingIterator.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml;

namespace DirectUI;

internal interface IContainerContentChangingIterator
{
	uint Size { get; }

	DependencyObject? Current { get; }

	bool MoveNext();

	void Reset();
}
