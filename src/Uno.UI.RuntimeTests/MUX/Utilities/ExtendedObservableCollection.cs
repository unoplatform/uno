// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MUXControlsTestApp.Utilities
{
	/// <summary>
	/// This class aims to add functionality that is present in other collections that can be used as ItemSource for controls,
	/// e.g. ReplaceAll of the winrt::IVector.
	/// See https://github.com/microsoft/microsoft-ui-xaml/issues/1379 for additional context as to why this is needed for easy testing.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class ExtendedObservableCollection<T> : ObservableCollection<T>
	{
		/// <summary>
		/// Removes all items of this collection and replaces them with the collection specified.
		/// Triggers the "Reset" action.
		/// </summary>
		/// <param name="items">Collection containing the items that will be replacing the previous content</param>
		public void ReplaceAll(ICollection<T> items)
		{
			this.Items.Clear();
			foreach (T item in items)
			{
				this.Items.Add(item);
			}
			this.OnCollectionChanged(
				new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
			);
		}

	}
}
