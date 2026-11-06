// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Tests.Native.External.Controls.Helpers;

/*
 * A data object class without a ToString implementation, used to exercise
 * AutomationProperties.Name resolutions when it isn't set and the obvious
 * sources are not available.
 */
public sealed class DataWithoutToString
{
	public DataWithoutToString()
	{
	}

	public DataWithoutToString(string name)
	{
		Name = name;
	}

	public string Name { get; set; }
}
