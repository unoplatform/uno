// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParallaxView.idl, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify how the source offset values of a ParallaxView are interpreted.
/// </summary>
public enum ParallaxSourceOffsetKind
{
	/// <summary>
	/// The source start/end offset value is interpreted as an absolute value.
	/// </summary>
	Absolute = 0,

	/// <summary>
	/// The source start/end offset value is added to the auto-computed source offset.
	/// </summary>
	Relative = 1,
}
