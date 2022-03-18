using System;
using System.Text;

namespace Uno.Globalization.NumberFormatting;

internal class StringBuilderPool
{
	private StringBuilder[] _items;

	public static StringBuilderPool Instance { get; } = new StringBuilderPool();

	public StringBuilderPool()
	{
		_items = new StringBuilder[Environment.ProcessorCount * 2];
	}

	public StringBuilder Get()
	{
		for (int i = 0; i < _items.Length; i++)
		{
			var val = _items[i];
			if (val != null)
			{
				_items[i] = null;
				return val;
			}
		}

		return new StringBuilder();
	}

	public void Return(StringBuilder obj)
	{
		obj.Clear();

		for (int i = 0; i < _items.Length; i++)
		{
			if (_items[i] == null)
			{
				_items[i] = obj;
				return;
			}
		}
	}
}
