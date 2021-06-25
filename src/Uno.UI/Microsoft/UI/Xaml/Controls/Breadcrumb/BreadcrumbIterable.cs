// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class BreadcrumbIterable : IEnumerable<object?>
	{
		public BreadcrumbIterable()
		{
		}

		public BreadcrumbIterable(object itemsSource)
		{
			ItemsSource = itemsSource;
		}

		public object? ItemsSource { get; }

		public IEnumerator<object?> GetEnumerator() => new BreadcrumbIterator(ItemsSource);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
