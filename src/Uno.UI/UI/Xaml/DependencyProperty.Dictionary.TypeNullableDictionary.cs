#nullable enable

using System;
using Uno.Collections;
using Uno.UI.Helpers;
using Uno.Extensions;
using System.Collections;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private readonly static TypeNullableDictionary _isTypeNullableDictionary = new TypeNullableDictionary();

		private bool GetIsTypeNullable(Type type)
		{
			if (!_isTypeNullableDictionary.TryGetValue(type, out var isNullable))
			{
				_isTypeNullableDictionary.Add(type, isNullable = type.IsNullable());
			}

			return isNullable;
		}

		private class TypeNullableDictionary
		{
			// This dictionary has a single static instance that is kept for the lifetime of the whole app.
			// So we don't use pooling to not cause pool exhaustion by renting without returning.
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default, usePooling: false);

			internal bool TryGetValue(Type key, out bool result)
			{
				if (_entries.TryGetValue(key, out var value))
				{
					result = (bool)value!;
					return true;
				}

				result = false;
				return false;
			}

			internal void Add(Type key, bool isNullable)
				=> _entries.Add(key, isNullable);

			internal void Clear()
				=> _entries.Clear();
		}
	}
}
