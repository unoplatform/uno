#nullable disable

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// DirtyFlags.h

namespace Uno.UI.Xaml.Rendering
{
	internal enum DirtyFlags
	{
		None = 0x00,
		Render = 0x01,
		Bounds = 0x02,
		Independent = 0x04,
		ForcePropagate = 0x08,  // Only applies to content/subgraph dirty
	}
}
