// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InspectingDataSource.cpp, commit 37ade09

namespace Microsoft.UI.Xaml.Controls;

// Even though the C++ code of this class contains the actual implementation,
// we had to move it to ItemsSourceView to match the public behavior.

internal partial class InspectingDataSource : ItemsSourceView
{
	public InspectingDataSource(object source) : base(source)
	{
	}
}
