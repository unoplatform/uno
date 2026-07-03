// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.cpp/.h, commit 5f9e85113

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Hosts and controls the playback of an animated visual provided by an
/// <see cref="IAnimatedVisualSource"/>.
/// </summary>
/// <remarks>
/// On Skia, the player drives the underlying composition tree directly via composition animations,
/// matching the WinUI implementation. It derives from <see cref="Panel"/> so that
/// <see cref="AnimatedVisualPlayer.FallbackContent"/> can be hosted as a child when a source cannot
/// produce an <see cref="IAnimatedVisual"/> (matching WinUI, which uses a Panel-derived base).
/// </remarks>
[ContentProperty(Name = "Source")]
public partial class AnimatedVisualPlayer : Panel
{
}
