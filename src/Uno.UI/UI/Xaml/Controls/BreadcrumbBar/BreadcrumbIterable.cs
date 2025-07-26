// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbIterable.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

internal partial class BreadcrumbIterable : IEnumerable<object?>
{
	public BreadcrumbIterable()
	{
	}

	public BreadcrumbIterable(object? itemsSource)
	{
		ItemsSource = itemsSource;
	}

	public object? ItemsSource { get; }

	public IEnumerator<object?> GetEnumerator() => new BreadcrumbIterator(ItemsSource);

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
