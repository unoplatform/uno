// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl, commit 3cae15f0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a player for animated visual content.
/// </summary>
[ContentProperty(Name = "Source")]
public partial class AnimatedVisualPlayer : FrameworkElement, IPanel
{
	// WinUI sets a transparent Background in OnLoaded so XAML hit-tests the player at all
	// (AnimatedVisualPlayer.cpp:391-395). Uno has no Background on FrameworkElement, and the root
	// visual is attached at the same point, so gate hit-testing on that instead.
	internal override bool IsViewHit() => HasCompositionChildVisual;
}
