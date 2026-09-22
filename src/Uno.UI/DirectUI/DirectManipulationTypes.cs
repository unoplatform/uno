// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DirectManipulationTypes.h, commit dc46907e92

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
		Zoom = 0x00000010,
		PanInertia = 0x00000020,
		ZoomInertia = 0x00000080,
		RailsX = 0x00000100,
		RailsY = 0x00000200,
	}

	// DirectManipulation alignment for primary content. Determines where short content
	// is positioned within a larger viewport.
	internal enum DMAlignment
	{
		None = 0x00,
		Near = 0x01,
		Center = 0x02,
		Far = 0x04,
		UnlockCenter = 0x08,
	}

	// Overpan (rubber-band) mode for one direction.
	internal enum DMOverpanMode
	{
		Default = 0x00,
		None = 0x04,
	}

	// Coordinate system in which snap points are expressed.
	internal enum DMSnapCoordinate
	{
		Boundary = 0x00,
		Origin = 0x01,
		Mirrored = 0x10,
	}

	// Motion types for which DM can produce snap points / chaining.
	[Flags]
	internal enum DMMotionTypes : uint
	{
		None = 0x00,
		PanX = 0x01,
		PanY = 0x02,
		Zoom = 0x04,
		CenterX = 0x10,
		CenterY = 0x20,
	}

	internal enum ZoomDirection
	{
		None = 0,
		In = 1,
		Out = 2,
	}
}
