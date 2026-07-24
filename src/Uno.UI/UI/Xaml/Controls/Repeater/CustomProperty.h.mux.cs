// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CustomProperty.h, commit 4b206bce3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class CustomProperty
{
	private string m_name;
	private Type m_typeName;
	private Func<object, object> m_getter;
	private Action<object, object> m_setter;
}
