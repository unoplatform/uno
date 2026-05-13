// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CustomProperty.cpp, commit 4b206bce3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class CustomProperty
{
	public CustomProperty(
		string name,
		Type typeName,
		Func<object, object> getter,
		Action<object, object> setter)
	{
		m_name = name;
		m_typeName = typeName;
		m_getter = getter;
		m_setter = setter;
	}

	public bool CanRead => m_getter != null;

	public bool CanWrite => m_setter != null;

	public string Name => m_name;

	public Type Type => m_typeName;

	public object GetValue(object target)
	{
		return m_getter(target);
	}

	public void SetValue(object target, object value)
	{
		m_setter(target, value);
	}

	public object GetIndexedValue(object target, object index)
	{
		throw new NotImplementedException();
	}

	public void SetIndexedValue(object target, object value, object index)
	{
		throw new NotImplementedException();
	}
}
