// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Uno-specific additions for ViewportManagerWithPlatformFeatures:
// - _uno_viewportUsedInLastMeasure: tracks the visible window at the time of the last
//   measure pass so that we can skip InvalidateMeasure calls that would otherwise be
//   triggered by minor viewport changes. See UpdateViewport for usage.
// Wrapped in the !UNO_HAS_ENHANCED_LIFECYCLE guard because the enhanced lifecycle
// avoids the Android/iOS arrange-phase invalidation that this workaround exists for.

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ViewportManagerWithPlatformFeatures
{
#if !UNO_HAS_ENHANCED_LIFECYCLE
	private Rect _uno_viewportUsedInLastMeasure;
#endif
}
