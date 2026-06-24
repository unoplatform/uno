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
/// matching the WinUI implementation. On other platforms (and for sources that do not return an
/// <see cref="IAnimatedVisual"/> from <see cref="IAnimatedVisualSource.TryCreateAnimatedVisual"/>),
/// the player falls back to invoking the legacy Uno <see cref="IAnimatedVisualSource"/> hooks.
/// </remarks>
[ContentProperty(Name = "Source")]
public partial class AnimatedVisualPlayer : FrameworkElement
{
}
