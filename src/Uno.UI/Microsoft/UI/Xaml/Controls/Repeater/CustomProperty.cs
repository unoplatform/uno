// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CustomProperty.cpp, commit d883cf3

#nullable enable

using System;
using Windows.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

internal class CustomProperty : ICustomProperty
{
	private Func<object, object>? _getter;
	private Action<object, object>? _setter;

	public CustomProperty(string name, Type type, Func<object, object> getter, Action<object, object> setter)
	{
		Name = name;
		Type = type;
		_getter = getter;
		_setter = setter;
	}

	public bool CanRead => _getter != null;

	public bool CanWrite => _setter != null;

	public string Name { get; }

	public Type Type { get; }

	public object GetValue(object target)
	{
		if (!CanRead)
		{
			throw new InvalidOperationException($"Property {Name} is not readable.");
		}
		return _getter!(target);
	}

	public void SetValue(object target, object value)
	{
		if (!CanWrite)
		{
			throw new InvalidOperationException($"Property {Name} is not writable.");
		}
		_setter!(target, value);
	}

	public object GetIndexedValue(object target, object index) => throw new NotImplementedException();

	public void SetIndexedValue(object target, object value, object index) => throw new NotImplementedException();
}
