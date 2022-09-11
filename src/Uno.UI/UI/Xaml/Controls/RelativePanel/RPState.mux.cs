// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RPNode.h

using System;

namespace Uno.UI.Xaml.Controls;

[Flags]
internal enum RPState
{
	Unresolved = 0x00,
	Pending = 0x01,
	Measured = 0x02,
	ArrangedHorizontally = 0x04,
	ArrangedVertically = 0x08,
	Arranged = 0x0C
}
