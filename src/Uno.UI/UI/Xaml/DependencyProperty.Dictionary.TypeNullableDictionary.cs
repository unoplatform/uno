#nullable enable

using System;
using Uno.Collections;
using Uno.UI.Helpers;
using Uno.Extensions;

namespace Microsoft.UI.Xaml
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
			private readonly HashtableEx _entries = new HashtableEx(FastTypeComparer.Default);

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

			internal void Dispose()
				=> _entries.Dispose();
		}
	}
}
