// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Tests.Common;

public sealed class GroupedHeader : List<object>, ICustomPropertyProvider
{
	public string Header { get; set; }

	public GroupedHeader(string header)
	{
		Header = header;
	}

	public ICustomProperty GetCustomProperty(string name) => null;

	public ICustomProperty GetIndexedProperty(string name, Type type) => throw new NotImplementedException();

	public string GetStringRepresentation() => Header;

	public Type Type => typeof(GroupedHeader);

	public override string ToString() => Header;

	public void Append(object value) => Add(value);
}
