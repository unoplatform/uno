// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.UI.Xaml.Controls
{
	public enum ScrollingContentOrientation
	{
		Vertical = 0,
		Horizontal = 1,
		None = 2,
		Both = 3,
	}

	public enum ScrollingInteractionState
	{
		Idle = 0,
		Interaction = 1,
		Inertia = 2,
		Animation = 3,
	}

	public enum ScrollingScrollMode
	{
		Enabled = 0,
		Disabled = 1,
		Auto = 2,
	}

	public enum ScrollingZoomMode
	{
		Enabled = 0,
		Disabled = 1,
	}

	public enum ScrollingChainMode
	{
		Auto = 0,
		Always = 1,
		Never = 2,
	}

	public enum ScrollingRailMode
	{
		Enabled = 0,
		Disabled = 1,
	}

	[Flags]
	public enum ScrollingInputKinds
	{
		None = 0,
		Touch = 1,
		Pen = 2,
		MouseWheel = 4,
		Keyboard = 8,
		Gamepad = 16,
		All = 255,
	}

	public enum ScrollingAnimationMode
	{
		Disabled = 0,
		Enabled = 1,
		Auto = 2,
	}

	public enum ScrollingSnapPointsMode
	{
		Default = 0,
		Ignore = 1,
	}
}
