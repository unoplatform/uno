// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DirectManipulationTypes.h, commit 5f9e85113

using System;

namespace DirectUI
{
	// DirectManipulation configuration flags. Used by ScrollViewer to describe the
	// allowed motions for a viewport configuration. In WinUI these flags are passed
	// to the DirectManipulation Win32 APIs; in Uno they are interpreted by the
	// managed scroll path to gate which gestures are allowed.
	[Flags]
	internal enum DMConfigurations : uint
	{
		None = 0x00000000,
		Interaction = 0x00000001,
		PanX = 0x00000002,
		PanY = 0x00000004,
		PanInertia = 0x00000008,
		PanRailsX = 0x00000010,
		PanRailsY = 0x00000020,
		Zoom = 0x00000040,
		ZoomInertia = 0x00000080,
	}

	// DirectManipulation alignment for primary content. Determines where short content
	// is positioned within a larger viewport.
	internal enum DMAlignment
	{
		None = 0x00,
		Near = 0x01,
		Center = 0x02,
		Far = 0x04,
		Stretch = 0x08,
		Unlocked = 0x10,
	}

	// Overpan (rubber-band) mode for one direction.
	internal enum DMOverpanMode
	{
		Default = 0,
		None = 1,
	}

	// Coordinate system in which snap points are expressed.
	internal enum DMSnapCoordinate
	{
		Boundary = 0,
		Origin = 1,
		Mirrored = 2,
	}

	// Motion types for which DM can produce snap points / chaining.
	[Flags]
	internal enum DMMotionTypes : uint
	{
		None = 0x00,
		PanX = 0x01,
		PanY = 0x02,
		Zoom = 0x04,
		CenterX = 0x08,
		CenterY = 0x10,
	}

	internal enum ZoomDirection
	{
		None = 0,
		In = 1,
		Out = 2,
	}
}
