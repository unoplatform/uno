// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference TrackerCollections.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DirectUI;

// TrackerCollection<T> itself lives in JoltCollections.h.cs. Remove, IndexOf and Contains aren't virtual there,
// so this class re-implements IList<T> to make them raise VectorChanged and compare through PropertyValue.
internal class ObservableTrackerCollection<T> : TrackerCollection<T>, IObservableVector<T>, IList<T>
{
	public event VectorChangedEventHandler<T>? VectorChanged;

	// Overrides of the write portion of IVector<T> since that is the
	// part that can cause changes
	public override T this[int index]
	{
		get => base[index];
		set
		{
			if ((uint)index >= (uint)Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}

			base[index] = value;

			RaiseVectorChanged(CollectionChange.ItemChanged, (uint)index);
		}
	}

	public override void Insert(int index, T item)
	{
		if ((uint)index > (uint)Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		base.Insert(index, item);

		RaiseVectorChanged(CollectionChange.ItemInserted, (uint)index);
	}

	public override void RemoveAt(int index)
	{
		if ((uint)index >= (uint)Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		base.RemoveAt(index);

		RaiseVectorChanged(CollectionChange.ItemRemoved, (uint)index);
	}

	public override void Add(T item)
	{
		base.Add(item);

		RaiseVectorChanged(CollectionChange.ItemInserted, (uint)Count - 1);
	}

	public override void RemoveAtEnd()
	{
		if (Count == 0)
		{
			throw new ArgumentOutOfRangeException();
		}

		RemoveAt(Count - 1);
	}

	public override void Clear()
	{
		base.Clear();

		RaiseVectorChanged(CollectionChange.Reset, 0);
	}

	public new int IndexOf(T item)
	{
		for (var i = 0; i < Count; i++)
		{
			if (PropertyValue.AreEqualImpl(item, base[i]))
			{
				return i;
			}
		}

		return -1;
	}

	public new bool Contains(T item) => IndexOf(item) >= 0;

	public new bool Remove(T item)
	{
		var index = IndexOf(item);
		if (index < 0)
		{
			return false;
		}

		RemoveAt(index);
		return true;
	}

	private void RaiseVectorChanged(CollectionChange action, uint index) =>
		VectorChanged?.Invoke(this, new VectorChangedEventArgs(action, index));
}
