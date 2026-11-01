// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference storyboard.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// Controls animations with a timeline, and provides object and property targeting information for its child animations.
/// </summary>
/// <remarks>
/// TODO Uno specific: WinUI's CStoryboard derives from CParallelTimeline and CTimelineGroup, which have no public
/// counterpart: they are folded into Storyboard (TimelineGroup.*.mux.cs and ParallelTimeline.*.mux.cs).
/// </remarks>
[ContentProperty(Name = nameof(Children))]
public sealed partial class Storyboard : Timeline
{
}
