// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\ReversedVector.h/.cpp, tag winui3/release/1.7.1, commit 5f27a786

#if HAS_UNO
using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A wrapper around an IList that presents items in reversed order.
/// Used by AutoSuggestBox when the suggestion list is positioned above
/// the TextBox without a legacy ScaleTransform template part.
/// C++ equivalent: ReversedVector in ReversedVector.h/.cpp
/// </summary>
internal sealed class ReversedVector : IList<object>, IObservableVector<object>
{
	private readonly IList<object> _source;
	private readonly IObservableVector<object> _observableSource;

	public event VectorChangedEventHandler<object> VectorChanged;

	public ReversedVector(IList<object> source)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_observableSource = source as IObservableVector<object>
			?? throw new ArgumentException("The source must implement IObservableVector<object>.", nameof(source));
		_observableSource.VectorChanged += OnSourceVectorChanged;
	}

	public int Count => _source.Count;

	public bool IsReadOnly => true;

	public object this[int index]
	{
		get => _source[ReverseIndex(index)];
		set => throw new NotSupportedException();
	}

	public int IndexOf(object item)
	{
		var sourceIndex = _source.IndexOf(item);
		return sourceIndex >= 0 ? ReverseIndex(sourceIndex) : -1;
	}

	public bool Contains(object item) => _source.Contains(item);

	public void CopyTo(object[] array, int arrayIndex)
	{
		for (int i = 0; i < _source.Count; i++)
		{
			array[arrayIndex + i] = _source[ReverseIndex(i)];
		}
	}

	public IEnumerator<object> GetEnumerator()
	{
		for (int i = _source.Count - 1; i >= 0; i--)
		{
			yield return _source[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// Write operations are not supported on the reversed view.
	public void Insert(int index, object item) => throw new NotSupportedException();
	public void RemoveAt(int index) => throw new NotSupportedException();
	public void Add(object item) => throw new NotSupportedException();
	public void Clear() => throw new NotSupportedException();
	public bool Remove(object item) => throw new NotSupportedException();

	public void Detach()
	{
		_observableSource.VectorChanged -= OnSourceVectorChanged;
	}

	public bool IsBoundTo(IList<object> source) => ReferenceEquals(_source, source);

	private int ReverseIndex(int index) => _source.Count - 1 - index;

	private void OnSourceVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
	{
		var size = (uint)_source.Count;
		var index = args.CollectionChange switch
		{
			CollectionChange.ItemInserted => size - 1u - args.Index,
			CollectionChange.ItemRemoved => size - args.Index,
			CollectionChange.ItemChanged => size - 1u - args.Index,
			_ => 0u,
		};

		VectorChanged?.Invoke(this, new VectorChangedEventArgs(args.CollectionChange, index));
	}
}
#endif
