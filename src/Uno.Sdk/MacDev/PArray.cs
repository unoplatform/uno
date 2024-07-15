// Original source: https://github.com/xamarin/Xamarin.MacDev

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Uno.Sdk.MacDev;

public class PArray : PObjectContainer, IEnumerable<PObject>
{
	private readonly List<PObject> _list;

	public override int Count => _list.Count;

	public PObject this[int i]
	{
		get => _list[i];
		set
		{
			if (i < 0 || i >= Count)
			{
				throw new ArgumentOutOfRangeException(nameof(i));
			}

			var existing = _list[i];
			_list[i] = value;

			OnChildReplaced(null, existing, value);
		}
	}

	public PArray()
	{
		_list = [];
	}

	public override PObject Clone()
	{
		var array = new PArray();
		foreach (var item in this)
		{
			array.Add(item.Clone());
		}

		return array;
	}

	protected override bool Reload(PropertyListFormat.ReadWriteContext ctx)
	{
		SuppressChangeEvents = true;
		var result = ctx.ReadArray(this);
		SuppressChangeEvents = false;
		if (result)
		{
			OnChanged(EventArgs.Empty);
		}

		return result;
	}

	public void Add(PObject obj)
	{
		_list.Add(obj);
		OnChildAdded(null, obj);
	}

	public void Insert(int index, PObject obj)
	{
		_list.Insert(index, obj);
		OnChildAdded(null, obj);
	}

	public void Replace(PObject oldObj, PObject newObject)
	{
		for (var i = 0; i < Count; i++)
		{
			if (_list[i] == oldObj)
			{
				_list[i] = newObject;
				OnChildReplaced(null, oldObj, newObject);
				break;
			}
		}
	}

	public void Remove(PObject obj)
	{
		if (_list.Remove(obj))
		{
			OnChildRemoved(null, obj);
		}
	}

	public void RemoveAt(int index)
	{
		var obj = _list[index];
		_list.RemoveAt(index);
		OnChildRemoved(null, obj);
	}

	public void Sort(IComparer<PObject> comparer) => _list.Sort(comparer);

	public void Clear()
	{
		_list.Clear();
		OnCleared();
	}

	public override string ToString() => string.Format(CultureInfo.InvariantCulture, "[PArray: Items={0}]", Count);

	public void AssignStringList(string strList)
	{
		SuppressChangeEvents = true;
		try
		{
			Clear();
			foreach (var item in strList.Split(',', ' '))
			{
				if (string.IsNullOrEmpty(item))
				{
					continue;
				}

				Add(new PString(item));
			}
		}
		finally
		{
			SuppressChangeEvents = false;
			OnChanged(EventArgs.Empty);
		}
	}

	public string[] ToStringArray()
	{
		var strlist = new List<string>();

		foreach (var str in _list.OfType<PString>())
		{
			strlist.Add(str.Value);
		}

		return [.. strlist];
	}

	public string ToStringList()
	{
		var sb = new StringBuilder();
		foreach (var str in _list.OfType<PString>())
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}

			sb.Append(str);
		}
		return sb.ToString();
	}

	public IEnumerator<PObject> GetEnumerator() => _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

	public override PObjectType Type => PObjectType.Array;
}
