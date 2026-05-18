// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\AutoSuggestBox\AutoSuggestBoxHelper.h/.cpp, tag winui3/release/1.7.1

#if HAS_UNO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// A wrapper around an IList that presents items in reversed order.
/// Used by AutoSuggestBox when the suggestion list is positioned above
/// the TextBox without a legacy ScaleTransform template part.
/// C++ equivalent: ReversedVector in AutoSuggestBoxHelper.h/.cpp
/// </summary>
internal sealed class ReversedVector : IList<object>, INotifyCollectionChanged
{
	private readonly IList<object> _source;
	private readonly INotifyCollectionChanged _sourceNotify;

	public event NotifyCollectionChangedEventHandler CollectionChanged;

	public ReversedVector(IList<object> source)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_sourceNotify = source as INotifyCollectionChanged;
		if (_sourceNotify is not null)
		{
			_sourceNotify.CollectionChanged += OnSourceCollectionChanged;
		}
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
		if (_sourceNotify is not null)
		{
			_sourceNotify.CollectionChanged -= OnSourceCollectionChanged;
		}
	}

	private int ReverseIndex(int index) => _source.Count - 1 - index;

	private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		// Forward the event with reversed indices where applicable.
		NotifyCollectionChangedEventArgs reversedArgs;

		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				var addIndex = e.NewStartingIndex >= 0 ? ReverseIndex(e.NewStartingIndex) : -1;
				reversedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems, addIndex);
				break;

			case NotifyCollectionChangedAction.Remove:
				// After removal, count has already changed, so we need to adjust.
				// The source has already removed the item, so the reversed index maps
				// from the old count perspective.
				var removeIndex = e.OldStartingIndex >= 0 ? _source.Count - e.OldStartingIndex : -1;
				reversedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems, removeIndex);
				break;

			case NotifyCollectionChangedAction.Reset:
				reversedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
				break;

			default:
				// For Replace and Move, just issue a Reset to keep things simple.
				reversedArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
				break;
		}

		CollectionChanged?.Invoke(this, reversedArgs);
	}
}
#endif
