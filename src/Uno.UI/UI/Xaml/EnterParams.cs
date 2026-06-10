// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EnterParams.h, commit fc2f82117

#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml;

//
// Structure used by Enter/EnterImpl for
// propagating state flags that affect the object.
//
internal struct EnterParams
{
	// These are control flags for Enter operations
	public bool IsLive;
	public bool SkipNameRegistration;
	public bool CoercedIsEnabled;
	public bool UseLayoutRounding;
	public bool IsForKeyboardAccelerator;
	public bool CheckForResourceOverrides;
	public ResourceDictionary? ParentResourceDictionary;

	/// <summary>
	/// The visual tree associated with this Enter walk. In WinUI, this is propagated
	/// through the Enter walk and set on each element via SetVisualTree.
	/// In Uno, we use this to help elements that aren't in the visual tree
	/// (e.g., flyout content) find the correct ContentRoot.
	/// </summary>
	public VisualTree? VisualTree;

	public EnterParams()
	{
		IsLive = true;
		SkipNameRegistration = false;
		CoercedIsEnabled = true;
		UseLayoutRounding = UseLayoutRoundingDefault;
		IsForKeyboardAccelerator = false;
		CheckForResourceOverrides = false;
		ParentResourceDictionary = null;
		VisualTree = null;
	}

	public EnterParams(
		bool isLive,
		bool skipNameRegistration,
		bool coercedIsEnabled,
		bool useLayoutRounding,
		VisualTree? enteringVisualTree)
	{
		IsLive = isLive;
		SkipNameRegistration = skipNameRegistration;
		CoercedIsEnabled = coercedIsEnabled;
		UseLayoutRounding = useLayoutRounding;
		IsForKeyboardAccelerator = false;
		CheckForResourceOverrides = false;
		ParentResourceDictionary = null;
		VisualTree = enteringVisualTree;
	}

	// Uno-specific convenience constructor (pre-existing call sites).
	public EnterParams(bool isLive) : this()
	{
		IsLive = isLive;
	}

	public const bool UseLayoutRoundingDefault = true;
}
