#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	public sealed partial class DependencyProperty
	{
		private readonly static Dictionary<Type, bool> _isTypeNullableDictionary = new(FastTypeComparer.Default);

		private bool GetIsTypeNullable(Type type)
		{
			if (!_isTypeNullableDictionary.TryGetValue(type, out var isNullable))
			{
				_isTypeNullableDictionary.Add(type, isNullable = type.IsNullable());
			}

			return isNullable;
		}
	}
}
