// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\phone\lib\ElevationHelper.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DirectUI;

internal static class ElevationHelper
{
	//// The initial Z offset applied to all elevated controls
	//private const float s_elevationBaseDepth = 32.0f;
	//// This additional Z offset will be applied for each tier of logically parented controls
	//private const float s_elevationIterativeDepth = 8.0f;

	//internal static void ApplyThemeShadow(UIElement target)
	//{
	//	var themeShadow = new ThemeShadow();
	//	target.Shadow = themeShadow;
	//}

	//internal static void ApplyElevationEffect(UIElement target, int depth)
	//{
	//	// Calculate the Z offset based on the depth of the shadow
	//	var calculatedZDepth = s_elevationBaseDepth + (depth * s_elevationIterativeDepth);

	//	var endTranslation = new Vector3(0.0f, 0.0f, calculatedZDepth);

	//	// Apply a translation facade value
	//	target.Translation = endTranslation;

	//	// Apply a shadow to the element
	//	ApplyThemeShadow(target);
	//}

	// Move the control forward in Z and apply a shadow effect to it.
	// If the control is part of a tier of elevated controls (for example a MenuFlyoutSubItem),
	// you may provide an additional "depth" value that provides an additional Z offset.
	internal static void ApplyElevationEffect(UIElement target, int depth = 0, int? baseElevation = null)
	{
	}

	// Remove any shadow applied with ApplyElevationEffect
	internal static void ClearElevationEffect(UIElement target)
	{
	}

	// Checks if the "IsDefaultShadowEnabled" resource is defined as True or not, determining
	// if a control should enable a shadow by default.
	internal static bool IsDefaultShadowEnabled(FrameworkElement resourceTarget) => false;
}
