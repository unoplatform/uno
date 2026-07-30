// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LeaveParams.h, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml;

//
// Structure used by Leave/LeaveImpl for
// propagating state flags that affect the object.
//
internal struct LeaveParams
{
	// These are control flags for Leave operations
	public bool IsLive;
	public bool SkipNameRegistration;
	public bool CoercedIsEnabled;
	public bool UseLayoutRounding;
	public bool VisualTreeBeingReset;
	public bool IsForKeyboardAccelerator;
	public bool CheckForResourceOverrides;
	public ResourceDictionary? ParentResourceDictionary;

	/// <summary>
	/// Uno-specific: the visual tree associated with this Leave walk. WinUI's LeaveParams
	/// carries no visual tree; Uno uses it to help elements that aren't in the visual tree
	/// (e.g., flyout content) find the ContentRoot they are leaving.
	/// </summary>
	public VisualTree? VisualTree;

	public LeaveParams()
	{
		IsLive = true;
		SkipNameRegistration = false;
		CoercedIsEnabled = true;
		UseLayoutRounding = EnterParams.UseLayoutRoundingDefault;
		VisualTreeBeingReset = false;
		IsForKeyboardAccelerator = false;
		CheckForResourceOverrides = false;
		ParentResourceDictionary = null;
		VisualTree = null;
	}

	public LeaveParams(
		bool isLive,
		bool skipNameRegistration,
		bool coercedIsEnabled,
		bool visualTreeBeingReset)
	{
		IsLive = isLive;
		SkipNameRegistration = skipNameRegistration;
		CoercedIsEnabled = coercedIsEnabled;
		UseLayoutRounding = EnterParams.UseLayoutRoundingDefault;
		VisualTreeBeingReset = visualTreeBeingReset;
		IsForKeyboardAccelerator = false;
		CheckForResourceOverrides = false;
		ParentResourceDictionary = null;
		VisualTree = null;
	}

	// Uno-specific convenience constructor (pre-existing call sites).
	public LeaveParams(bool isLive) : this()
	{
		IsLive = isLive;
	}
}
