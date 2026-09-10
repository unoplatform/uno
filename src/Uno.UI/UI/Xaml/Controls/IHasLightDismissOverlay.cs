// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by controls that expose a LightDismissOverlayMode, so that
/// <see cref="LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl{T}"/> can
/// be constrained the way WinUI's C++ template is duck-typed.
/// </summary>
internal interface IHasLightDismissOverlay
{
	LightDismissOverlayMode LightDismissOverlayMode { get; }
}
