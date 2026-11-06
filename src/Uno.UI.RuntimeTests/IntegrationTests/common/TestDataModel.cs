// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Tests.Common;

public sealed class MocoBasicFlatDataModel : ICustomPropertyProvider, INotifyPropertyChanged
{
	private string _name;
	private string _subject;
	private string _message;
	private int _minHeight;
	private bool _isExpanded;

	public MocoBasicFlatDataModel(string name)
	{
		Name = name;
		Subject = "A dummy subject";
		Message = "This is a dummy message";
		MinHeight = 50;
	}

	public ICustomProperty GetCustomProperty(string name) => null;

	public ICustomProperty GetIndexedProperty(string name, Type type) => throw new NotImplementedException();

	public string GetStringRepresentation() => Name;

	public Type Type => typeof(MocoBasicFlatDataModel);

	public override string ToString() => Name;

	public void OnPropertyChanged(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;

	public string Name
	{
		get => _name;
		set
		{
			if (_name != value)
			{
				_name = value;
				OnPropertyChanged("Name");
			}
		}
	}

	public string Subject
	{
		get => _subject;
		set
		{
			if (_subject != value)
			{
				_subject = value;
				OnPropertyChanged("Subject");
			}
		}
	}

	public string Message
	{
		get => _message;
		set
		{
			if (_message != value)
			{
				_message = value;
				OnPropertyChanged("Message");
			}
		}
	}

	public int MinHeight
	{
		get => _minHeight;
		set
		{
			if (_minHeight != value)
			{
				_minHeight = value;
			}
		}
	}

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
			}
		}
	}
}

public sealed class MocoBasicGroupedDataModel : List<object>, ICustomPropertyProvider, INotifyPropertyChanged
{
	private string _header;
	private int _height;
	private int _width;

	public MocoBasicGroupedDataModel(string key)
	{
		Header = key;
		_height = 50;
		_width = 50;
	}

	public ICustomProperty GetCustomProperty(string name) => null;

	public ICustomProperty GetIndexedProperty(string name, Type type) => throw new NotImplementedException();

	public string GetStringRepresentation() => Header;

	public Type Type => typeof(MocoBasicGroupedDataModel);

	public override string ToString() => Header;

	public void OnPropertyChanged(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;

	public string Header
	{
		get => _header;
		set
		{
			if (_header != value)
			{
				_header = value;
				OnPropertyChanged("Header");
			}
		}
	}

	public int Height
	{
		get => _height;
		set
		{
			if (_height != value)
			{
				_height = value;
			}
		}
	}

	public int Width
	{
		get => _width;
		set
		{
			if (_width != value)
			{
				_width = value;
			}
		}
	}

	public void Append(object value) => Add(value);
}

public sealed class VectorChangedEventArgs : IVectorChangedEventArgs
{
	public VectorChangedEventArgs(CollectionChange change, uint index)
	{
		CollectionChange = change;
		Index = index;
	}

	public CollectionChange CollectionChange { get; }

	public uint Index { get; }
}

public sealed class MocoResettableCollection : IObservableVector<object>
{
	private readonly List<object> _items = new();

	public event VectorChangedEventHandler<object> VectorChanged;

	public void Reset() => OnVectorChanged(CollectionChange.Reset, 0);

	public void ResetRemove(int index)
	{
		if (index >= 0)
		{
			_items.RemoveAt(index);
		}

		OnVectorChanged(CollectionChange.Reset, 0);
	}

	public void ResetFlip()
	{
		for (int i = 0; i < _items.Count / 2; i++)
		{
			int otherIndex = _items.Count - 1 - i;

			object temp = _items[i];
			_items[i] = _items[otherIndex];
			_items[otherIndex] = temp;
		}

		OnVectorChanged(CollectionChange.Reset, 0);
	}

	public void ResetRemoveFlip(int index)
	{
		if (index >= 0)
		{
			_items.RemoveAt(index);
		}

		ResetFlip();
	}

	public void OnVectorChanged(CollectionChange change, int index)
		=> VectorChanged?.Invoke(this, new VectorChangedEventArgs(change, (uint)index));

	public void Append(object value) => Add(value);

	public void RemoveAtEnd()
	{
		_items.RemoveAt(_items.Count - 1);
		OnVectorChanged(CollectionChange.ItemRemoved, _items.Count);
	}

	public void ReplaceAll(IEnumerable<object> items)
	{
		_items.Clear();
		_items.AddRange(items);
		OnVectorChanged(CollectionChange.Reset, 0);
	}

	public int Count => _items.Count;

	public bool IsReadOnly => false;

	public object this[int index]
	{
		get => _items[index];
		set
		{
			_items[index] = value;
			OnVectorChanged(CollectionChange.ItemChanged, index);
		}
	}

	public int IndexOf(object item) => _items.IndexOf(item);

	public void Insert(int index, object item)
	{
		_items.Insert(index, item);
		OnVectorChanged(CollectionChange.ItemInserted, index);
	}

	public void RemoveAt(int index)
	{
		_items.RemoveAt(index);
		OnVectorChanged(CollectionChange.ItemRemoved, index);
	}

	public void Add(object item)
	{
		_items.Add(item);
		OnVectorChanged(CollectionChange.ItemInserted, _items.Count - 1);
	}

	public void Clear()
	{
		_items.Clear();
		OnVectorChanged(CollectionChange.Reset, 0);
	}

	public bool Contains(object item) => _items.Contains(item);

	public void CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

	public bool Remove(object item)
	{
		int index = _items.IndexOf(item);

		if (index < 0)
		{
			return false;
		}

		RemoveAt(index);
		return true;
	}

	public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

public static class MocoBasicDataSource
{
	public static IList<object> GetFlatDataCollection(int count)
	{
		var dataCollection = new List<object>();
		PopulateWithFlatData(dataCollection, count);
		return dataCollection;
	}

	public static IList<object> GetGroupedDataCollection(int count, int itemsCount, bool includeEmptyGroups)
	{
		var dataCollection = new List<object>();
		PopulateWithGroupedData(dataCollection, count, itemsCount, includeEmptyGroups);
		return dataCollection;
	}

	public static MocoResettableCollection GetResettableDataCollection(int count)
	{
		var dataCollection = new MocoResettableCollection();
		PopulateWithFlatData(dataCollection, count);
		return dataCollection;
	}

	public static MocoResettableCollection GetGroupedResettableDataCollection(int count, int itemsCount, bool includeEmptyGroups)
	{
		var dataCollection = new MocoResettableCollection();
		PopulateWithGroupedData(dataCollection, count, itemsCount, includeEmptyGroups);
		return dataCollection;
	}

	private static void PopulateWithFlatData(IList<object> dataCollection, int count)
	{
		for (int i = 0; i < count; i++)
		{
			dataCollection.Add(new MocoBasicFlatDataModel("Item" + i));
		}
	}

	private static void PopulateWithGroupedData(IList<object> dataCollection, int count, int itemsCount, bool includeEmptyGroups)
	{
		string groupHeaderNames = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

		for (int i = 0; i < count; i++)
		{
			var substring = groupHeaderNames.Substring(i % groupHeaderNames.Length, 1);
			var group = new MocoBasicGroupedDataModel(substring);
			if (!(includeEmptyGroups && i % 2 == 1)) // every alternate group is empty if includeEmptyGroups flag is set
			{
				for (int j = 0; j < itemsCount; j++)
				{
					group.Append(new MocoBasicFlatDataModel(group.Header + " - Item " + j));
				}
			}
			dataCollection.Add(group);
		}
	}
}
