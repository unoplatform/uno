// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX reference b1878e2

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	public class TreeViewSelectionChangedEventArgs
	{
		internal TreeViewSelectionChangedEventArgs(IList<object> addedItems, IList<object> removedItems)
		{
			AddedItems = addedItems;
			RemovedItems = removedItems;
		}

		public IList<object> AddedItems { get; }

		public IList<object> RemovedItems { get; }
	}
}
